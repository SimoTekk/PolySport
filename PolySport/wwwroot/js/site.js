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

    // --- Update-Fortschritt ---
    // Fragt den Zustand ab, solange das Update läuft. Während des Neustarts
    // antwortet der Server nicht – das ist erwartet, danach geht es weiter.
    var progress = document.getElementById('updateProgress');
    if (progress) {
        var pollUrl = progress.getAttribute('data-poll-url');
        var stateText = document.getElementById('updateStateText');
        var messageText = document.getElementById('updateMessage');
        var offlineSince = null;

        setInterval(function () {
            fetch(pollUrl, { cache: 'no-store' })
                .then(function (response) { return response.ok ? response.json() : null; })
                .then(function (data) {
                    offlineSince = null;
                    if (!data) return;

                    if (messageText && data.message) messageText.textContent = data.message;

                    if (data.state === 'success' || data.state === 'failed') {
                        window.location.reload();
                    } else if (stateText) {
                        stateText.textContent = data.state === 'requested'
                            ? 'Update angefordert'
                            : 'Update läuft';
                    }
                })
                .catch(function () {
                    // Server nicht erreichbar: läuft gerade neu an
                    if (offlineSince === null) offlineSince = Date.now();
                    if (stateText) stateText.textContent = 'Anwendung startet neu';
                    if (messageText) messageText.textContent = 'Warte auf die Anwendung...';
                });
        }, 5000);
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
