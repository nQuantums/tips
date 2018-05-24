chrome.app.runtime.onLaunched.addListener(function(launchData) {
    chrome.app.window.create('index.html', {
        'bounds': {
            'width' : 200,
            'height': 300,
            'top' : 0,
            'left': 0
        }
    });
});