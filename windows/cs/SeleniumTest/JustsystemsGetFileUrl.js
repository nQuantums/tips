return (() => {
    // ダウンロードのボタンを探す
    let a = document.documentElement.querySelector('li.btn_dl a[href]');
    if (!a) {
        return '';
    }

    // ボタンの href から呼び出し関数取得
    let matchs = a.href.match(/javascript\:([^\(]+)\(([^)]+)\)/);
    if (!matchs) {
        return '';
    }

    // 関数定義と呼び出しパラメータ取得
    let funcName = matchs[1];
    let funcDef = eval(matchs[1]).toString();
    let params = matchs[2];

    // 関数定義を引数で渡された文字列をURLに合成して返す様に書き換える
    funcDef = funcDef.replace(/(.*)var win *= *window\.open\(([a-zA-Z0-9_]+)[^;]*;(.*)/g, '$1return $2;$3');
    eval(funcDef);

    // 書き換えた関数を呼び出す
    return eval(funcName + '(' + params + ')');
})();
