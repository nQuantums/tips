return (() => {
    elems = [];
    document.documentElement.querySelectorAll('#main1 > div.content_wrap > div.pdt_section > div.pdt_sectionTitle').forEach(t => elems.push(t));
    document.documentElement.querySelectorAll('#main2 > div.content_wrap > div.pdt_section > div.pdt_sectionTitle').forEach(t => elems.push(t));

    let links = [];
    for (let i = 0; i < elems.length; i++) {
        let e = elems[i];
        if (e.innerText.trim().startsWith('ダウンロード')) {
            let p = e.parentElement;
            p.querySelectorAll('div.pdt_sectionInner > div.pdt_sectionInnerBox.pdt_tableLayout > ul > li > a').forEach(a => links.push(
                {
                    title: a.innerText,
                    url: a.href
                }
            ));
        }
    }

    return JSON.stringify(links);
})();
