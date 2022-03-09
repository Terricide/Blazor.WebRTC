using System.Collections.Concurrent;
using System.Runtime.InteropServices.JavaScript;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.WebRTC
{

	/// <summary>
	/// Provides a client for connecting to WebSocket services.
	/// </summary>
	public sealed class RTCPeerConnection : IDisposable
	{
		public event EventHandler<RTCIceCandidateInit> OnIceCandidate;
		public event EventHandler<RTCDataChannel> OnDataChannel;
		public event EventHandler OnNegotiationNeeded;
		public event EventHandler OnConnectionstatechange;

		private TaskCompletionSource<bool> tcsClose;

		private JSObject innerRtcPeerConnection;

		private Action<JSObject> onIcecandidate;
		private Action<JSObject> onDataChannel;
		private Action<JSObject> onNegotiationNeeded;
		private Action<JSObject> onConnectionstatechange;
		private Action<JSObject> onIcegatheringstatechange;
		private Action<JSObject> onIcecandidateerror;
		private Action<JSObject> onIceconnectionstatechange;
		private Action<JSObject> onSignalingstatechange;


		private readonly CancellationTokenSource cts;

		// Stages of this class. 
		private int state;
		private const int created = 0;
		private const int connecting = 1;
		private const int connected = 2;
		private const int disposed = 3;

		/// <summary>
		/// Initializes a new instance of the <see cref="T:WebAssembly.Net.WebSockets.ClientWebSocket"/> class.
		/// </summary>
		public RTCPeerConnection(RTCConfiguration configuration = null)
		{	
			state = created;
			cts = new CancellationTokenSource();

			if (configuration == null)
			{
				innerRtcPeerConnection = new HostObject("RTCPeerConnection");
			}
			else
            {
				innerRtcPeerConnection = new HostObject("RTCPeerConnection", configuration);
			}

			onIcecandidate = new Action<JSObject>((e) =>
            {
				//var json = e.Invoke("toJSON");
				var c = e.GetObjectProperty("candidate") as JSObject;
				RTCIceCandidate iceCandidate = null;
				if (c != null)
                {
					iceCandidate = new RTCIceCandidate();
					iceCandidate.candidate = c?.GetObjectProperty("candidate") as string;
					iceCandidate.sdpMid = c?.GetObjectProperty("sdpMid") as string;
					iceCandidate.sdpMLineIndex = (int)c?.GetObjectProperty("sdpMLineIndex");
				}
				OnIceCandidate?.Invoke(this, new RTCIceCandidateInit()
				{
					candidate = iceCandidate
				});
				e.Dispose();
            });

            // Attach the onError callback
            innerRtcPeerConnection.SetObjectProperty("onicecandidate", onIcecandidate);

			// Setup the onOpen callback
			onDataChannel = new Action<JSObject>((evt) =>
            {
				var channel = (JSObject)evt.GetObjectProperty("channel");
				var dc = new RTCDataChannel(channel);
				OnDataChannel?.Invoke(this, dc);
				evt.Dispose();
            });

            // Attach the onOpen callback
            innerRtcPeerConnection.SetObjectProperty("ondatachannel", onDataChannel);

			onNegotiationNeeded = new Action<JSObject>((e) =>
			{
				OnNegotiationNeeded?.Invoke(this, EventArgs.Empty);
				e.Dispose();
			});

			// Attach the onError callback
			innerRtcPeerConnection.SetObjectProperty("onnegotiationneeded", onNegotiationNeeded);

			onConnectionstatechange = new Action<JSObject>((e) =>
			{
				OnConnectionstatechange?.Invoke(this, EventArgs.Empty);
				e.Dispose();
			});

			// Attach the onError callback
			innerRtcPeerConnection.SetObjectProperty("onconnectionstatechange", onConnectionstatechange);

			onIcegatheringstatechange = new Action<JSObject>((e) =>
			{
				var s = this.iceGatheringState;
				e.Dispose();
			});

			// Attach the onError callback
			innerRtcPeerConnection.SetObjectProperty("onicegatheringstatechange", onIcegatheringstatechange);

			onIcecandidateerror = new Action<JSObject>((e) =>
			{
				e.Dispose();
			});

			innerRtcPeerConnection.Invoke("addEventListener", "icecandidateerror", onIcecandidateerror);


			onIceconnectionstatechange = new Action<JSObject>((e) =>
			{
				e.Dispose();
			});

			innerRtcPeerConnection.Invoke("addEventListener", "icecandidateerror", onIceconnectionstatechange);

			onSignalingstatechange = new Action<JSObject>((e) =>
			{
				Console.WriteLine("state:" + this.SignalingState);
				e.Dispose();
			});

			innerRtcPeerConnection.Invoke("addEventListener", "signalingstatechange", onSignalingstatechange);
		}

		#region Properties

		public string iceGatheringState
        {
			get
            {
				return innerRtcPeerConnection.GetObjectProperty("iceGatheringState") as string;
            }
        }

		public RTCDataChannel createDataChannel(string label)
        {
			Console.WriteLine("Creating datachannel:" + label);
			var dc = innerRtcPeerConnection.Invoke("createDataChannel", label) as JSObject;
			var dataChannel = new RTCDataChannel(dc);
			Console.WriteLine("Done creating datachannel:" + label);
			return dataChannel;
		}

		public async Task<RTCSessionDescription> createOffer()
        {
			var task = (Task<object>)innerRtcPeerConnection.Invoke("createOffer");

			var offer = await task as JSObject;

			var init = new RTCSessionDescription(offer);

			return init;
		}

		public async Task<RTCSessionDescription> createAnswer()
        {
			var task = (Task<object>)innerRtcPeerConnection.Invoke("createAnswer");

			var answer = await task as JSObject;

			var init = new RTCSessionDescription(answer);

			return init;
		}

		public RTCSessionDescription currentLocalDescription
        {
			get
            {
				var obj = this.innerRtcPeerConnection.GetObjectProperty("currentLocalDescription");
				var jsObj = obj as JSObject;
				return new RTCSessionDescription(jsObj);
            }
        }

		public RTCSessionDescription localDescription
		{
			get
			{
				var jsObj = this.innerRtcPeerConnection.GetObjectProperty("localDescription") as JSObject;
				return new RTCSessionDescription(jsObj);
			}
		}

		public RTCSessionDescription pendingLocalDescription
		{
			get
			{
				var jsObj = this.innerRtcPeerConnection.GetObjectProperty("pendingLocalDescription") as JSObject;
				return new RTCSessionDescription(jsObj);
			}
		}

		public RTCSessionDescription currentRemoteDescription
		{
			get
			{
				var jsObj = this.innerRtcPeerConnection.GetObjectProperty("currentRemoteDescription") as JSObject;
				return new RTCSessionDescription(jsObj);
			}
		}

		public async Task setLocalDescription(RTCSessionDescription init)
        {
			var task = (Task<object>)innerRtcPeerConnection.Invoke("setLocalDescription", init.HostObject);
			var res = await task;
		}

		public async Task setLocalDescription()
		{
			var task = (Task<object>)innerRtcPeerConnection.Invoke("setLocalDescription");
			var res = await task;
		}

		public async Task setRemoteDescription(RTCSessionDescriptionInit init)
		{
			var desc = new HostObject("RTCSessionDescription", init);
			desc.SetObjectProperty("type", init.type.ToString().ToLower());
			desc.SetObjectProperty("sdp", init.sdp);
			var task = (Task<object>)innerRtcPeerConnection.Invoke("setRemoteDescription", desc);
			await task;
		}

		public async Task setRemoteDescription(RTCSessionDescription init)
		{
			var desc = new HostObject("RTCSessionDescription");
			desc.SetObjectProperty("type", init.type.ToString().ToLower());
			desc.SetObjectProperty("sdp", init.sdp);
			var task = (Task<object>)innerRtcPeerConnection.Invoke("setRemoteDescription", desc);
			await task;
		}

		public async Task addIceCandidate(RTCIceCandidate candidate)
        {
			var json = (JSObject)System.Runtime.InteropServices.JavaScript.Runtime.GetGlobalObject("JSON");
			var desc = json.Invoke("parse", JsonSerializer.Serialize(candidate));
			var task = (Task<object>)innerRtcPeerConnection.Invoke("addIceCandidate", desc);
			await task;
		}

		public RTCSignalingState SignalingState
        {
			get
            {
				var s = innerRtcPeerConnection.GetObjectProperty("signalingState") as string;
				switch(s)
                {
					case "have-local-offer":
						return RTCSignalingState.HaveLocalOffer;
					case "have-remote-offer":
						return RTCSignalingState.HaveRemoteOffer;
					case "have-local-pranswer":
						return RTCSignalingState.HaveLocalPranswer;
					case "have-remote-pranswer":
						return RTCSignalingState.HaveRemotePranswer;
					case "closed":
						return RTCSignalingState.Closed;
					default:
						return RTCSignalingState.Stable;
				}
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
			if (onIcecandidate != null)
			{
				innerRtcPeerConnection.SetObjectProperty("onicecandidate", "");
				System.Runtime.InteropServices.JavaScript.Runtime.FreeObject(onIcecandidate);
			}
			if (onDataChannel != null)
			{
				innerRtcPeerConnection.SetObjectProperty("ondatachannel", "");
				System.Runtime.InteropServices.JavaScript.Runtime.FreeObject(onDataChannel);
			}
			if (onNegotiationNeeded != null)
			{
				innerRtcPeerConnection.SetObjectProperty("onnegotiationNeeded", "");
				System.Runtime.InteropServices.JavaScript.Runtime.FreeObject(onNegotiationNeeded);
			}
			if (onConnectionstatechange != null)
			{
				innerRtcPeerConnection.SetObjectProperty("ononnectionstatechange", "");
				System.Runtime.InteropServices.JavaScript.Runtime.FreeObject(onConnectionstatechange);
			}
			if (onIcegatheringstatechange != null)
			{
				innerRtcPeerConnection.SetObjectProperty("onicegatheringstatechange", "");
				System.Runtime.InteropServices.JavaScript.Runtime.FreeObject(onIcegatheringstatechange);
			}
			if (onIcecandidateerror != null)
			{
				innerRtcPeerConnection.SetObjectProperty("onicecandidateerror", "");
				System.Runtime.InteropServices.JavaScript.Runtime.FreeObject(onIcecandidateerror);
			}
			if (onIceconnectionstatechange != null)
			{
				innerRtcPeerConnection.SetObjectProperty("oniceconnectionstatechange", "");
				System.Runtime.InteropServices.JavaScript.Runtime.FreeObject(onIceconnectionstatechange);
			}
			if (onSignalingstatechange != null)
			{
				innerRtcPeerConnection.SetObjectProperty("onsignalingstatechange", "");
				System.Runtime.InteropServices.JavaScript.Runtime.FreeObject(onSignalingstatechange);
			}
			innerRtcPeerConnection?.Dispose();
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

        #endregion
    }
}
