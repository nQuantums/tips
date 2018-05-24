var serverSocketId;
 
/**
 * サーバ起動
 */
chrome.sockets.tcpServer.create({}, function(createInfo) {
    // サーバ用のソケット
    serverSocketId = createInfo.socketId;
 
    // 3000番ポートをlisten
    chrome.sockets.tcpServer.listen(serverSocketId, '0.0.0.0', 3000, function(resultCode) {
        if (resultCode < 0) {
            console.log("Error listening:" + chrome.runtime.lastError.message);
        }
    });
});
 
/**
 * リクエスト用ソケット作成
 */
chrome.sockets.tcpServer.onAccept.addListener(function(info) {
    if (info.socketId === serverSocketId) {
        chrome.sockets.tcp.setPaused(info.clientSocketId, false);
    }
});
 
/**
 * リクエスト受信
 */
chrome.sockets.tcp.onReceive.addListener(function(info) {
    console.log("Receive: ", info);
 
    // リクエスト確認: ArrayBufferを文字列に変換
    // 本来はヘッダの先頭と、Content-Length等からリクエストの範囲を検出し、
    // 受信データからHTTPリクエストを取り出す必要がある
    var requestText = ab2str(info.data);
    console.log(requestText);
 
    // レスポンス送信
    var socketId = info.socketId;
    var message = 'Hello world';
    var responseText = [
        ' HTTP/1.1 200 OK',
        'Content-Type: text/plain',
        'Content-Length: ' + message.length,
        '',
        message
    ].join("\n");
    chrome.sockets.tcp.send(socketId, str2ab(responseText), function(info) {
        if (info.resultCode < 0) {
            console.log("Error sending:" + chrome.runtime.lastError.message);
        }
 
        // ソケット破棄
        chrome.sockets.tcp.disconnect(socketId);
        chrome.sockets.tcp.close(socketId);
    });
});
 
/**
 * データ受信エラー
 */
chrome.sockets.tcp.onReceiveError.addListener(function(info) {
    console.log("Error: ", info);
});
 
/**
 * 文字列をArrayBufferに変換する(ASCIIコード専用)
 *
 * @param text
 * @returns {ArrayBuffer}
 */
function str2ab(text) {
    var typedArray = new Uint8Array(text.length);
 
    for (var i = 0; i < typedArray.length; i++) {
        typedArray[i] = text.charCodeAt(i);
    }
 
    return typedArray.buffer;
}
 
/**
 * ArrayBufferを文字列に変換する(ASCIIコード専用)
 *
 * @param arrayBuffer
 * @returns {string}
 */
function ab2str(arrayBuffer) {
    var typedArray = new Uint8Array(arrayBuffer);
    var text = '';
 
    for (var i = 0; i < typedArray.length; i++) {
        text += String.fromCharCode(typedArray[i]);
    }
 
    return text;
}