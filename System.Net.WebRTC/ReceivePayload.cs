using System.Net.WebSockets;
using System.Runtime.InteropServices.JavaScript;

namespace System.Net.WebRTC
{
    class ReceivePayload
	{

		private byte[] dataMessageReceived;
		private WebSocketMessageType messageType;
		private int unconsumedDataOffset;

		public ReceivePayload(byte[] array, WebSocketMessageType messageType = WebSocketMessageType.Binary)
		{
			dataMessageReceived = array;
			this.messageType = messageType;
		}

		public ReceivePayload(ArrayBuffer arrayBuffer, WebSocketMessageType messageType = WebSocketMessageType.Binary)
		{
			using (var bin = new Uint8Array(arrayBuffer))
			{
				dataMessageReceived = bin.ToArray();
				this.messageType = messageType;
			}

		}

		public ReceivePayload(ArraySegment<byte> payload, WebSocketMessageType messageType = WebSocketMessageType.Binary)
		{
			dataMessageReceived = payload.Array;
			this.messageType = messageType;
		}

		public bool BufferPayload(ArraySegment<byte> arraySegment, out WebRtcReceiveResult receiveResult)
		{
			var bytesTransferred = Math.Min(dataMessageReceived.Length - unconsumedDataOffset, arraySegment.Count);
			var endOfMessage = (dataMessageReceived.Length - unconsumedDataOffset) <= arraySegment.Count;

			Buffer.BlockCopy(dataMessageReceived, unconsumedDataOffset, arraySegment.Array, arraySegment.Offset, bytesTransferred);

			if (!endOfMessage)
				unconsumedDataOffset += arraySegment.Count;

			receiveResult = new WebRtcReceiveResult(bytesTransferred, messageType, endOfMessage);

			return endOfMessage;
		}
	}
}
