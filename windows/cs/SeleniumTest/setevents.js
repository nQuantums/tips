console.log(document.readyState);
console.log(arguments);

if (!window.seleniumWindowHandle) {
    window.seleniumWindowHandle = arguments[0];

    var fetcher = async(jsonData) => {
        var params = '?';
        var first = true;
        for (var p in jsonData) {
            if (first) {
                first = false;
            } else {
                params += '&';
            }
            params += p + '=';
            params += jsonData[p];
        }
        var url = arguments[1] + params;
        console.log(url)
        const response = await fetch(url);
        if (response) {
            var text = await response.text();
            console.log(text);
        }
    };

    // JavaScriptでsleepする方法
    // ビジーwaitを使う方法
    function sleep(waitMsec) {
        var startMsec = new Date();
        // 指定ミリ秒間、空ループ。CPUは常にビジー。
        while (new Date() - startMsec < waitMsec);
    }

    window.addEventListener('unload', (e) => {
        fetcher({ event: 'unload', handle: window.seleniumWindowHandle });
        // 非同期処理でHTTPリクエストしてるが完了を待たずにドキュメント終了してしまうので仕方ないのでウェイトを入れる
        sleep(100);
    });

    window.addEventListener('visibilitychange', () => {
        fetcher({ event: 'visibilitychange', handle: window.seleniumWindowHandle, visibilityState: document.visibilityState });
    });

    window.addEventListener('focus', () => {
        fetcher({ event: 'focus', handle: window.seleniumWindowHandle });
    });

    window.addEventListener('blur', () => {
        fetcher({ event: 'blur', handle: window.seleniumWindowHandle });
    });
} else {

}