(async function() {
    await CefSharp.BindObjectAsync("HostObj", "HostObj");

    // ページ全体のテキストを取得
    HostObj.setInnerText(document.documentElement.innerText);

    // 全リンクを取得して関連キーワードと共に登録していく
    var allLinks = document.links;
    HostObj.startAddLink();
    for (var i = 0; i < allLinks.length; i++) {
        var node = allLinks[i];
        var hrefOrg = node.getAttribute('href');
        var href = node.href;
        if (!hrefOrg.startsWith('#') && !href.startsWith('javascript')) {
            // リンクが td 内にあるなら同じ行、同じ列からヘッダを取得しキーワードを追加する
            // 公開日時やバージョンなどの情報が取得できることもある
            var a = node.parentElement;
            if (a.nodeName.toLowerCase() == 'td') {
                var $td = $(a);

                // TODO: テーブルの構造を取得＆解析しリンクに関連するキーワードをタグ付きで取得する
                var $table = $td.closest('table');
                var rowCount = 0;
                var colCount = 0;

                $table.find('tr').each(function() {
                    if ($(this).attr('rowspan')) {
                        rowCount += +$(this).attr('rowspan');
                    } else {
                        rowCount++;
                    }
                });

                $table.find('tr:nth-child(1) td').each(function() {
                    if ($(this).attr('colspan')) {
                        colCount += +$(this).attr('colspan');
                    } else {
                        colCount++;
                    }
                });
                console.log('rowCount: ' + rowCount.toString());
                console.log('colCount: ' + colCount.toString());


                var $row = $td.closest('tr');
                var values = [];



                console.log('----siblings----');
                $td.siblings().each(function() {
                    console.log(this);
                    console.log($(this).text());
                    values.push($(this).text());
                });
                console.log('----children----');
                $row.children().each(function() {
                    console.log($(this).text());
                });
                console.log('--------');
                values.push(32);
                HostObj.testFunc(values);
                //var $row = $td.closest('tr');
                //var $allTd = $row.find('td');
                //// var $row1 = $table.children('th:first')
                //var $th = $td.closest('table').find('th').eq($td.index());

                //$table.text('afefefefe')

                //// $($table).find('th').each(function() {
                ////     console.log($(this).text());
                //// })

                //// $table.find('th').css('color', 'red');

                //// console.log($th.text());

                //// var s = getSiblings(parentElement);
                //// for (var j = 0; j < s.length; j++) {
                ////     console.log(s[j].textContent);
                //// }
            }

            // リンクを登録
            HostObj.addLink(href, node.textContent.replace(/^\s+|\s+$/g, ''));
        }
    }
    HostObj.endAddLink();
})();