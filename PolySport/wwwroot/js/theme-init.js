/* PolySport – Hell/Dunkel-Umschaltung.
   Läuft blockierend im <head>, damit die gespeicherte Einstellung vor dem
   ersten Rendern steht und nichts kurz in der falschen Helligkeit aufblitzt. */
(function () {
    'use strict';

    var root = document.documentElement;

    function read(key) {
        try { return localStorage.getItem(key) || ''; } catch (e) { return ''; }
    }

    function write(key, value) {
        try { localStorage.setItem(key, value); } catch (e) { /* privater Modus */ }
    }

    function apply(theme) {
        theme = theme === 'dark' ? 'dark' : 'light';
        root.setAttribute('data-bs-theme', theme);
        write('theme', theme);
        return theme;
    }

    window.PolySportTheme = {
        apply: apply,
        toggle: function () {
            return apply(root.getAttribute('data-bs-theme') === 'dark' ? 'light' : 'dark');
        },
        current: function () {
            return root.getAttribute('data-bs-theme');
        }
    };

    apply(read('theme'));

    // Reste der früheren Farbpalette entfernen, damit keine alten Werte
    // in gespeicherten Browsern herumliegen.
    try {
        ['bgColor', 'textColor', 'accentColor'].forEach(function (key) {
            localStorage.removeItem(key);
        });
    } catch (e) { /* egal */ }
})();
