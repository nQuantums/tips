var system = require('system');
console.log('initialising program, waiting for user input');

var lastPage = null;

setInterval(function() {
    var line = system.stdin.readLine(); // ここでブロックされちゃってる
    line = line.toString();
    if (line) {
        console.log('>>', line);
        if (line === 'exit') {
            phantom.exit();
        } else if (line.indexOf('http') == 0) {
            var page = require('webpage').create();
            page.open(line, function(status) {
                console.log("Status: " + status);
                if (status === "success") {
                    console.log('ロード終わったよ');
                    lastPage = page;
                } else {
                    console.log('ロード失敗');
                }
            });
        } else {
            lastPage.render(line + '.png');
        }
    } else {
        console.log("none");
    }
}, 100);

console.log('logic running async');


// "use strict";
// var system = require('system');

// system.stdout.write('Hello, system.stdout.write!');
// system.stdout.writeLine('\nHello, system.stdout.writeLine!');

// system.stderr.write('Hello, system.stderr.write!');
// system.stderr.writeLine('\nHello, system.stderr.writeLine!');

// system.stdout.writeLine('system.stdin.readLine(): ');
// var line = system.stdin.readLine();
// system.stdout.writeLine(JSON.stringify(line));

// // This is essentially a `readAll`
// system.stdout.writeLine('system.stdin.read(5): (ctrl+D to end)');
// var input = system.stdin.read(5);
// system.stdout.writeLine(JSON.stringify(input));

// phantom.exit(0);





// var page = require('webpage').create();
// page.open('https://www.catalog.update.microsoft.com/Search.aspx?q=KB4056893/', function(status) {
//     console.log("Status: " + status);
//     if (status === "success") {
//         // page.render('updatecatalog.png');
//         // console.log('スクリーンショット撮ったよ');
//         console.log('ロード終わったよ');
//     }
//     phantom.exit();
// });





// var page = require('webpage').create();
// var url = 'http://www.google.co.jp/';
// page.open(url, function (status) {
//     page.includeJs('https://ajax.googleapis.com/ajax/libs/jquery/3.3.1/jquery.min.js', function () {
//         console.log(page.evaluate(function () {
//             return $('title').text();
//         }));
//         phantom.exit();
//     });
// });




//var page = require('webpage').create();
//page.open('http://yahoo.co.jp', function (status) {
//    console.log("Status: " + status);
//    if (status === "success") {
//        page.render('example.png');
//    }
//    phantom.exit();
//});