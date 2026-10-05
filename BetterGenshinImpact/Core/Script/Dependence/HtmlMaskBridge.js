window.htmlMask = {
    _callbacks: {},
    _seq: 0,
    request: function(url, data) {
        return new Promise(function(resolve, reject) {
            var id = '__req_' + (++window.htmlMask._seq);
            window.htmlMask._callbacks[id] = { resolve: resolve, reject: reject };
            window.chrome.webview.postMessage(JSON.stringify({
                url: url,
                data: data || {},
                requestId: id
            }));
        });
    },
    onMessage: null,
    _dispatch: function(raw) {
        try {
            var msg = JSON.parse(raw);
            if (msg.requestId && window.htmlMask._callbacks[msg.requestId]) {
                window.htmlMask._callbacks[msg.requestId].resolve(msg);
                delete window.htmlMask._callbacks[msg.requestId];
            } else if (window.htmlMask.onMessage) {
                var result = window.htmlMask.onMessage(msg);
                if (msg.requestId && result !== undefined) {
                    Promise.resolve(result).then(function(data) {
                        window.chrome.webview.postMessage(JSON.stringify({
                            requestId: msg.requestId,
                            url: '/__response__',
                            data: data
                        }));
                    });
                }
            }
        } catch(e) {
            if (window.htmlMask.onMessage) window.htmlMask.onMessage(raw);
        }
    }
};
window.chrome.webview.addEventListener('message', function(e) {
    window.htmlMask._dispatch(e.data);
});
