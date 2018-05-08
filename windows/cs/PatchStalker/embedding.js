(async function() {
    await CefSharp.BindObjectAsync("HostObj", "HostObj");

    // 指定ノードが有効なリンク先を持つなら true を返す
    var isValidLink = function(node) {
        return !node.getAttribute('href').startsWith('#') && !node.href.startsWith('javascript');
    }

    // 指定ノードが td、th 内のものならノードを返す、それ以外なら null
    var getTd = function(node) {
        var p = node.parentElement;
        var nodeName = p.nodeName.toLowerCase();
        return nodeName === 'td' || nodeName === 'th' ? p : null;
    }

    // リンク取得処理開始を通知
    HostObj.start();

    // ページ全体のテキストを取得
    HostObj.setInnerText(document.documentElement.innerText);

    // 関連ノードのID
    var nodesMap = new Map();

    // 全リンクから有効なリンクを取得
    var allLinks = document.links;
    var linksMap = new Map(); // ノードがキーで値がID
    var links = [];
    for (var i = 0; i < allLinks.length; i++) {
        var node = allLinks[i];
        if (isValidLink(node)) {
            linksMap.set(node, linksMap.size);
            links.push(node);
        }
    }

    // 全リンクをID、関連キーワードと共に登録していく
    for (var [node, id] of linksMap) {
        HostObj.addLink(id, node.href, node.textContent.replace(/^\s+|\s+$/g, ''));
    }

    // リンクを包含するテーブル一覧の取得
    var tables = new Map();
    for (var [node, id] of linksMap) {
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
                        var cellNodeId = nodesMap.size;
                        nodesMap.set(this, cellNodeId);

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

                        var col = [];
                        col.push(nodeName);
                        col.push(cellNodeId);
                        col.push(r);
                        col.push(c);
                        col.push($td.text());
                        $td.find(links).each(function() {
                            col.push(linksMap.get(this));
                        });

                        cols.push(col);
                    });
                    cells.push(cols);
                });

                HostObj.setTable(id, cells);
            }
        }
    }

    HostObj.end();
})();