document.addEventListener('DOMContentLoaded', function () {

    // --- Dark/Light Mode Toggle ---
    var themeSwitch = document.getElementById('themeSwitch');
    if (themeSwitch && window.PolySportTheme) {
        themeSwitch.addEventListener('click', function () {
            window.PolySportTheme.toggle();
        });
    }

    // --- Drucken (Saisonblatt) ---
    var printButton = document.getElementById('printSheet');
    if (printButton) {
        printButton.addEventListener('click', function () {
            window.print();
        });
    }

    // --- Spieluhr ---
    // Der Server liefert den Stand beim Rendern; hier wird nur weitergezählt.
    // Die Wahrheit bleibt auf dem Server, ein Neuladen synchronisiert wieder.
    var clock = document.getElementById('matchClock');
    if (clock && clock.getAttribute('data-running') === 'true') {
        var seconds = parseInt(clock.getAttribute('data-elapsed'), 10);
        if (isNaN(seconds)) seconds = 0;

        var format = function (total) {
            var m = Math.floor(total / 60);
            var s = total % 60;
            return (m < 10 ? '0' + m : m) + ':' + (s < 10 ? '0' + s : s);
        };

        setInterval(function () {
            seconds += 1;
            clock.textContent = format(seconds);
        }, 1000);
    }

});
