using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.JavaScript;
using System.Text;
using System.Threading.Tasks;

namespace System.Net.WebRTC
{
    public class RTCConfiguration
    {
        public RTCIceServer[] IceServers { get; set; }
    }

    public class RTCIceServer
    {
        public string Credential { get; set; }

        public RTCIceCredentialType? CredentialType { get; set; }

        public string[] Urls { get; set; }

        public string Username { get; set; }
    }

    public enum RTCIceCredentialType
    {
        Oauth,
        Password
    }
}
