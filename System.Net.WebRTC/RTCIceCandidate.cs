namespace System.Net.WebRTC
{
    public class RTCIceCandidate
    {
        public string candidate { get; set; }
        public string sdpMid { get; set; }
        public int sdpMLineIndex { get; set; }
    }
}
