// IIM Theme Integration - HUD-Free Mode (direct control, persistent, no external deps)

window.IIMThemeIntegration = window.IIMThemeIntegration || {};

(function (IIMTheme) {
    'use strict';

    // --- Cookie utilities (vanilla JS) ---
    function setCookie(name, value, days) {
        let expires = "";
        if (days) {
            const d = new Date();
            d.setTime(d.getTime() + (days * 24 * 60 * 60 * 1000));
            expires = "; expires=" + d.toUTCString();
        }
        document.cookie = name + "=" + encodeURIComponent(value || "") + expires + "; path=/";
    }
    function getCookie(name) {
        const nameEQ = name + "=";
        const ca = document.cookie.split(';');
        for (let i = 0; i < ca.length; i++) {
            let c = ca[i];
            while (c.charAt(0) == ' ') c = c.substring(1, c.length);
            if (c.indexOf(nameEQ) == 0) return decodeURIComponent(c.substring(nameEQ.length, c.length));
        }
        return null;
    }
    function deleteCookie(name) {
        document.cookie = name + "=; Max-Age=-99999999; path=/";
    }

    // --- Theme Mode ---
    IIMTheme.setThemeMode = function (mode) {
        const html = document.documentElement;
        if (mode === "dark" || mode === "light") {
            html.setAttribute("data-bs-theme", mode);
            setCookie("app-theme-mode", mode, 365);
            return true;
        }
        html.removeAttribute("data-bs-theme");
        deleteCookie("app-theme-mode");
        return false;
    };

    // --- Theme Color (still assumes a class, e.g. theme-color-blue) ---
    IIMTheme.setThemeColor = function (themeClass) {
        const html = document.documentElement;
        // Remove all theme-color-* classes
        html.className = html.className.replace(/\btheme-color-\S+/g, '').trim();
        if (themeClass && themeClass !== '') {
            html.classList.add(themeClass);
            setCookie("app-theme", themeClass, 365);
        } else {
            deleteCookie("app-theme");
        }
        return true;
    };

    // --- Theme Cover (sets bg-cover-*) ---
    IIMTheme.setThemeCover = function (coverClass) {
        const html = document.documentElement;
        // Remove any old bg-cover-* classes
        html.className = html.className.replace(/\bbg-cover-\d+\b/g, '').trim();
        // Add new class if provided
        if (coverClass && coverClass.startsWith('bg-cover-')) {
            html.classList.add(coverClass);
            setCookie('app-theme-cover', coverClass, 365);
        } else {
            deleteCookie('app-theme-cover');
        }
        return true;
    };

    // --- Restore theme mode/cover at startup ---
    function restoreTheme() {
        // Theme mode
        const mode = getCookie("app-theme-mode");
        if (mode) IIMTheme.setThemeMode(mode);
        // Theme color
        const tColor = getCookie("app-theme");
        if (tColor) IIMTheme.setThemeColor(tColor);
        // Theme cover
        const cover = getCookie("app-theme-cover");
        if (cover) IIMTheme.setThemeCover(cover);
    }
    if (document.readyState === "loading") {
        document.addEventListener("DOMContentLoaded", restoreTheme);
    } else {
        restoreTheme();
    }

    // --- Direction (LTR/RTL, sets attribute) ---
    IIMTheme.setDirection = function (direction) {
        const html = document.documentElement;
        if (direction === "rtl" || direction === "ltr") {
            html.setAttribute("dir", direction);
            setCookie("app-theme-direction", direction, 365);
            return true;
        }
        html.removeAttribute("dir");
        deleteCookie("app-theme-direction");
        return false;
    };

    // --- Get settings from cookies ---
    IIMTheme.getCurrentSettings = function () {
        return {
            themeMode: getCookie('app-theme-mode') || 'dark',
            themeClass: getCookie('app-theme') || '',
            themeCover: getCookie('app-theme-cover') || '',
            themeDirection: getCookie('app-theme-direction') || 'ltr'
        };
    };

    // --- IIM-specific (unchanged) ---
    IIMTheme.setCompactMode = function (enabled) {
        if (enabled) document.body.classList.add('compact-mode');
        else document.body.classList.remove('compact-mode');
        localStorage.setItem('iim-compact-mode', enabled.toString());
    };
    IIMTheme.setHighContrast = function (enabled) {
        if (enabled) document.body.classList.add('high-contrast');
        else document.body.classList.remove('high-contrast');
        localStorage.setItem('iim-high-contrast', enabled.toString());
    };
    IIMTheme.setAnimations = function (enabled) {
        if (!enabled) document.body.classList.add('no-animations');
        else document.body.classList.remove('no-animations');
        localStorage.setItem('iim-animations', enabled.toString());
    };
    // Custom background image (overrides theme cover)
    IIMTheme.setCustomBackground = function (imageUrl) {
        if (imageUrl) {
            document.body.style.backgroundImage = 'url(' + imageUrl + ')';
            document.body.style.backgroundSize = 'cover';
            document.body.style.backgroundPosition = 'center';
            document.body.style.backgroundAttachment = 'fixed';
            // Remove bg-cover-* from <html>
            const html = document.documentElement;
            html.className = html.className.replace(/\bbg-cover-\d+\b/g, '').trim();
            localStorage.setItem('iim-custom-bg', imageUrl);
        } else {
            document.body.style.backgroundImage = '';
            localStorage.removeItem('iim-custom-bg');
        }
    };
    IIMTheme.loadIIMSettings = function () {
        return {
            compactMode: localStorage.getItem('iim-compact-mode') === 'true',
            highContrast: localStorage.getItem('iim-high-contrast') === 'true',
            animations: localStorage.getItem('iim-animations') !== 'false',
            customBackground: localStorage.getItem('iim-custom-bg') || ''
        };
    };
    IIMTheme.applyIIMSettings = function (settings) {
        if (settings.compactMode !== undefined) IIMTheme.setCompactMode(settings.compactMode);
        if (settings.highContrast !== undefined) IIMTheme.setHighContrast(settings.highContrast);
        if (settings.animations !== undefined) IIMTheme.setAnimations(settings.animations);
        if (settings.customBackground) IIMTheme.setCustomBackground(settings.customBackground);
    };
    IIMTheme.resetToDefaults = function () {
        IIMTheme.setThemeMode('dark');
        IIMTheme.setThemeColor('');
        IIMTheme.setThemeCover('');
        IIMTheme.setDirection('ltr');
        IIMTheme.setCompactMode(false);
        IIMTheme.setHighContrast(false);
        IIMTheme.setAnimations(true);
        IIMTheme.setCustomBackground('');
        localStorage.removeItem('iim-compact-mode');
        localStorage.removeItem('iim-high-contrast');
        localStorage.removeItem('iim-animations');
        localStorage.removeItem('iim-custom-bg');
    };
    IIMTheme.exportSettings = function () {
        var hudSettings = IIMTheme.getCurrentSettings();
        var iimSettings = IIMTheme.loadIIMSettings();
        return { ...hudSettings, ...iimSettings };
    };
    IIMTheme.triggerFileInput = function (selector) {
        var input = document.querySelector(selector);
        if (input) {
            input.click();
            return true;
        }
        return false;
    };

})(window.IIMThemeIntegration);
