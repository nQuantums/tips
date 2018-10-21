(() => {
    let fetcher = async (paramsJson) => {
        let params = '?';
        let first = true;
        for (let p in paramsJson) {
            if (first) {
                first = false;
            } else {
                params += '&';
            }
            params += p + '=';
            params += paramsJson[p];
        }
        let url = arguments[1] + params;
        const response = await fetch(url);
        if (response) {
            return await response.text();
        } else {
            return null;
        }
    };
    let fetcherPost = async (paramsJson, bodyData) => {
        let params = '?';
        let first = true;
        for (let p in paramsJson) {
            if (first) {
                first = false;
            } else {
                params += '&';
            }
            params += p + '=';
            params += paramsJson[p];
        }
        let url = arguments[1] + params;
        const response = await fetch(url, { method: 'POST', body: bodyData });
        if (response) {
            return await response.text();
        } else {
            return null;
        }
    };

    if (!window.seleniumWindowHandle) {
        window.seleniumWindowHandle = arguments[0];

        // JavaScriptでsleepする方法
        // ビジーwaitを使う方法
        function sleep(waitMsec) {
            let startMsec = new Date();
            // 指定ミリ秒間、空ループ。CPUは常にビジー。
            while (new Date() - startMsec < waitMsec);
        }

        window.addEventListener('unload', (e) => {
            fetcher({ event: 'unload', handle: window.seleniumWindowHandle });
            // 非同期処理でHTTPリクエストしてるが完了を待たずにドキュメント終了してしまうので仕方ないのでウェイトを入れる
            sleep(1000);
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

        window.requestIdleCallback(() => {
            fetcherPost({ event: 'page_after_init', handle: window.seleniumWindowHandle }, JSON.stringify({
                url: document.URL,
                title: document.title,
                id: arguments[2]
            }));
        });
    }
})();