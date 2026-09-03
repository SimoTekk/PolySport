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

    // --- Kaderauswahl (Match anlegen / bearbeiten) ---
    // Ankreuzliste statt Dropdown: das Suchfeld blendet nur Zeilen aus,
    // gesetzte Haken bleiben dabei erhalten.
    var picker = document.querySelector('[data-roster-picker]');
    if (picker) {
        var items = Array.prototype.slice.call(picker.querySelectorAll('[data-roster-item]'));
        var boxes = Array.prototype.slice.call(picker.querySelectorAll('[data-roster-check]'));
        var counter = picker.querySelector('[data-roster-count]');
        var emptyHint = picker.querySelector('[data-roster-empty]');
        var filter = picker.querySelector('[data-roster-filter]');
        var goalie = picker.querySelector('[data-roster-goalie]');

        var updateCount = function () {
            if (!counter) return;
            counter.textContent = boxes.filter(function (box) { return box.checked; }).length;
        };

        var applyFilter = function () {
            var term = filter ? filter.value.trim().toLowerCase() : '';
            var visible = 0;

            items.forEach(function (item) {
                var match = term === '' || (item.getAttribute('data-roster-name') || '').indexOf(term) !== -1;
                item.classList.toggle('d-none', !match);
                if (match) visible += 1;
            });

            if (emptyHint) emptyHint.classList.toggle('d-none', visible > 0);
        };

        boxes.forEach(function (box) {
            box.addEventListener('change', function () {
                // Der Torhüter muss im Kader stehen: wird sein Haken entfernt,
                // ist die Torhüter-Angabe hinfällig.
                if (!box.checked && goalie && goalie.value === box.value) goalie.value = '';
                updateCount();
            });
        });

        if (filter) filter.addEventListener('input', applyFilter);

        // Der Torhüter gehört zum Aufgebot – der Haken wird mitgesetzt.
        if (goalie) {
            goalie.addEventListener('change', function () {
                boxes.forEach(function (box) {
                    if (box.value === goalie.value) box.checked = true;
                });
                updateCount();
            });
        }

        var selectAll = picker.querySelector('[data-roster-all]');
        var selectNone = picker.querySelector('[data-roster-none]');

        // Alle/Keine wirken nur auf sichtbare Zeilen, damit ein gesetzter
        // Filter nicht heimlich den ganzen Kader umstellt.
        if (selectAll) {
            selectAll.addEventListener('click', function () {
                items.forEach(function (item) {
                    if (item.classList.contains('d-none')) return;
                    var box = item.querySelector('[data-roster-check]');
                    if (box) box.checked = true;
                });
                updateCount();
            });
        }

        if (selectNone) {
            selectNone.addEventListener('click', function () {
                items.forEach(function (item) {
                    if (item.classList.contains('d-none')) return;
                    var box = item.querySelector('[data-roster-check]');
                    if (!box) return;
                    box.checked = false;
                    if (goalie && goalie.value === box.value) goalie.value = '';
                });
                updateCount();
            });
        }

        applyFilter();
        updateCount();
    }

    // --- Aufgebot kopieren (Dashboard) ---
    // Der Text steht fertig im data-Attribut, damit er genau so in den
    // Chat wandert, wie er auf der Kachel steht.
    var copyButtons = document.querySelectorAll('[data-copy-text]');
    Array.prototype.forEach.call(copyButtons, function (button) {
        button.addEventListener('click', function () {
            var text = button.getAttribute('data-copy-text') || '';
            var original = button.textContent;

            var done = function (message) {
                button.textContent = message;
                setTimeout(function () { button.textContent = original; }, 2000);
            };

            if (navigator.clipboard && navigator.clipboard.writeText) {
                navigator.clipboard.writeText(text)
                    .then(function () { done('Kopiert'); })
                    .catch(function () { done('Kopieren nicht möglich'); });
                return;
            }

            // Rückfall für Browser ohne Clipboard-API (und http ohne TLS)
            var helper = document.createElement('textarea');
            helper.value = text;
            helper.setAttribute('readonly', 'readonly');
            helper.style.position = 'fixed';
            helper.style.left = '-1000px';
            document.body.appendChild(helper);
            helper.select();

            try {
                done(document.execCommand('copy') ? 'Kopiert' : 'Kopieren nicht möglich');
            } catch (error) {
                done('Kopieren nicht möglich');
            }

            document.body.removeChild(helper);
        });
    });

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
