return (function() {
    var id = 1;

    function setUniqueId(parent) {
        if (parent.hasChildNodes()) {
            var children = parent.childNodes;
            for (var i = 0; i < children.length; i++) {
                var node = children[i];
                if (!node.nodeName.startsWith('#')) {
                    node.setAttribute('hogeid', id);
                    id++;
                    setUniqueId(node);
                }
            }
        }
    };

    setUniqueId(document.documentElement);

    return document.documentElement.innerHTML;
})();