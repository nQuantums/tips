console.log(document.readyState);
console.log(arguments);

if (!window.seleniumWindowHandle) {
    window.seleniumWindowHandle = arguments[0];

    /**
     * 呼び出し元プログラムが用意したHTTPサーバーへGET要求を送る
     * @param {*} paramsJson HTTPサーバーへ送るパラメータ
     */
    async function fetcher(paramsJson) {
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

    /**
     * 呼び出し元プログラムが用意したHTTPサーバーへPOST要求を送る
     * @param {*} paramsJson HTTPサーバーへ送るパラメータ
     * @param {*} bodyData ボディーデータ
     */
    async function fetcherPost(paramsJson, bodyData) {
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

    /**
     * 指定時間経過するまでループ処理にて待つ
     * @param {Number} waitMsec ミリ秒単位での待ち時間
     */
    function sleep(waitMsec) {
        let startMsec = new Date();
        // 指定ミリ秒間、空ループ。CPUは常にビジー。
        while (new Date() - startMsec < waitMsec);
    }

    async function detectText(e) {
        document.clickedElement = e;
        let text = e.innerText || e.textContent;
        while (text.length < 128) {
            let p = e.parentElement;
            if (p) {
                e = p;
                text = text = e.textContent || e.innerText;
            } else {
                break;
            }
        }
        let result = await fetcherPost({ event: 'click', handle: window.seleniumWindowHandle }, text);
        console.log(result);
        console.log(JSON.parse(result));
    }

    /**
     * 指定テーブル要素の内容を解析
     * @param {Element} t テーブル要素
     * @return {Object} rowspan,colspan を考慮した上での行数ｘ列数の配列でのセル内容
     */
    function analyzeTable(t) {
        // まず見えている行を取得しながら最大列数を数える
        let rows = t.querySelectorAll('tr');
        let cellElems = [];
        let colCount = 0;
        rows.forEach(r => {
            if (!r.hidden) {
                let cc = 0;
                let colElems = [];
                let cols = r.querySelectorAll('th, td');
                cols.forEach(c => {
                    if (!c.hidden) {
                        colElems.push(c);
                        cc += 1 || c.colspan;
                    }
                });
                if (colCount < cc) {
                    colCount = cc;
                }
                cellElems.push(colElems);
            }
        });

        // 行数ｘ列数の配列を作成する
        let rowCount = cellElems.length;
        let cells = [];
        for (let ir = 0; ir < rowCount; ir++) {
            cells.push(new Array(colCount));
        }

        // 行数ｘ列数の配列にセルの内容をセットしていく
        for (let ir = 0; ir < rowCount; ir++) {
            let colElems = cellElems[ir];
            for (let i = 0, ic = 0; i < colElems.length; i++) {
                let col = colElems[i];
                let links = [];
                col.querySelectorAll('a[href]').forEach(a => {
                    let href = a.href;
                    if (href && !href.startsWith('#')) {
                        links.push(href);
                    }
                });
                let value = { text: col.innerText || col.textContent, links: links };
                let rowspan = 1 || col.rowspan;
                let colspan = 1 || col.colspan;
                let ire = ir + rowspan;
                let ice = ic + colspan;
                for (let r = ir; r < ire; r++) {
                    for (let c = ic; c < ice; c++) {
                        cells[r][c] = value;
                    }
                }
                ic += colspan;
            }
        }

        return cells;
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
        let target = e.target || e.srcElement;

        fetcherPost({ event: 'click', handle: window.seleniumWindowHandle }, JSON.stringify(target.href || ''));

        // detectText(target);
        let t = target.closest('table');
        if (t) {
            document.clickedElement = t;
            let cells = analyzeTable(t);
            fetcherPost({ event: 'click', handle: window.seleniumWindowHandle }, JSON.stringify(cells));
        }
    }, false);
}

return JSON.stringify({
    url: document.URL,
    text: document.documentElement.innerText
});