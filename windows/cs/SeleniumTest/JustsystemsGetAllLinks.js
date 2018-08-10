return (() => {
    links = [];
    document.documentElement.querySelectorAll('#main1 > div.content_wrap > div.searchbox > div.search-free.catearea > div.searcharea > div.searcharea_result > div.resultArea > div.concrete2').forEach(
        c => links.push({
            title: c.querySelector('p').textContent,
            url: c.querySelector('a').href
        }));
    document.documentElement.querySelectorAll('#main2 > div.content_wrap > div.searchbox > div.search-free.catearea > div.searcharea > div.searcharea_result > div.resultArea > div.concrete2').forEach(
        c => links.push({
            title: c.querySelector('p').textContent,
            url: c.querySelector('a').href
        }));
    return JSON.stringify(links);
})();
