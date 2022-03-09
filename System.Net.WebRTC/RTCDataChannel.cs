using System.Buffers;
using System.Collections.Concurrent;
using System.IO;
using System.Runtime.InteropServices.JavaScript;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.WebRTC
{
    public class RTCDataChannel : IDisposable
    {
        private const int receiveChunkSize = 64 * 1024;
        private ActionQueue<ReceivePayload> receiveMessageQueue = new ActionQueue<ReceivePayload>();
        private JSObject hostObject;
        private Action<JSObject> onMessage;
        private Action<JSObject> onOpen;
        private Action<JSObject> onClose;
        // Stages of this class. 
        private int state;
        private const int created = 0;
        private const int connecting = 1;
        private const int connected = 2;
        private const int disposed = 3;
        private readonly CancellationTokenSource cts;
        private ArrayPool<byte> arrayPool = ArrayPool<byte>.Shared;
        public event AsyncEventHandler<byte[]> OnDataMessage;
        public event AsyncEventHandler<string> OnMessage;
        public event EventHandler OnOpen;
        public event EventHandler OnClose;
        public RTCDataChannel(JSObject ho)
        {
            cts = new CancellationTokenSource();
            this.hostObject = ho;

            _ = Task.Run(Receive);

            onMessage = new Action<JSObject>((messageEvent) =>
            {
                ThrowIfNotConnected();

                using (messageEvent)
                {
                    // get the events "data"
                    var eventData = messageEvent.GetObjectProperty("data");

                    switch(eventData)
                    {
                        case ArrayBuffer buffer: using (buffer)
                            {
                                var mess = new ReceivePayload(buffer, WebSockets.WebSocketMessageType.Binary);
                                receiveMessageQueue.BufferPayload(mess);
                            }
                            break;
                        case JSObject blobData: using (blobData)
                            {
                                // TODO: Handle ArrayBuffer binary type but have only seen 'blob' so far without
                                // changing the default websocket binary type manually.
                                var dataType = hostObject.GetObjectProperty("binaryType").ToString();
                                if (dataType == "blob")
                                {

                                    Action<JSObject> loadend = null;
                                    // Create a new "FileReader" object
                                    using (var reader = new HostObject("FileReader"))
                                    {
                                        loadend = new Action<JSObject>((loadEvent) =>
                                        {
                                            using (var target = (JSObject)loadEvent.GetObjectProperty("target"))
                                            {
                                                if ((int)target.GetObjectProperty("readyState") == 2)
                                                {
                                                    using (var binResult = (ArrayBuffer)target.GetObjectProperty("result"))
                                                    {
                                                        var mess = new ReceivePayload(binResult, WebSockets.WebSocketMessageType.Binary);
                                                        receiveMessageQueue.BufferPayload(mess);
                                                        //Runtime.FreeObject(loadend);
                                                    }
                                                }
                                            }
                                            loadEvent.Dispose();

                                        });

                                        reader.Invoke("addEventListener", "loadend", loadend);

                                        //using (var blobData = (JSObject)messageEvent.GetObjectProperty("data"))
                                        //    reader.Invoke("readAsArrayBuffer", blobData);
                                    }
                                }
                                else
                                    throw new NotImplementedException($"WebSocket binary type '{hostObject.GetObjectProperty("binaryType").ToString()}' not supported.");
                            }
                            break;
                        case String message:
                            {
                                var mess = new ReceivePayload(Encoding.UTF8.GetBytes(message), WebSockets.WebSocketMessageType.Text);
                                receiveMessageQueue.BufferPayload(mess);
                            }
                            break;
                    }
                }
            });

            // Attach the onMessage callaback
            hostObject.Invoke("addEventListener", "message", onMessage);

            onOpen = new Action<JSObject>((messageEvent) =>
            {
                this.OnOpen?.Invoke(this, EventArgs.Empty);
                messageEvent.Dispose();
            });

            // Attach the onMessage callaback
            hostObject.SetObjectProperty("onopen", onOpen);

            onClose = new Action<JSObject>((messageEvent) =>
            {
                this.OnClose?.Invoke(this, EventArgs.Empty);
                messageEvent.Dispose();

            });

            // Attach the onMessage callaback
            hostObject.SetObjectProperty("onclose", onClose);
        }

        private async ValueTask Receive()
        {
            var ms = new MemoryStream();
            while (state != disposed)
            {
                var buffer = arrayPool.Rent(receiveChunkSize);

                var result = await this.ReceiveAsync(buffer, CancellationToken.None);
                if (result.MessageType == WebSockets.WebSocketMessageType.Close)
                {
                    //await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None).ConfigureAwait(false);
                }
                else
                {
                    switch (result.MessageType)
                    {
                        case WebSockets.WebSocketMessageType.Text:
                            {
                                var evt = OnMessage;
                                if (evt != null)
                                {
                                    await evt.Invoke(this, Encoding.UTF8.GetString(buffer, 0, result.Count)).ConfigureAwait(false);
                                    arrayPool.Return(buffer);
                                }
                            }
                            break;
                        case WebSockets.WebSocketMessageType.Binary:
                            {
                                if (result.EndOfMessage)
                                {
                                    var evt = OnDataMessage;
                                    if (evt != null)
                                    {
                                        byte[] buf = new byte[result.Count];
                                        Buffer.BlockCopy(buffer, 0, buf, 0, result.Count);
                                        await evt.Invoke(this, buf).ConfigureAwait(false);
                                    }
                                }
                                else
                                {
                                    ms.Write(buffer, 0, result.Count);
                                    while (!result.EndOfMessage)
                                    {
                                        result = await ReceiveAsync(buffer, CancellationToken.None);
                                        ms.Write(buffer, 0, result.Count);
                                    }

                                    var evt = OnDataMessage;
                                    if (evt != null)
                                    {
                                        await evt.Invoke(this, ms.ToArray()).ConfigureAwait(false);
                                    }
                                    ms.SetLength(0);
                                }
                            }
                            break;
                        case WebSockets.WebSocketMessageType.Close:
                            break;
                    }
                }
            }
        }

        private ReceivePayload bufferedPayload;

        /// <summary>
        /// Receives data on <see cref="T:WebAssembly.Net.WebSockets.ClientWebSocket"/> as an asynchronous operation.
        /// </summary>
        /// <returns>The async.</returns>
        /// <param name="buffer">Buffer.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public async Task<WebRtcReceiveResult> ReceiveAsync(ArraySegment<byte> buffer, CancellationToken cancellationToken)
        {
            ThrowIfDisposed();

            var tcsReceive = new TaskCompletionSource<WebRtcReceiveResult>();

            // Wrap the cancellationToken in a using so that it can be disposed of whether
            // we successfully receive or not.
            // Otherwise any timeout/cancellation would apply to the full session.
            using (cancellationToken.Register(() => tcsReceive.TrySetCanceled()))
            {

                if (bufferedPayload == null)
                    bufferedPayload = await receiveMessageQueue.DequeuePayloadAsync(cancellationToken);

                var endOfMessage = bufferedPayload.BufferPayload(buffer, out WebRtcReceiveResult receiveResult);

                tcsReceive.SetResult(receiveResult);

                if (endOfMessage)
                    bufferedPayload = null;

                return await tcsReceive.Task;
            }
        }


        public RTCDataChannelState ReadyState
        {
            get
            {
                var s = this.hostObject.GetObjectProperty("readyState") as string;
                switch(s)
                {
                    case "connecting":
                        return RTCDataChannelState.Connecting;
                    case "open":
                        return RTCDataChannelState.Open;
                    case "closed":
                        return RTCDataChannelState.Closed;
                    default:
                        return RTCDataChannelState.Closing;
                }
            }
        }

        private void ThrowIfDisposed()
        {
            if (state == disposed)
            {
                throw new ObjectDisposedException(GetType().FullName);
            }
        }

        public void Send(ArraySegment<byte> buffer)
        {
            ThrowIfNotConnected();
            using (var uint8Buffer = Uint8Array.From(buffer))
            {
                hostObject.Invoke("send", uint8Buffer);
            }
        }

        public void Send(string buffer)
        {
            ThrowIfNotConnected();

            hostObject.Invoke("send", buffer);
        }

        private void ThrowIfNotConnected()
        {
            if (state == disposed)
            {
                throw new ObjectDisposedException(GetType().FullName);
            }
            else if (ReadyState != RTCDataChannelState.Open)
            {
                throw new InvalidOperationException("WebSocket is not connected");
            }
        }

        public void Dispose()
        {
            int priorState = Interlocked.Exchange(ref state, disposed);
            if (priorState == disposed)
            {
                // No cleanup required.
                return;
            }

            // registered by the CancellationTokenSource cts in the connect method
            cts.Cancel(false);
            cts.Dispose();


            // We need to clear the events on websocket as well or stray events
            // are possible leading to crashes.
            if (onMessage != null)
            {
                hostObject.SetObjectProperty("onmessage", "");
                System.Runtime.InteropServices.JavaScript.Runtime.FreeObject(onMessage);
            }

            // We need to clear the events on websocket as well or stray events
            // are possible leading to crashes.
            if (onOpen != null)
            {
                hostObject.SetObjectProperty("onopen", "");
                System.Runtime.InteropServices.JavaScript.Runtime.FreeObject(onOpen);
            }

            // We need to clear the events on websocket as well or stray events
            // are possible leading to crashes.
            if (onClose != null)
            {
                hostObject.SetObjectProperty("onclose", "");
                System.Runtime.InteropServices.JavaScript.Runtime.FreeObject(onClose);
            }

            hostObject?.Dispose();
        }

        private class ActionQueue<T>
        {

            private readonly SemaphoreSlim actionSem;
            private readonly ConcurrentQueue<T> actionQueue;

            public ActionQueue()
            {
                actionSem = new SemaphoreSlim(0);
                actionQueue = new ConcurrentQueue<T>();
            }

            public void BufferPayload(T item)
            {
                actionQueue.Enqueue(item);
                actionSem.Release();
            }

            public async Task<T> DequeuePayloadAsync(CancellationToken cancellationToken = default(CancellationToken))
            {
                while (true)
                {
                    await actionSem.WaitAsync(cancellationToken);

                    T item;
                    if (actionQueue.TryDequeue(out item))
                    {
                        return item;
                    }
                }
            }
        }
    }

    public enum RTCDataChannelState
    {
        Connecting,
        Open,
        Closing,
        Closed
    }
}
