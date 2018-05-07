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
            var parentElement = node.parentElement;
            if (parentElement.nodeName.toLowerCase() == 'td') {
                //var $td = $(parentElement);
                //var $row = $td.closest('tr');
                //var $allTd = $row.find('td');
                //var $table = $td.closest('table');
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