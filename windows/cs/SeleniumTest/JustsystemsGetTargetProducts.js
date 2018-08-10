return (() => {
    let titles = document.documentElement.querySelectorAll('body > div > div.cont_dlm_main > h4.ttl');
    let parent = null;
    for (let i = 0; i < titles.length; i++) {
        let t = titles[i];
        if (t.innerText == '対象製品一覧') {
            parent = t.parentElement;
            break;
        }
    }
    if (!parent) {
        return '';
    }

    //let tp = parent.querySelector('div.c_middle_in02 > div.midasi_cont > ul.none')
    let tp = parent.querySelector('div.c_middle_in02')
    if (!tp) {
        return '';
    }

    return tp.innerText;
})();
