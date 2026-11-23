/**
 * MANARA Workspace Sidebar Scroll Persistence & Navigation Handling
 * Restores sidebar scroll position seamlessly across page reloads.
 */
(function () {
    "use strict";

    var STORAGE_KEY = "manara_ws_sidebar_scroll";

    function getScrollEl() {
        return document.querySelector(".ws-scroll");
    }

    function savePosition() {
        var el = getScrollEl();
        if (el) {
            try {
                sessionStorage.setItem(STORAGE_KEY, String(el.scrollTop));
            } catch (e) { }
        }
    }

    function restorePosition() {
        var el = getScrollEl();
        if (!el) return;

        var saved = null;
        try {
            saved = sessionStorage.getItem(STORAGE_KEY);
        } catch (e) { }

        if (saved !== null) {
            var pos = parseInt(saved, 10);
            if (!isNaN(pos)) {
                el.scrollTop = pos;
                return;
            }
        }

        // Fallback: If no saved position, ensure active navigation item is in view
        var activeBtn = el.querySelector(".ws-nav-btn.active");
        if (activeBtn) {
            try {
                activeBtn.scrollIntoView({ block: "nearest" });
            } catch (e) { }
        }
    }

    // Attempt early restore as soon as DOM is interactive
    if (document.readyState === "loading") {
        document.addEventListener("DOMContentLoaded", restorePosition);
    } else {
        restorePosition();
    }

    // Save on scroll (throttled)
    var scrollTimer = null;
    document.addEventListener("scroll", function (e) {
        if (e.target && e.target.classList && e.target.classList.contains("ws-scroll")) {
            if (scrollTimer) clearTimeout(scrollTimer);
            scrollTimer = setTimeout(savePosition, 100);
        }
    }, true);

    // Save on sidebar link click
    document.addEventListener("click", function (e) {
        var link = e.target.closest(".ws-sidebar a");
        if (link) {
            savePosition();
        }

        // Fix: Explicit direct navigation to homepage for Back to Website button
        var backBtn = e.target.closest("#backToWebsiteLink");
        if (backBtn) {
            e.preventDefault();
            window.location.href = "/";
        }
    });

    window.addEventListener("beforeunload", savePosition);
})();
