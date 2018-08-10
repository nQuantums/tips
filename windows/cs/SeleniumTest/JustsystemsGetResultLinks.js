return (() => {
    elems = [];
    document.documentElement.querySelectorAll('table[summary="main_area"] table[summary="resultlist_outline"]  table[summary="resultlist"] tr a').forEach(a => elems.push(a));

    let links = [];
    for (let i = 0; i < elems.length; i++) {
        let a = elems[i];
        let updateDate = a.parentElement.parentElement.lastElementChild.innerText;
        links.push(
            {
                title: a.innerText,
                url: a.href,
                updateDate: updateDate
            }
        );
    }

    return JSON.stringify(links);
})();
