document.addEventListener('DOMContentLoaded', function () {

    // --- Dark/Light Mode Toggle ---
    var themeSwitch = document.getElementById('themeSwitch');
    if (themeSwitch) {
        themeSwitch.addEventListener('click', function () {
            var current = document.documentElement.getAttribute('data-bs-theme');
            var next = current === 'dark' ? 'light' : 'dark';
            document.documentElement.setAttribute('data-bs-theme', next);
            localStorage.setItem('theme', next);
        });
    }

    // --- Color Palette ---
    var bgColors = [
        { name: 'Standard', value: '' },
        { name: 'Weiss', value: '#ffffff' },
        { name: 'Hellgrau', value: '#f0f2f5' },
        { name: 'Sand', value: '#fdf6e3' },
        { name: 'Anthrazit', value: '#2d2d2d' },
        { name: 'Dunkelgrau', value: '#1e1e1e' },
        { name: 'Kohle', value: '#121212' },
        { name: 'Navy', value: '#0d1b2a' },
        { name: 'Nachtblau', value: '#1a1a2e' },
        { name: 'Dunkelgruen', value: '#1a2e1a' },
        { name: 'Aubergine', value: '#2d1b2e' },
        { name: 'Schwarz', value: '#000000' }
    ];

    var textColors = [
        { name: 'Standard', value: '' },
        { name: 'Schwarz', value: '#000000' },
        { name: 'Dunkelgrau', value: '#333333' },
        { name: 'Grau', value: '#6c757d' },
        { name: 'Weiss', value: '#ffffff' },
        { name: 'Hellgrau', value: '#c0c0c0' },
        { name: 'Dunkelblau', value: '#1b2a4a' },
        { name: 'Braun', value: '#5c4033' },
        { name: 'Sepia', value: '#704214' },
        { name: 'Elfenbein', value: '#f5f5dc' }
    ];

    var accentColors = [
        { name: 'Standard', value: '' },
        { name: 'Blau', value: '#0d6efd' },
        { name: 'Indigo', value: '#6610f2' },
        { name: 'Lila', value: '#6f42c1' },
        { name: 'Pink', value: '#d63384' },
        { name: 'Rot', value: '#dc3545' },
        { name: 'Orange', value: '#fd7e14' },
        { name: 'Gruen', value: '#198754' },
        { name: 'Tuerkis', value: '#20c997' },
        { name: 'Cyan', value: '#0dcaf0' }
    ];

    function darkenColor(hex, amount) {
        var num = parseInt(hex.replace('#', ''), 16);
        var r = Math.max((num >> 16) - amount, 0);
        var g = Math.max(((num >> 8) & 0x00FF) - amount, 0);
        var b = Math.max((num & 0x0000FF) - amount, 0);
        return '#' + (0x1000000 + r * 0x10000 + g * 0x100 + b).toString(16).slice(1);
    }

    function applyBgColor(color) {
        if (color) {
            document.documentElement.style.setProperty('--ps-bg', color);
            localStorage.setItem('bgColor', color);
        } else {
            document.documentElement.style.removeProperty('--ps-bg');
            localStorage.removeItem('bgColor');
        }
        updateSwatches();
    }

    function applyTextColor(color) {
        if (color) {
            document.documentElement.style.setProperty('--ps-text', color);
            localStorage.setItem('textColor', color);
        } else {
            document.documentElement.style.removeProperty('--ps-text');
            localStorage.removeItem('textColor');
        }
        updateSwatches();
    }

    function applyAccentColor(color) {
        if (color) {
            document.documentElement.style.setProperty('--ps-accent', color);
            document.documentElement.style.setProperty('--ps-accent-hover', darkenColor(color, 30));
            localStorage.setItem('accentColor', color);
        } else {
            document.documentElement.style.removeProperty('--ps-accent');
            document.documentElement.style.removeProperty('--ps-accent-hover');
            localStorage.removeItem('accentColor');
        }
        updateSwatches();
    }

    function buildSwatches(container, colors, type) {
        if (!container) return;
        colors.forEach(function (c) {
            var swatch = document.createElement('div');
            swatch.className = 'color-swatch';
            swatch.title = c.name;
            if (c.value) {
                swatch.style.backgroundColor = c.value;
            } else {
                swatch.style.background = 'conic-gradient(#ff0000, #ff8800, #ffff00, #00ff00, #0088ff, #8800ff, #ff0000)';
                swatch.style.opacity = '0.4';
            }
            swatch.setAttribute('data-color', c.value);
            swatch.setAttribute('data-type', type);
            swatch.addEventListener('click', function () {
                if (type === 'bg') {
                    applyBgColor(c.value);
                } else if (type === 'text') {
                    applyTextColor(c.value);
                } else {
                    applyAccentColor(c.value);
                }
            });
            container.appendChild(swatch);
        });
    }

    function updateSwatches() {
        var currentBg = localStorage.getItem('bgColor') || '';
        var currentText = localStorage.getItem('textColor') || '';
        var currentAccent = localStorage.getItem('accentColor') || '';
        document.querySelectorAll('.color-swatch').forEach(function (s) {
            var type = s.getAttribute('data-type');
            var color = s.getAttribute('data-color');
            if (type === 'bg') {
                s.classList.toggle('active', color === currentBg);
            } else if (type === 'text') {
                s.classList.toggle('active', color === currentText);
            } else {
                s.classList.toggle('active', color === currentAccent);
            }
        });
    }

    buildSwatches(document.getElementById('bgColors'), bgColors, 'bg');
    buildSwatches(document.getElementById('textColors'), textColors, 'text');
    buildSwatches(document.getElementById('accentColors'), accentColors, 'accent');
    updateSwatches();

    // Apply saved accent hover on load
    var savedAccent = localStorage.getItem('accentColor');
    if (savedAccent) {
        document.documentElement.style.setProperty('--ps-accent-hover', darkenColor(savedAccent, 30));
    }

    // Reset button
    var resetBtn = document.getElementById('resetColors');
    if (resetBtn) {
        resetBtn.addEventListener('click', function () {
            applyBgColor('');
            applyTextColor('');
            applyAccentColor('');
        });
    }

});
