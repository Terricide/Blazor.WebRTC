using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Blazor.WebRTC
{
    public class RTCSessionDescriptionInit
    {
        public string Type { get; set; }
        public string Sdp { get; set; }
    }
}
