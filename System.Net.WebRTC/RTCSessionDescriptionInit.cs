using System.Runtime.InteropServices.JavaScript;

namespace System.Net.WebRTC
{
    public class RTCSessionDescriptionInit 
    {
        public RTCSdpType type { get; set; }
        public string sdp { get; set; }
    }

    public enum RTCSdpType
    {
        Offer,
        Answer,
        Pranswer,
        Rollback
    }
}
