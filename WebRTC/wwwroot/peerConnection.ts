var DotNet: any;

declare global {
    interface Window { MyNamespace: any; Blazor: any; }
}

window.MyNamespace = window.MyNamespace || {};
window.Blazor = window.Blazor || {};

namespace BlazorWebRTC {
    export class PeerConnection {
        public helper;
        private Id: string;
        private localConnection: RTCPeerConnection;
        private datachannels: { [name: string]: PeerConnectionDataChannel } = {};
        constructor() {
            this.Id = this.uuidv4();
        }

        public async init(helper) {
            this.helper = helper;
            this.localConnection = new RTCPeerConnection();
            this.localConnection.onicecandidate = async (evt) => {
                await helper.invokeMethodAsync("_onicecandidate", evt.candidate);
            };
            this.localConnection.ondatachannel = async (evt) => {
                var objRef = await helper.invokeMethodAsync("_getDataChannel", evt.channel.label);
                var dc = new PeerConnectionDataChannel(evt.channel, objRef.result);
                this.datachannels[evt.channel.label] = dc;
                await helper.invokeMethodAsync('_ondatachannel', evt.channel.label);
            };
            this.localConnection.onconnectionstatechange = async (evt) => {
                await helper.invokeMethodAsync('_onconnectionstatechange', this.localConnection.connectionState);
            };
            this.localConnection.onsignalingstatechange = async (evt) => {
                await helper.invokeMethodAsync('_onsignalingstatechange', this.localConnection.signalingState);
            };
        }

        public async getId() {
            return this.Id;
        }

        public async createOffer() {
            return await this.localConnection.createOffer();
        }

        public async createAnswer() {
            
            return await this.localConnection.createAnswer();
        }

        public async setLocalDescription(sdp: RTCSessionDescriptionInit) {
            await this.localConnection.setLocalDescription(sdp);
        }

        public async setRemoteDescription(sdp: RTCSessionDescriptionInit) {
            await this.localConnection.setRemoteDescription(sdp);
        }

        public createDataChannel(label: string, helper) {
            var dc = this.localConnection.createDataChannel(label);
            var peerDc = new PeerConnectionDataChannel(dc, helper);
            this.datachannels[label] = peerDc;
            return peerDc;
        }

        public createDataChannelRef(label: string) {
            const name = window.Blazor.platform.readStringField(label, 0);
            var dc = this.localConnection.createDataChannel(name);
            var peerDc = new PeerConnectionDataChannel(dc, null);
            this.datachannels[name] = peerDc;
            return peerDc;
        }

        public updateDataChannel(label: string, helper) {
            var dc = this.datachannels[label];
            dc.setHelper(helper);
        }

        public getDataChannel(label: string) {
            return this.datachannels[label];
        }

        public addIceCandidate(ice: RTCIceCandidate) {
            this.localConnection.addIceCandidate(ice).catch(e => {
                console.log("Failure during addIceCandidate(): " + e.name);
            });
        }

        private uuidv4() {
            return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, function (c) {
                var r = Math.random() * 16 | 0, v = c == 'x' ? r : (r & 0x3 | 0x8);
                return v.toString(16);
            });
        }

        public async add() {
            if (!(window.MyNamespace.peerConnections instanceof Array)) {
                window.MyNamespace.peerConnections = [];
            }
            window.MyNamespace.peerConnections[this.Id] = this;
            return this.Id;
        }
    }

    export class PeerConnectionDataChannel {
        public helper;
        private dataChannel: RTCDataChannel;
        constructor(dc: RTCDataChannel, helper) {
            this.dataChannel = dc;
            this.helper = helper;
            this.setHelper(helper);
        }
        public setHelper(helper) {
            this.helper = helper;
            this.dataChannel.onopen = async (ev) => {
                await helper.invokeMethodAsync("_onopen");
            }
            this.dataChannel.onclose = async (ev) => {
                await helper.invokeMethodAsync("_onclose");
            }
            this.dataChannel.onmessage = async (ev) => {
                if (ev.data instanceof ArrayBuffer) {
                    var u8 = new Uint8Array(ev.data);
                    var decoder = new TextDecoder('utf8');
                    var text = decoder.decode(u8);
                    var b64encoded = btoa(text);
                    await helper.invokeMethod("_ondatamessage", b64encoded);
                }
                else {
                    await helper.invokeMethodAsync("_onmessage", ev.data);
                }
            }
        }

        public getReadyState() {
            return this.dataChannel.readyState;
        }

        public send(data) {
            var data = window.Blazor.platform.readStringField(data, 0);
            if (data.isText) {
                this.dataChannel.send(data.Text);
            }
            else {
                var buffer = this._base64ToArrayBuffer(data.data);
                this.dataChannel.send(buffer);
            }
        }

        public sendData(data) {
            var buffer = window.Blazor.platform.toUint8Array(data);
            this.dataChannel.send(buffer);
        }

        private _base64ToArrayBuffer(base64) {
            var binary_string = window.atob(base64);
            var len = binary_string.length;
            var bytes = new Uint8Array(len);
            for (var i = 0; i < len; i++) {
                bytes[i] = binary_string.charCodeAt(i);
            }
            return bytes.buffer;
        }
    }
}

var connection: BlazorWebRTC.PeerConnection;

export function create(dotnetHelper) {
    connection = new BlazorWebRTC.PeerConnection();
    connection.init(dotnetHelper);
    return connection;
}

window.MyNamespace.returnJSObjectReference = (id) => {
    const name = window.Blazor.platform.readStringField(id, 0);
    var conn = window.MyNamespace.peerConnections[name];
    return conn;
}

window.MyNamespace.dataChannelRef = (id) => {
    const name = window.Blazor.platform.readStringField(id, 0);
    var conn = window.MyNamespace.peerConnections[name];
    var dc = conn.getDataChannel("default");
    return dc;
};