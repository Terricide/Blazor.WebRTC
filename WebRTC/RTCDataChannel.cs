using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Blazor.WebRTC
{
    public class RTCDataChannel : IAsyncDisposable
    {
        private string _readyState;
        public string ReadyState
        {
            get
            {
                return _readyState;
            }
            set
            {
                if (_readyState != value)
                {
                    _readyState = value;
                    switch (_readyState)
                    {
                        case "open":
                            OnOpen?.Invoke(this, EventArgs.Empty);
                            break;
                        case "closed":
                            OnClose?.Invoke(this, EventArgs.Empty);
                            break;
                    }
                }
            }
        }
        public DotNetObjectReference<RTCDataChannel> Ref;
        private IJSObjectReference dataChannel;
        private IJSUnmarshalledObjectReference dataChannelReference;
        public event EventHandler OnOpen;
        public event EventHandler OnClose;
        public event EventHandler<string> OnMessage;
        public event EventHandler<byte[]> OnDataMessage;
        public RTCDataChannel()
        {
            this.Ref = DotNetObjectReference.Create(this);
        }

        public void SetJsObject(IJSObjectReference objectReference)
        {
            this.dataChannel = objectReference;
        }

        public void SetJsObject(IJSUnmarshalledObjectReference objectReference)
        {
            this.dataChannelReference = objectReference;
        }

        public string Label { get; set; }

        [JSInvokable]
        public ValueTask _onopen()
        {
            ReadyState = "open";
            return ValueTask.CompletedTask;
        }

        [JSInvokable]
        public ValueTask _onclose()
        {
            ReadyState = "closed";
            return ValueTask.CompletedTask;
        }

        [JSInvokable]
        public ValueTask _onmessage(string msg)
        {
            OnMessage?.Invoke(this, msg);
            return ValueTask.CompletedTask;
        }

        [JSInvokable]
        public ValueTask _ondatamessage(byte[] msg)
        {
            OnDataMessage?.Invoke(this, msg);
            return ValueTask.CompletedTask;
        }

        public async ValueTask DisposeAsync()
        {
            await dataChannel.DisposeAsync();
        }

        public async ValueTask<string> GetReadyState()
        {
            var state = await dataChannel.InvokeAsync<string>("getReadyState");
            if (state != ReadyState)
            {
                ReadyState = state;
            }
            return state;
        }

        [StructLayout(LayoutKind.Explicit)]
        public struct InteropData
        {
            [FieldOffset(0)]
            public byte[] Data;

            [FieldOffset(8)]
            public bool IsText;
        }

        [StructLayout(LayoutKind.Explicit)]
        public struct InteropString
        {
            [FieldOffset(0)]
            public string Text;

            [FieldOffset(8)]
            public bool IsText;
        }

        public void Send(byte[] data)
        {
            if (dataChannelReference != null)
            {
                dataChannelReference.InvokeUnmarshalled<byte[], object>("sendData", data);
            }
            else
            {
                dataChannel.InvokeVoidAsync("send", new InteropData
                {
                    IsText = false,
                    Data = data
                });
            }
        }

        public void Send(string text)
        {
            if (dataChannelReference != null)
            {
                dataChannelReference.InvokeUnmarshalled<InteropString, object>("send", new InteropString
                {
                    IsText = true,
                    Text = text
                });
            }
            else
            {
                dataChannel.InvokeVoidAsync("send", new InteropString
                {
                    IsText = true,
                    Text = text
                });
            }
        }
    }
}
