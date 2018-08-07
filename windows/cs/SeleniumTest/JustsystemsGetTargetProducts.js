return (() => {
    let e = document.documentElement.querySelector('body > div > div:nth-child(17) > div > div > ul');
    if (!e) {
        return '';
    }
    return e.innerText;
})();
