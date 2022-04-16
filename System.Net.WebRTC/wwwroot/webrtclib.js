let rtcConnection;
let thisInstance;
var config = {
    iceServers: [
        {
            // Single url
            urls: ["stun:stun.l.google.com:19302"]
        },
        {
            // List of urls and credentials
            urls: ["stun:stun.stunprotocol.org:3478"],
        },
        {
            // List of urls and credentials
            urls: ["stun:stun.anyfirewall.com:3478"],
        },
    ],
};

export function createRTCSessionDescription(type, sdp) {
    return new RTCSessionDescription({ type: type, sdp: sdp });
}

export function init(obj) {
    thisInstance = obj;
    window.CreateRTCSessionDescription = (type, sdp, rtc) => {
        return new RTCSessionDescription({ type: type, sdp: sdp });
    };
    window.CreatePeerConnection = () => {
        thisInstance = new RTCPeerConnection(config);
        return thisInstance;
    };
}