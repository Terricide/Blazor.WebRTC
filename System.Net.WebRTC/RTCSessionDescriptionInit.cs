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
        public RTCSessionDescriptionInit()
        {
            this.HostObject = new HostObject("RTCSessionDescription");
        }
        public RTCSdpType type
        {
            get
            {
                var str = HostObject.GetObjectProperty("type") as string;
                Enum.TryParse<RTCSdpType>(str, true, out var r);
                return r;
            }
            set
            {
                var strType = value.ToString().ToLower();
                HostObject.SetObjectProperty("type", strType);
            }
        }
        public string sdp
        {
            get
            {
                return HostObject.GetObjectProperty("sdp") as string;
            }
            set
            {
                HostObject.SetObjectProperty("sdp", value);
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
