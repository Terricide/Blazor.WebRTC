using System.Runtime.InteropServices.JavaScript;
using System.Threading;

namespace System.Net.WebRTC
{
    public class RTCDataChannel : IDisposable
    {
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

        public event EventHandler<byte[]> OnDataMessage;
        public event EventHandler<string> OnMessage;
        public event EventHandler OnOpen;
        public event EventHandler OnClose;
        public RTCDataChannel(JSObject ho)
        {
            cts = new CancellationTokenSource();
            this.hostObject = ho;

            onMessage = new Action<JSObject>((messageEvent) =>
            {
                ThrowIfNotConnected();

                // get the events "data"
                var eventData = messageEvent.GetObjectProperty("data");

                // If the messageEvent's data property is marshalled as a JSObject then we are dealing with 
                // binary data
                if (eventData is ArrayBuffer ab)
                {
                    using (var arrayBuffer = ab)
                    {
                        using (var bin = new Uint8Array(arrayBuffer))
                        {
                            OnDataMessage?.Invoke(this, bin.ToArray());
                        }
                    }
                }
                else if (eventData is JSObject evt)
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
                                            //var mess = new ReceivePayload(binResult, WebSocketMessageType.Binary);
                                            //receiveMessageQueue.BufferPayload(mess);
                                            //Runtime.FreeObject(loadend);
                                        }
                                    }
                                }
                                loadEvent.Dispose();

                            });

                            reader.Invoke("addEventListener", "loadend", loadend);

                            using (var blobData = (JSObject)messageEvent.GetObjectProperty("data"))
                                reader.Invoke("readAsArrayBuffer", blobData);
                        }
                    }
                    else
                        throw new NotImplementedException($"WebSocket bynary type '{hostObject.GetObjectProperty("binaryType").ToString()}' not supported.");
                }
                else if (eventData is string)
                {
                    OnMessage?.Invoke(this, eventData as string);
                }
                messageEvent.Dispose();

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
    }

    public enum RTCDataChannelState
    {
        Connecting,
        Open,
        Closing,
        Closed
    }
}
