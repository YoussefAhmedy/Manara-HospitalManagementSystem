/**
 * MANARA Medical Complex — Public Site, Video Player, Carousel & Modal Controls
 * Strictly unobtrusive, CSP-compliant event delegation.
 */
(function () {
    "use strict";

    function showToast(msg, isError) {
        var stack = document.getElementById("toastStack");
        if (!stack) return;
        var el = document.createElement("div");
        el.className = "toast" + (isError ? " error" : "");
        el.textContent = msg;
        stack.appendChild(el);
        setTimeout(function () { el.remove(); }, 4200);
    }

    function openModal(title, bodyHtml, footHtml, isLarge, isDark) {
        var titleEl = document.getElementById("modalTitle");
        var bodyEl = document.getElementById("modalBody");
        var footEl = document.getElementById("modalFooter");
        var boxEl = document.getElementById("modalBox");
        var overlayEl = document.getElementById("modalOverlay");

        if (!titleEl || !bodyEl || !footEl || !boxEl || !overlayEl) return;

        titleEl.textContent = title;
        bodyEl.innerHTML = bodyHtml;
        footEl.innerHTML = footHtml;
        boxEl.className = "modal" + (isLarge ? " modal-lg" : "") + (isDark ? " hero-video-modal-box" : "");
        overlayEl.classList.add("open");
    }

    function closeModal() {
        var overlayEl = document.getElementById("modalOverlay");
        if (overlayEl) {
            overlayEl.classList.remove("open");
            var modalVideo = overlayEl.querySelector("video");
            if (modalVideo) {
                modalVideo.pause();
            }
        }
    }

    function openBookModal(dept, doc, docId, fee, followUpFee) {
        var defaultDept = dept || "Cardiology";
        var defaultDoc = doc || "Any Available Consultant";
        var tomorrow = new Date(Date.now() + 86400000).toISOString().slice(0, 10);
        var priceInfo = fee ? (fee + " EGP consultation (Returning Follow-Up: " + (followUpFee || 150) + " EGP)") : "350 – 750 EGP (Follow-Up discount applies automatically)";

        var body = '<form id="manaraBookForm" class="form-grid">' +
            '<div class="form-field full"><label class="form-label">Specialist / Department</label><input class="input" value="' + defaultDoc + ' — ' + defaultDept + '" readonly /></div>' +
            '<div class="form-field full"><div style="font-size:12px;padding:8px 12px;border-radius:8px;background:var(--teal-soft);color:var(--teal);font-weight:600;">Fee Schedule: ' + priceInfo + '</div></div>' +
            '<div class="form-field"><label class="form-label">Preferred Date *</label><input class="input" type="date" name="date" value="' + tomorrow + '" required /></div>' +
            '<div class="form-field"><label class="form-label">Preferred Time Slot</label><select class="select" name="slot"><option>09:30 AM</option><option>10:30 AM</option><option>11:30 AM</option><option>01:00 PM</option></select></div>' +
            '<div class="form-field full"><label class="form-label">Reason for Visit</label><input class="input" name="notes" placeholder="Brief symptoms or follow-up inquiry..." /></div>' +
            '</form>';

        var foot = '<button type="button" class="btn btn-outline btn-sm" id="btnCancelBook">Cancel</button>' +
            '<button type="button" class="btn btn-navy btn-sm" id="btnProceedBook">Continue to Booking</button>';

        openModal("Book Consultation at MANARA", body, foot);

        var cancelBtn = document.getElementById("btnCancelBook");
        if (cancelBtn) cancelBtn.addEventListener("click", closeModal);

        var proceedBtn = document.getElementById("btnProceedBook");
        if (proceedBtn) {
            proceedBtn.addEventListener("click", function () {
                var target = "/Patient/AddAppointment";
                if (docId) {
                    target += "?doctorId=" + encodeURIComponent(docId);
                }
                closeModal();
                window.location.href = target;
            });
        }
    }

    function openCinemaModal() {
        var body = '<video class="hero-modal-video" controls autoplay playsinline loop>' +
            '<source src="/videos/manara-complex.webm" type="video/webm" />' +
            '<source src="/videos/manara-complex.mp4" type="video/mp4" />' +
            '</video>';
        var foot = '<button type="button" class="btn btn-outline btn-sm" id="btnCloseCinema" style="color:#F8FAFC;border-color:rgba(255,255,255,0.2);">Close Cinema View</button>';
        openModal("MANARA Medical Complex — Sanctuary Showcase", body, foot, true, true);
        var btn = document.getElementById("btnCloseCinema");
        if (btn) btn.addEventListener("click", closeModal);
    }

    function switchAuthTab(tab) {
        var loginBtn = document.getElementById("tabLoginBtn");
        var regBtn = document.getElementById("tabRegBtn");
        var loginPane = document.getElementById("loginFormPane");
        var regPane = document.getElementById("registerFormPane");
        if (!loginBtn || !regBtn || !loginPane || !regPane) return;

        if (tab === "register") {
            loginBtn.classList.remove("active");
            regBtn.classList.add("active");
            loginPane.style.display = "none";
            regPane.style.display = "block";
        } else {
            regBtn.classList.remove("active");
            loginBtn.classList.add("active");
            loginPane.style.display = "block";
            regPane.style.display = "none";
        }
    }

    function initHeroVideo() {
        var hero = document.getElementById("manaraHeroSection");
        var video = document.getElementById("manaraHeroVideo");
        var playBtn = document.getElementById("heroPlayBtn");
        var toggleBtn = document.getElementById("heroPauseToggle");
        var muteBtn = document.getElementById("heroMuteToggle");
        var closeBtn = document.getElementById("heroStopBtn");
        var cinemaBtn = document.getElementById("heroCinemaBtn");

        if (!hero || !video || !playBtn) return;

        playBtn.addEventListener("click", function () {
            hero.classList.add("video-playing");
            video.play().catch(function () {
                openCinemaModal();
            });
        });

        if (toggleBtn) {
            toggleBtn.addEventListener("click", function () {
                if (video.paused) {
                    video.play();
                    toggleBtn.textContent = "Pause";
                } else {
                    video.pause();
                    toggleBtn.textContent = "Play";
                }
            });
        }

        if (muteBtn) {
            muteBtn.addEventListener("click", function () {
                video.muted = !video.muted;
                muteBtn.textContent = video.muted ? "Unmute" : "Mute";
            });
        }

        if (closeBtn) {
            closeBtn.addEventListener("click", function () {
                video.pause();
                video.currentTime = 0;
                hero.classList.remove("video-playing");
            });
        }

        if (cinemaBtn) {
            cinemaBtn.addEventListener("click", function () {
                video.pause();
                hero.classList.remove("video-playing");
                openCinemaModal();
            });
        }
    }

    function initDoctorsCarousel() {
        var viewport = document.getElementById("docCarouselViewport");
        var track = document.getElementById("docCarouselTrack");
        var prevBtn = document.getElementById("docPrevBtn");
        var nextBtn = document.getElementById("docNextBtn");
        if (!viewport || !track) return;

        var cards = track.querySelectorAll(".doc-card");
        if (cards.length === 0) return;

        var currentIndex = 0;
        var paused = false;

        function visibleCount() {
            var w = window.innerWidth;
            if (w <= 680) return 1;
            if (w <= 1080) return 2;
            return 4;
        }

        function updateTransform() {
            var cardWidth = cards[0].getBoundingClientRect().width;
            var gap = 22;
            var maxIdx = Math.max(0, cards.length - visibleCount());
            if (currentIndex > maxIdx) currentIndex = 0;
            if (currentIndex < 0) currentIndex = maxIdx;
            track.style.transform = "translateX(-" + (currentIndex * (cardWidth + gap)) + "px)";
        }

        if (nextBtn) {
            nextBtn.addEventListener("click", function () {
                var maxIdx = Math.max(0, cards.length - visibleCount());
                currentIndex = currentIndex >= maxIdx ? 0 : currentIndex + 1;
                updateTransform();
            });
        }

        if (prevBtn) {
            prevBtn.addEventListener("click", function () {
                var maxIdx = Math.max(0, cards.length - visibleCount());
                currentIndex = currentIndex <= 0 ? maxIdx : currentIndex - 1;
                updateTransform();
            });
        }

        viewport.addEventListener("mouseenter", function () { paused = true; });
        viewport.addEventListener("mouseleave", function () { paused = false; });

        window.addEventListener("resize", updateTransform);

        setInterval(function () {
            if (paused) return;
            var maxIdx = Math.max(0, cards.length - visibleCount());
            currentIndex = currentIndex >= maxIdx ? 0 : currentIndex + 1;
            updateTransform();
        }, 4200);
    }

    function initAvatarMenu() {
        var trigger = document.getElementById("avatarMenuBtn");
        var dropdown = document.getElementById("avatarDropdown");
        if (!trigger || !dropdown) return;

        trigger.addEventListener("click", function (e) {
            e.stopPropagation();
            var isOpen = dropdown.classList.toggle("open");
            trigger.setAttribute("aria-expanded", isOpen ? "true" : "false");
        });

        document.addEventListener("click", function (e) {
            if (!dropdown.contains(e.target) && !trigger.contains(e.target)) {
                dropdown.classList.remove("open");
                trigger.setAttribute("aria-expanded", "false");
            }
        });
    }

    document.addEventListener("DOMContentLoaded", function () {
        var mobileBtn = document.getElementById("mobileMenuBtn");
        var navCenter = document.getElementById("navCenter");
        if (mobileBtn && navCenter) {
            mobileBtn.addEventListener("click", function () {
                navCenter.classList.toggle("open");
            });
        }

        var modalClose = document.getElementById("modalCloseBtn");
        if (modalClose) modalClose.addEventListener("click", closeModal);

        var overlay = document.getElementById("modalOverlay");
        if (overlay) {
            overlay.addEventListener("click", function (e) {
                if (e.target === overlay) closeModal();
            });
        }

        document.querySelectorAll("[data-book-modal], [data-book-dept], [data-book-doc]").forEach(function (btn) {
            btn.addEventListener("click", function () {
                openBookModal(
                    btn.getAttribute("data-book-dept"),
                    btn.getAttribute("data-book-doc"),
                    btn.getAttribute("data-book-doc-id"),
                    btn.getAttribute("data-book-fee"),
                    btn.getAttribute("data-book-followup")
                );
            });
        });

        var tabLoginBtn = document.getElementById("tabLoginBtn");
        if (tabLoginBtn) tabLoginBtn.addEventListener("click", function () { switchAuthTab("login"); });

        var tabRegBtn = document.getElementById("tabRegBtn");
        if (tabRegBtn) tabRegBtn.addEventListener("click", function () { switchAuthTab("register"); });

        var switchToRegLink = document.getElementById("switchToRegisterLink");
        if (switchToRegLink) switchToRegLink.addEventListener("click", function (e) { e.preventDefault(); switchAuthTab("register"); });

        var switchToLoginLink = document.getElementById("switchToLoginLink");
        if (switchToLoginLink) switchToLoginLink.addEventListener("click", function (e) { e.preventDefault(); switchAuthTab("login"); });

        var togglePw = document.getElementById("togglePwBtn");
        if (togglePw) {
            togglePw.addEventListener("click", function () {
                var passInput = document.getElementById("loginPassInput");
                if (!passInput) return;
                if (passInput.type === "password") {
                    passInput.type = "text";
                    togglePw.textContent = "Hide";
                } else {
                    passInput.type = "password";
                    togglePw.textContent = "Show";
                }
            });
        }

        // Apply font size accessibility preference from Settings
        try {
            if (localStorage.getItem("manara_ws_font_large") === "1") {
                document.body.classList.add("ws-font-large");
            }
        } catch (err) { }

        var fontToggle = document.getElementById("wsFontSizeToggle");
        if (fontToggle) {
            try { fontToggle.checked = localStorage.getItem("manara_ws_font_large") === "1"; } catch (err) { }
            fontToggle.addEventListener("change", function () {
                document.body.classList.toggle("ws-font-large", fontToggle.checked);
                try { localStorage.setItem("manara_ws_font_large", fontToggle.checked ? "1" : "0"); } catch (err) { }
                showToast(fontToggle.checked ? "Large accessible typography enabled." : "Default typography restored.");
            });
        }

        document.querySelectorAll("[data-action='print']").forEach(function (btn) {
            btn.addEventListener("click", function () { window.print(); });
        });

        var pacsImg = document.getElementById("pacsScanImg");
        if (pacsImg) {
            document.querySelectorAll("[data-pacs-filter]").forEach(function (btn) {
                btn.addEventListener("click", function () {
                    var filter = btn.getAttribute("data-pacs-filter");
                    if (filter === "invert") pacsImg.style.filter = "invert(1)";
                    else if (filter === "contrast") pacsImg.style.filter = "contrast(1.5)";
                    else pacsImg.style.filter = "none";
                });
            });
        }

        initHeroVideo();
        initDoctorsCarousel();
        initAvatarMenu();
    });

    window.manaraShowToast = showToast;
})();
