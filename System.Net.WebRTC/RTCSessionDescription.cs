﻿using System.Collections.Generic;
using System.Runtime.InteropServices.JavaScript;

namespace System.Net.WebRTC
{
    public class RTCSessionDescription
    {
        public JSObject HostObject;
        public RTCSessionDescription(JSObject hostObj)
        {
            this.HostObject = hostObj;
        }
        public RTCSessionDescription()
        {
            this.HostObject = new HostObject("RTCSessionDescription");
        }
        public RTCSessionDescription(RTCSdpType type, string sdp)
        {
            var strType = type.ToString().ToLower();
            var dict = new Dictionary<string, string>();
            dict.Add("type", strType);
            dict.Add("sdp", sdp);
            this.HostObject = new HostObject("RTCSessionDescription", dict);
        }
        public RTCSdpType type
        {
            get
            {
                var str = HostObject.GetObjectProperty("type") as string;
                Enum.TryParse<RTCSdpType>(str, out var r);
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
}
