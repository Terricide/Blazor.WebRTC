using Microsoft.JSInterop;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text.Json;
using System.Buffers;
using System.Runtime.InteropServices;

namespace Blazor.WebRTC
{
    // This class provides an example of how JavaScript functionality can be wrapped
    // in a .NET class for easy consumption. The associated JavaScript module is
    // loaded on demand when first needed.
    //
    // This class can be registered as scoped DI service and then injected into Blazor
    // components for use.

    public class RTCPeerConnection : IAsyncDisposable
    {
        private readonly Lazy<Task<IJSObjectReference>> moduleTask;
        private IJSUnmarshalledRuntime unmarshalledRuntime;
        private IJSObjectReference connection;
        private IJSUnmarshalledObjectReference connectionReference;
        private DotNetObjectReference<RTCPeerConnection> myRef;
        private IJSRuntime jsRuntime;
        public event EventHandler<RTCIceCandidate> onicecandidate;
        public event EventHandler<RTCDataChannel> ondatachannel;
        public event EventHandler<string> onsignalingstatechange;
        public ConnectionState ConnectionState { get; private set; }
        public string SignalingState { get; private set; }
        public List<RTCDataChannel> DataChannels { get; } = new List<RTCDataChannel>();
        private bool isWebAssembly;
        private string connectionId;

        public RTCPeerConnection(IJSRuntime jsRuntime)
        {
            this.jsRuntime = jsRuntime;
            isWebAssembly = jsRuntime is IJSInProcessRuntime;
            if (isWebAssembly)
            {
                unmarshalledRuntime = (IJSUnmarshalledRuntime)jsRuntime;
            }
            moduleTask = new(() => jsRuntime.InvokeAsync<IJSObjectReference>(
                   "import", "./_content/Blazor.WebRTC/peerConnection.js").AsTask());
        }

        public async ValueTask<RTCSessionDescriptionInit> createOffer()
        {
            await Init();
            return await connection.InvokeAsync<RTCSessionDescriptionInit>("createOffer");
        }

        public async ValueTask<RTCSessionDescriptionInit> createAnswer()
        {
            await Init();
            return await connection.InvokeAsync<RTCSessionDescriptionInit>("createAnswer");
        }

        private async ValueTask Init()
        {
            if (connection == null)
            {
                this.myRef = DotNetObjectReference.Create(this);
                var module = await moduleTask.Value;
                connection = await module.InvokeAsync<IJSObjectReference>("create", myRef);
                connectionId = await connection.InvokeAsync<string>("add");
                if (isWebAssembly)
                {
                    connectionReference = unmarshalledRuntime.InvokeUnmarshalled<InteropStruct, IJSUnmarshalledObjectReference>("window.MyNamespace.returnJSObjectReference", new InteropStruct()
                    {
                        Id = connectionId
                    });
                }
            }
        }

        public async ValueTask setLocalDescription(RTCSessionDescriptionInit sdpMessage)
        {
            await Init();
            await connection.InvokeVoidAsync("setLocalDescription", sdpMessage);
        }

        public async ValueTask setRemoteDescription(RTCSessionDescriptionInit sdpMessage)
        {
            await Init();
            await connection.InvokeVoidAsync("setRemoteDescription", sdpMessage);
        }

        public async ValueTask addIceCandidate(RTCIceCandidate ice)
        {
            await Init();
            await connection.InvokeVoidAsync("addIceCandidate", ice);
        }

        public async ValueTask<RTCDataChannel> createDataChannel(string label)
        {
            await Init();
            var dataChannel = AddChannel(label);
            if (connectionReference != null)
            {
                Console.WriteLine("createDataChannelRef");
                connectionReference.InvokeUnmarshalled<InteropStruct,object>("createDataChannelRef", new InteropStruct()
                {
                    Id = label
                });
                var dc = unmarshalledRuntime.InvokeUnmarshalled<InteropStruct, IJSUnmarshalledObjectReference>("window.MyNamespace.dataChannelRef", new InteropStruct()
                {
                    Id = connectionId
                });
                await connection.InvokeVoidAsync("updateDataChannel", label, dataChannel.Ref);
                Console.WriteLine(dc?.ToString());
                dataChannel.SetJsObject(dc);
            }
            else
            {
                var dc = await connection.InvokeAsync<IJSObjectReference>("createDataChannel", label, dataChannel.Ref);
                dataChannel.SetJsObject(dc);
            }
            return dataChannel;
        }

        [JSInvokable]
        public async ValueTask<DotNetObjectReference<RTCDataChannel>> _getDataChannel(string label)
        {
            await Init();
            var dc = AddChannel(label);
            return dc.Ref;
        }

        private RTCDataChannel AddChannel(string label)
        {
            RTCDataChannel dc = this.DataChannels.Where(n => n.Label == label).FirstOrDefault();
            if (dc == null)
            {
                dc = new RTCDataChannel();
                dc.Label = label;
                DataChannels.Add(dc);
            }
            return dc;
        }

        [JSInvokable]
        public ValueTask _onicecandidate(RTCIceCandidate ice)
        {
            onicecandidate?.Invoke(this, ice);
            return ValueTask.CompletedTask;
        }

        [JSInvokable]
        public async ValueTask _ondatachannel(string label)
        {
            var dcRef = await connection.InvokeAsync<IJSObjectReference>("getDataChannel", label);

            var dc = AddChannel(label);
            dc.SetJsObject(dcRef);
            var state = await dc.GetReadyState();
            ondatachannel?.Invoke(this, dc);
        }

        [JSInvokable]
        public ValueTask _onconnectionstatechange(string connectionState)
        {
            this.ConnectionState = Enum.Parse<ConnectionState>(connectionState, true);
            return ValueTask.CompletedTask;
        }

        [JSInvokable]
        public ValueTask _onsignalingstatechange(string signalingState)
        {
            this.SignalingState = signalingState;
            onsignalingstatechange?.Invoke(this, signalingState);
            return ValueTask.CompletedTask;
        }


        public async ValueTask DisposeAsync()
        {
            if (moduleTask.IsValueCreated)
            {
                var module = await moduleTask.Value;
                await module.DisposeAsync();
            }
        }
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct InteropStruct
    {
        [FieldOffset(0)]
        public string Id;
    }
}
