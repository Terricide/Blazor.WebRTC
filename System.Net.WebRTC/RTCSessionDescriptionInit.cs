using System.Runtime.InteropServices.JavaScript;

namespace System.Net.WebRTC
{
    public class RTCSessionDescriptionInit : IDisposable
    {
        public JSObject HostObject;
        public RTCSessionDescriptionInit(JSObject hostObj)
        {
            this.HostObject = hostObj;
        }
        public RTCSdpType type
        {
            get
            {
                var str = HostObject.GetObjectProperty("type") as string;
                Enum.TryParse<RTCSdpType>(str, true, out var r);
                return r;
            }
        }
        public string sdp
        {
            get
            {
                return HostObject.GetObjectProperty("sdp") as string;
            }
        }

        public void Dispose()
        {
            HostObject?.Dispose();
        }
    }

    public enum RTCSdpType
    {
        Offer,
        Answer,
        Pranswer,
        Rollback
    }
}
