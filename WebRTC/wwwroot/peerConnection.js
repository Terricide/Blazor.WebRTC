var __awaiter = (this && this.__awaiter) || function (thisArg, _arguments, P, generator) {
    function adopt(value) { return value instanceof P ? value : new P(function (resolve) { resolve(value); }); }
    return new (P || (P = Promise))(function (resolve, reject) {
        function fulfilled(value) { try { step(generator.next(value)); } catch (e) { reject(e); } }
        function rejected(value) { try { step(generator["throw"](value)); } catch (e) { reject(e); } }
        function step(result) { result.done ? resolve(result.value) : adopt(result.value).then(fulfilled, rejected); }
        step((generator = generator.apply(thisArg, _arguments || [])).next());
    });
};
var DotNet;
window.MyNamespace = window.MyNamespace || {};
window.Blazor = window.Blazor || {};
var BlazorWebRTC;
(function (BlazorWebRTC) {
    class PeerConnection {
        constructor() {
            this.datachannels = {};
            this.Id = this.uuidv4();
        }
        init(helper) {
            return __awaiter(this, void 0, void 0, function* () {
                this.helper = helper;
                this.localConnection = new RTCPeerConnection();
                this.localConnection.onicecandidate = (evt) => __awaiter(this, void 0, void 0, function* () {
                    yield helper.invokeMethodAsync("_onicecandidate", evt.candidate);
                });
                this.localConnection.ondatachannel = (evt) => __awaiter(this, void 0, void 0, function* () {
                    var objRef = yield helper.invokeMethodAsync("_getDataChannel", evt.channel.label);
                    var dc = new PeerConnectionDataChannel(evt.channel, objRef.result);
                    this.datachannels[evt.channel.label] = dc;
                    yield helper.invokeMethodAsync('_ondatachannel', evt.channel.label);
                });
                this.localConnection.onconnectionstatechange = (evt) => __awaiter(this, void 0, void 0, function* () {
                    yield helper.invokeMethodAsync('_onconnectionstatechange', this.localConnection.connectionState);
                });
                this.localConnection.onsignalingstatechange = (evt) => __awaiter(this, void 0, void 0, function* () {
                    yield helper.invokeMethodAsync('_onsignalingstatechange', this.localConnection.signalingState);
                });
            });
        }
        getId() {
            return __awaiter(this, void 0, void 0, function* () {
                return this.Id;
            });
        }
        createOffer() {
            return __awaiter(this, void 0, void 0, function* () {
                return yield this.localConnection.createOffer();
            });
        }
        createAnswer() {
            return __awaiter(this, void 0, void 0, function* () {
                return yield this.localConnection.createAnswer();
            });
        }
        setLocalDescription(sdp) {
            return __awaiter(this, void 0, void 0, function* () {
                yield this.localConnection.setLocalDescription(sdp);
            });
        }
        setRemoteDescription(sdp) {
            return __awaiter(this, void 0, void 0, function* () {
                yield this.localConnection.setRemoteDescription(sdp);
            });
        }
        createDataChannel(label, helper) {
            var dc = this.localConnection.createDataChannel(label);
            var peerDc = new PeerConnectionDataChannel(dc, helper);
            this.datachannels[label] = peerDc;
            return peerDc;
        }
        createDataChannelRef(label) {
            const name = window.Blazor.platform.readStringField(label, 0);
            var dc = this.localConnection.createDataChannel(name);
            var peerDc = new PeerConnectionDataChannel(dc, null);
            this.datachannels[name] = peerDc;
            return peerDc;
        }
        updateDataChannel(label, helper) {
            var dc = this.datachannels[label];
            dc.setHelper(helper);
        }
        getDataChannel(label) {
            return this.datachannels[label];
        }
        addIceCandidate(ice) {
            this.localConnection.addIceCandidate(ice).catch(e => {
                console.log("Failure during addIceCandidate(): " + e.name);
            });
        }
        uuidv4() {
            return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, function (c) {
                var r = Math.random() * 16 | 0, v = c == 'x' ? r : (r & 0x3 | 0x8);
                return v.toString(16);
            });
        }
        add() {
            return __awaiter(this, void 0, void 0, function* () {
                if (!(window.MyNamespace.peerConnections instanceof Array)) {
                    window.MyNamespace.peerConnections = [];
                }
                window.MyNamespace.peerConnections[this.Id] = this;
                return this.Id;
            });
        }
    }
    BlazorWebRTC.PeerConnection = PeerConnection;
    class PeerConnectionDataChannel {
        constructor(dc, helper) {
            this.dataChannel = dc;
            this.helper = helper;
            this.setHelper(helper);
        }
        setHelper(helper) {
            this.helper = helper;
            this.dataChannel.onopen = (ev) => __awaiter(this, void 0, void 0, function* () {
                yield helper.invokeMethodAsync("_onopen");
            });
            this.dataChannel.onclose = (ev) => __awaiter(this, void 0, void 0, function* () {
                yield helper.invokeMethodAsync("_onclose");
            });
            this.dataChannel.onmessage = (ev) => __awaiter(this, void 0, void 0, function* () {
                if (ev.data instanceof ArrayBuffer) {
                    var u8 = new Uint8Array(ev.data);
                    //var decoder = new TextDecoder('utf8');
                    //var text = decoder.decode(u8);
                    //var b64encoded = btoa(text);
                    yield helper.invokeMethod("_ondatamessage", u8);
                }
                else {
                    yield helper.invokeMethodAsync("_onmessage", ev.data);
                }
            });
        }
        getReadyState() {
            return this.dataChannel.readyState;
        }
        send(data) {
            var data = window.Blazor.platform.readStringField(data, 0);
            if (data.isText) {
                this.dataChannel.send(data.Text);
            }
            else {
                var buffer = this._base64ToArrayBuffer(data.data);
                this.dataChannel.send(buffer);
            }
        }
        sendData(data) {
            var buffer = window.Blazor.platform.toUint8Array(data);
            this.dataChannel.send(buffer);
        }
        _base64ToArrayBuffer(base64) {
            var binary_string = window.atob(base64);
            var len = binary_string.length;
            var bytes = new Uint8Array(len);
            for (var i = 0; i < len; i++) {
                bytes[i] = binary_string.charCodeAt(i);
            }
            return bytes.buffer;
        }
    }
    BlazorWebRTC.PeerConnectionDataChannel = PeerConnectionDataChannel;
})(BlazorWebRTC || (BlazorWebRTC = {}));
var connection;
export function create(dotnetHelper) {
    connection = new BlazorWebRTC.PeerConnection();
    connection.init(dotnetHelper);
    return connection;
}
window.MyNamespace.returnJSObjectReference = (id) => {
    const name = window.Blazor.platform.readStringField(id, 0);
    var conn = window.MyNamespace.peerConnections[name];
    return conn;
};
window.MyNamespace.dataChannelRef = (id) => {
    const name = window.Blazor.platform.readStringField(id, 0);
    var conn = window.MyNamespace.peerConnections[name];
    var dc = conn.getDataChannel("default");
    return dc;
};
//# sourceMappingURL=peerConnection.js.map