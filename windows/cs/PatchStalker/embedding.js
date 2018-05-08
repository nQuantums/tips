(async function() {
    await CefSharp.BindObjectAsync("HostObj", "HostObj");

    // 指定ノードが有効なリンク先を持つなら true を返す
    var isValidLink = function(node) {
        return !node.getAttribute('href').startsWith('#') && !node.href.startsWith('javascript');
    }

    // 指定ノードが td 内のものなら td 要素を取得する
    var getTd = function(node) {
        var p = node.parentElement;
        var nodeName = p.nodeName.toLowerCase();
        return nodeName === 'td' || nodeName === 'th' ? p : null;
    }

    // リンク取得処理開始を通知
    HostObj.start();

    // ページ全体のテキストを取得
    HostObj.setInnerText(document.documentElement.innerText);

    // 全リンクを取得
    var allLinks = document.links;

    // リンクを包含するテーブル一覧の取得
    var tables = new Map();
    for (var i = 0; i < allLinks.length; i++) {
        var node = allLinks[i];
        if (isValidLink(node)) {
            var td = getTd(node);
            if (td) {
                var $table = $(td).closest('table');
                var table = $table.get(0);

                if (!tables.has(table)) {
                    var id = tables.size;
                    tables.set(table, id);

                    // セルのプロパティ、テキストを取得する
                    var cells = [];
                    $table.find('tr').each(function() {
                        var $tr = $(this);
                        var cols = [];
                        $tr.find('td, th').each(function() {
                            var nodeName = this.nodeName.toLowerCase();
                            var $td = $(this);
                            var rowspan = $td.attr('rowspan');
                            var colspan = $td.attr('colspan');
                            var r = 1;
                            var c = 1;
                            if (rowspan) {
                                r = +rowspan;
                            }
                            if (colspan) {
                                c = +colspan;
                            }
                            cols.push(nodeName);
                            cols.push(r);
                            cols.push(c);
                            cols.push($td.text());
                        });
                        cells.push(cols);
                    });

                    HostObj.setTable(id, cells);
                }
            }
        }
    }

    // 全リンクを取得して関連キーワードと共に登録していく
    for (var i = 0; i < allLinks.length; i++) {
        var node = allLinks[i];
        if (isValidLink(node)) {
            // TODO: リンクがテーブル内のものならテーブル内の位置も通知する
            HostObj.addLink(href, node.textContent.replace(/^\s+|\s+$/g, ''));
        }
    }

    HostObj.end();
})();