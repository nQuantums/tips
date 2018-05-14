console.log(document.readyState);
console.log(arguments);

if (!window.seleniumWindowHandle) {
    window.seleniumWindowHandle = arguments[0];

    var fetcher = async(paramsJson) => {
        var params = '?';
        var first = true;
        for (var p in paramsJson) {
            if (first) {
                first = false;
            } else {
                params += '&';
            }
            params += p + '=';
            params += paramsJson[p];
        }
        var url = arguments[1] + params;
        const response = await fetch(url);
        if (response) {
            return await response.text();
        } else {
            return null;
        }
    };
    var fetcherPost = async(paramsJson, bodyData) => {
        var params = '?';
        var first = true;
        for (var p in paramsJson) {
            if (first) {
                first = false;
            } else {
                params += '&';
            }
            params += p + '=';
            params += paramsJson[p];
        }
        var url = arguments[1] + params;
        const response = await fetch(url, { method: 'POST', body: bodyData });
        if (response) {
            return await response.text();
        } else {
            return null;
        }
    };

    // JavaScriptでsleepする方法
    // ビジーwaitを使う方法
    function sleep(waitMsec) {
        var startMsec = new Date();
        // 指定ミリ秒間、空ループ。CPUは常にビジー。
        while (new Date() - startMsec < waitMsec);
    }

    async function detectText(e) {
        document.clickedElement = e;
        var text = e.innerText || e.textContent;
        while (text.length < 128) {
            var p = e.parentElement;
            if (p) {
                e = p;
                text = text = e.textContent || e.innerText;
            } else {
                break;
            }
        }
        var result = await fetcherPost({ event: 'click', handle: window.seleniumWindowHandle }, text);
        console.log(result);
        console.log(JSON.parse(result));
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

    document.addEventListener('click', function(e) {
        e = e || window.event;
        var target = e.target || e.srcElement;
        detectText(target);
    }, false);
} else {

}