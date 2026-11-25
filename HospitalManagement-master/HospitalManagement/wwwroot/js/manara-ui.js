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

    var bookingState = {
        catalog: null,
        mode: "dept",
        deptId: null,
        doctorId: null,
        date: null,
        slotMinutes: null,
        slotsCache: null
    };

    function escapeHtml(s) {
        return String(s || "")
            .replace(/&/g, "&amp;")
            .replace(/</g, "&lt;")
            .replace(/>/g, "&gt;")
            .replace(/"/g, "&quot;");
    }

    function openBookModal(dept, doc, docId, fee, followUpFee, deptId) {
        var isAuthenticated = document.body.getAttribute("data-auth") === "true";
        if (!isAuthenticated) {
            window.location.href = "/Account/LogIn?returnUrl=" + encodeURIComponent(window.location.pathname || "/");
            return;
        }

        bookingState.mode = "dept";
        bookingState.deptId = deptId ? parseInt(deptId, 10) : null;
        bookingState.doctorId = docId || null;
        bookingState.slotMinutes = null;
        bookingState.slotsCache = null;

        openModal(
            "Book Appointment at MANARA",
            '<div style="padding:24px;text-align:center;color:#64748B;">Loading specialist clinics and available dates&hellip;</div>',
            '<button type="button" class="btn btn-outline btn-sm" id="btnCancelBooking">Cancel</button>',
            true,
            false
        );

        var boxEl = document.getElementById("modalBox");
        if (boxEl) boxEl.classList.add("manara-book-modal");
        var cancelBtn = document.getElementById("btnCancelBooking");
        if (cancelBtn) cancelBtn.addEventListener("click", closeModal);

        fetch("/Appointments/PublicCatalog", { headers: { "Accept": "application/json" } })
            .then(function (r) { return r.json(); })
            .then(function (catalog) {
                bookingState.catalog = catalog;
                bookingState.date = catalog.minDate;

                if (!bookingState.deptId && bookingState.doctorId && catalog.doctors) {
                    var matchedDoc = catalog.doctors.find(function (d) { return d.id === bookingState.doctorId; });
                    if (matchedDoc) bookingState.deptId = matchedDoc.departmentId;
                }
                if (!bookingState.deptId && catalog.departments && catalog.departments.length > 0) {
                    bookingState.deptId = catalog.departments[0].id;
                }

                renderBookingModal();
                refreshBookingAvailability();
            })
            .catch(function () {
                var bodyEl = document.getElementById("modalBody");
                if (bodyEl) bodyEl.innerHTML = '<div class="validation-summary-errors">Unable to load clinic schedules right now. Please try again.</div>';
            });
    }

    function renderBookingModal() {
        var catalog = bookingState.catalog;
        var bodyEl = document.getElementById("modalBody");
        var footEl = document.getElementById("modalFooter");
        if (!catalog || !bodyEl || !footEl) return;

        var depts = catalog.departments || [];
        var docs = catalog.doctors || [];

        if (bookingState.mode === "dept") {
            docs = docs.filter(function (d) {
                return !bookingState.deptId || d.departmentId === bookingState.deptId;
            });
            if (!bookingState.doctorId || !docs.some(function (d) { return d.id === bookingState.doctorId; })) {
                bookingState.doctorId = docs.length > 0 ? docs[0].id : null;
            }
        }

        var deptOptions = depts.map(function (d) {
            var sel = d.id === bookingState.deptId ? " selected" : "";
            return '<option value="' + d.id + '"' + sel + '>' + escapeHtml(d.name) + ' (' + escapeHtml(d.floorLocation) + ')</option>';
        }).join("");

        var docHtml = docs.map(function (d) {
            var isSel = d.id === bookingState.doctorId;
            var priceText = d.isFollowUp
                ? ('Follow-Up: ' + Math.round(d.followUpFee) + ' EGP')
                : (Math.round(d.fee) + ' EGP &bull; Follow-Up: ' + Math.round(d.followUpFee) + ' EGP');
            return '<button type="button" class="book-doc-item' + (isSel ? ' selected' : '') + '" data-pick-doc="' + escapeHtml(d.id) + '">' +
                '<img class="book-doc-img" src="/images/doctors/' + escapeHtml(d.image) + '" alt="' + escapeHtml(d.name) + '" />' +
                '<div style="min-width:0;">' +
                '<div class="book-doc-title">' + escapeHtml(d.name) + '</div>' +
                '<div class="book-doc-sub">' + escapeHtml(d.departmentName) + ' &bull; ' + escapeHtml(d.clinicRoom) + '</div>' +
                '<div class="book-doc-price tabular">' + priceText + '</div>' +
                '</div>' +
                '</button>';
        }).join("");

        var staffBanner = !catalog.isPatient
            ? '<div class="auth-light-alert" style="margin-bottom:14px;">Previewing live patient booking slots. Sign in with a patient account to complete an online self-booking.</div>'
            : '';

        bodyEl.innerHTML =
            staffBanner +
            '<div class="book-mode-tabs" role="tablist">' +
            '<button type="button" class="book-mode-tab' + (bookingState.mode === "dept" ? " active" : "") + '" id="bookTabDept">1. Department &rarr; Doctor</button>' +
            '<button type="button" class="book-mode-tab' + (bookingState.mode === "day" ? " active" : "") + '" id="bookTabDay">2. Browse Available by Day</button>' +
            '</div>' +
            '<div class="book-field-grid">' +
            (bookingState.mode === "dept"
                ? '<div class="book-field"><label class="book-label" for="bookDeptSelect">Medical Department</label><select class="book-select" id="bookDeptSelect">' + deptOptions + '</select></div>'
                : '<div class="book-field"><label class="book-label" for="bookDeptSelect">Filter Department (Optional)</label><select class="book-select" id="bookDeptSelect"><option value="0">All Departments Open on Day</option>' + deptOptions + '</select></div>') +
            '<div class="book-field"><label class="book-label" for="bookDateInput">Consultation Date</label><input type="date" class="book-input" id="bookDateInput" min="' + escapeHtml(catalog.minDate) + '" value="' + escapeHtml(bookingState.date) + '" /></div>' +
            '<div class="book-field full"><label class="book-label">' + (bookingState.mode === "dept" ? "Choose Attending Specialist" : "Specialists Holding Clinics on Selected Day") + '</label>' +
            '<div class="book-doc-strip" id="bookDocList">' + (docHtml || '<div style="font-size:13px;color:#64748B;padding:12px;">No active doctors found for this selection.</div>') + '</div></div>' +
            '<div class="book-field full"><label class="book-label">Available Time Slots (Open vs Booked)</label>' +
            '<div class="book-slots-wrap" id="bookSlotsWrap"><span style="font-size:13px;color:#64748B;">Loading time slots&hellip;</span></div></div>' +
            '<div class="book-field full"><label class="book-label" for="bookNotesInput">Reason for Consultation / Symptoms (Optional)</label>' +
            '<input type="text" class="book-input" id="bookNotesInput" maxlength="240" placeholder="e.g. Chest discomfort follow-up or routine check" /></div>' +
            '</div>' +
            '<div id="bookErrorBox"></div>';

        footEl.innerHTML =
            '<button type="button" class="btn btn-outline btn-sm" id="btnCancelBooking">Cancel</button>' +
            '<button type="button" class="btn btn-navy btn-sm" id="btnConfirmBooking"' + (catalog.isPatient ? '' : ' disabled') + '>Confirm Appointment</button>';

        document.getElementById("bookTabDept").addEventListener("click", function () {
            bookingState.mode = "dept";
            bookingState.slotMinutes = null;
            if (!bookingState.deptId && depts.length > 0) bookingState.deptId = depts[0].id;
            renderBookingModal();
            refreshBookingAvailability();
        });

        document.getElementById("bookTabDay").addEventListener("click", function () {
            bookingState.mode = "day";
            bookingState.deptId = 0;
            bookingState.slotMinutes = null;
            renderBookingModal();
            refreshBookingAvailability();
        });

        document.getElementById("bookDeptSelect").addEventListener("change", function (e) {
            bookingState.deptId = parseInt(e.target.value, 10) || 0;
            bookingState.slotMinutes = null;
            renderBookingModal();
            refreshBookingAvailability();
        });

        document.getElementById("bookDateInput").addEventListener("change", function (e) {
            bookingState.date = e.target.value || catalog.minDate;
            bookingState.slotMinutes = null;
            refreshBookingAvailability();
        });

        document.querySelectorAll("[data-pick-doc]").forEach(function (btn) {
            btn.addEventListener("click", function () {
                bookingState.doctorId = btn.getAttribute("data-pick-doc");
                bookingState.slotMinutes = null;
                document.querySelectorAll("[data-pick-doc]").forEach(function (x) { x.classList.remove("selected"); });
                btn.classList.add("selected");
                refreshBookingAvailability();
            });
        });

        document.getElementById("btnCancelBooking").addEventListener("click", closeModal);
        document.getElementById("btnConfirmBooking").addEventListener("click", submitPublicBooking);
    }

    function refreshBookingAvailability() {
        var wrap = document.getElementById("bookSlotsWrap");
        var docStrip = document.getElementById("bookDocList");
        if (!wrap) return;
        wrap.innerHTML = '<span style="font-size:13px;color:#64748B;">Checking live schedule&hellip;</span>';

        var qs = "date=" + encodeURIComponent(bookingState.date);
        if (bookingState.deptId) qs += "&departmentId=" + encodeURIComponent(bookingState.deptId);
        if (bookingState.mode === "dept" && bookingState.doctorId) {
            qs += "&doctorId=" + encodeURIComponent(bookingState.doctorId);
        }

        fetch("/Appointments/PublicAvailability?" + qs, { headers: { "Accept": "application/json" } })
            .then(function (r) { return r.json(); })
            .then(function (data) {
                var clinics = data.clinics || [];

                if (bookingState.mode === "day" && docStrip) {
                    if (clinics.length === 0) {
                        docStrip.innerHTML = '<div style="font-size:13px;color:#64748B;padding:12px;">No clinics are scheduled on ' + escapeHtml(data.dayName) + '. Try another date.</div>';
                    } else {
                        if (!bookingState.doctorId || !clinics.some(function (c) { return c.doctorId === bookingState.doctorId; })) {
                            bookingState.doctorId = clinics[0].doctorId;
                        }
                        var allDocs = (bookingState.catalog && bookingState.catalog.doctors) || [];
                        docStrip.innerHTML = clinics.map(function (c) {
                            var meta = allDocs.find(function (d) { return d.id === c.doctorId; }) || {};
                            var isSel = c.doctorId === bookingState.doctorId;
                            return '<button type="button" class="book-doc-item' + (isSel ? ' selected' : '') + '" data-pick-doc="' + escapeHtml(c.doctorId) + '">' +
                                '<img class="book-doc-img" src="/images/doctors/' + escapeHtml(meta.image || "doc-salma.jpg") + '" alt="' + escapeHtml(c.doctorName) + '" />' +
                                '<div style="min-width:0;">' +
                                '<div class="book-doc-title">' + escapeHtml(c.doctorName) + '</div>' +
                                '<div class="book-doc-sub">' + escapeHtml(c.departmentName) + ' &bull; ' + c.openCount + ' slots open</div>' +
                                '<div class="book-doc-price tabular">' + (c.isFollowUp ? ('Follow-Up: ' + Math.round(c.followUpFee) + ' EGP') : (Math.round(c.fee) + ' EGP')) + '</div>' +
                                '</div></button>';
                        }).join("");

                        docStrip.querySelectorAll("[data-pick-doc]").forEach(function (btn) {
                            btn.addEventListener("click", function () {
                                bookingState.doctorId = btn.getAttribute("data-pick-doc");
                                bookingState.slotMinutes = null;
                                docStrip.querySelectorAll("[data-pick-doc]").forEach(function (x) { x.classList.remove("selected"); });
                                btn.classList.add("selected");
                                renderSlotsFromClinics(clinics, wrap);
                            });
                        });
                    }
                }

                renderSlotsFromClinics(clinics, wrap);
            })
            .catch(function () {
                wrap.innerHTML = '<span style="font-size:13px;color:#B91C1C;">Could not load live slots for that day.</span>';
            });
    }

    function renderSlotsFromClinics(clinics, wrap) {
        var clinic = clinics.find(function (c) { return c.doctorId === bookingState.doctorId; });
        if (!clinic || !clinic.slots || clinic.slots.length === 0) {
            wrap.innerHTML = '<span style="font-size:13px;color:#64748B;">This physician does not hold clinic hours on the chosen day. Try another date or switch to Browse by Day.</span>';
            bookingState.slotMinutes = null;
            return;
        }

        if (bookingState.slotMinutes === null) {
            var firstOpen = clinic.slots.find(function (s) { return s.isAvailable; });
            bookingState.slotMinutes = firstOpen ? firstOpen.slotMinutes : null;
        }

        wrap.innerHTML = clinic.slots.map(function (s) {
            var sel = s.slotMinutes === bookingState.slotMinutes && s.isAvailable;
            return '<button type="button" class="book-slot-pill' + (sel ? ' selected' : '') + '" data-slot-min="' + s.slotMinutes + '"' + (s.isAvailable ? '' : ' disabled title="Booked"') + '>' +
                escapeHtml(s.timeLabel) + (s.isAvailable ? '' : ' &bull; Booked') +
                '</button>';
        }).join("");

        wrap.querySelectorAll("[data-slot-min]").forEach(function (pill) {
            pill.addEventListener("click", function () {
                bookingState.slotMinutes = parseInt(pill.getAttribute("data-slot-min"), 10);
                wrap.querySelectorAll("[data-slot-min]").forEach(function (p) { p.classList.remove("selected"); });
                pill.classList.add("selected");
            });
        });
    }

    function submitPublicBooking() {
        var errBox = document.getElementById("bookErrorBox");
        var btn = document.getElementById("btnConfirmBooking");
        if (!bookingState.doctorId || bookingState.slotMinutes === null) {
            if (errBox) errBox.innerHTML = '<div class="auth-light-alert error">Please select an open consultation time slot first.</div>';
            return;
        }

        var tokenMeta = document.querySelector('meta[name="csrf-token"]');
        var token = tokenMeta ? tokenMeta.getAttribute("content") : "";
        var notesEl = document.getElementById("bookNotesInput");
        var notes = notesEl ? notesEl.value : "";

        if (btn) {
            btn.disabled = true;
            btn.textContent = "Confirming…";
        }

        fetch("/Appointments/PublicBook", {
            method: "POST",
            headers: {
                "Content-Type": "application/json",
                "Accept": "application/json",
                "RequestVerificationToken": token
            },
            body: JSON.stringify({
                doctorId: bookingState.doctorId,
                date: bookingState.date,
                slotMinutes: bookingState.slotMinutes,
                notes: notes
            })
        })
            .then(function (r) { return r.json(); })
            .then(function (res) {
                if (!res.ok) {
                    if (errBox) errBox.innerHTML = '<div class="auth-light-alert error">' + escapeHtml(res.message) + '</div>';
                    if (btn) {
                        btn.disabled = false;
                        btn.textContent = "Confirm Appointment";
                    }
                    refreshBookingAvailability();
                    return;
                }

                var bodyEl = document.getElementById("modalBody");
                var footEl = document.getElementById("modalFooter");
                if (bodyEl) {
                    bodyEl.innerHTML =
                        '<div class="book-receipt-card">' +
                        '<div class="book-receipt-badge">Appointment Confirmed &bull; ' + escapeHtml(res.appointmentNumber) + '</div>' +
                        '<h3 style="margin:4px 0 6px;color:#0F172A;">We look forward to welcoming you</h3>' +
                        '<p style="font-size:13px;color:#475569;margin:0;">Your consultation request has been logged and synced to your My Updates timeline.</p>' +
                        '<div class="book-receipt-details">' +
                        '<div><strong>Physician</strong><div>' + escapeHtml(res.doctorName) + '</div></div>' +
                        '<div><strong>Department</strong><div>' + escapeHtml(res.departmentName) + '</div></div>' +
                        '<div><strong>Date &amp; Slot</strong><div>' + escapeHtml(res.dateLabel) + ' &bull; ' + escapeHtml(res.timeLabel) + '</div></div>' +
                        '<div><strong>Consultation Rate</strong><div>' + escapeHtml(res.visitType) + ' &bull; ' + escapeHtml(res.fee) + '</div></div>' +
                        '</div></div>';
                }
                if (footEl) {
                    footEl.innerHTML =
                        '<button type="button" class="btn btn-outline btn-sm" id="btnDoneReceipt">Done</button>' +
                        '<a class="btn btn-navy btn-sm" href="/Updates" style="text-decoration:none;">View in My Updates &rarr;</a>';
                    var doneBtn = document.getElementById("btnDoneReceipt");
                    if (doneBtn) doneBtn.addEventListener("click", closeModal);
                }
                showToast("Appointment " + res.appointmentNumber + " scheduled.", false);
            })
            .catch(function () {
                if (errBox) errBox.innerHTML = '<div class="auth-light-alert error">Network error while saving reservation. Please try again.</div>';
                if (btn) {
                    btn.disabled = false;
                    btn.textContent = "Confirm Appointment";
                }
            });
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
                    btn.getAttribute("data-book-followup"),
                    btn.getAttribute("data-book-dept-id")
                );
            });
        });

        document.querySelectorAll("[data-clear-date]").forEach(function (btn) {
            btn.addEventListener("click", function () {
                var targetId = btn.getAttribute("data-clear-date");
                var input = targetId ? document.getElementById(targetId) : null;
                if (input) {
                    input.value = "";
                    input.focus();
                }
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

        var profileFileInput = document.getElementById("imageFile");
        var profileFileName = document.getElementById("profileImageFileName");
        if (profileFileInput && profileFileName) {
            profileFileInput.addEventListener("change", function () {
                if (profileFileInput.files && profileFileInput.files.length > 0) {
                    profileFileName.textContent = profileFileInput.files[0].name;
                } else {
                    profileFileName.textContent = "PNG, JPG, or WebP (max 5 MB)";
                }
            });
        }

        var heroVideo = document.getElementById("manaraHeroVideo");
        if (heroVideo) {
            heroVideo.muted = true;
            heroVideo.play().catch(function () { });
        }

        // Persist and synchronize accessibility preferences
        try {
            var savedFont = localStorage.getItem("manara_ws_font_large");
            if (savedFont === "large" || savedFont === "1") {
                document.body.setAttribute("data-font-size", "large");
            }
            var savedDensity = localStorage.getItem("manara_ws_density");
            if (savedDensity === "compact") {
                document.body.setAttribute("data-density", "compact");
            }
            var savedMotion = localStorage.getItem("manara_ws_motion");
            if (savedMotion === "reduced") {
                document.body.setAttribute("data-motion", "reduced");
            }
        } catch (err) { }

        var fontSelect = document.getElementById("wsFontSizeSelect");
        if (fontSelect) {
            fontSelect.addEventListener("change", function () {
                document.body.setAttribute("data-font-size", fontSelect.value);
                try { localStorage.setItem("manara_ws_font_large", fontSelect.value); } catch (err) { }
            });
        }

        var densitySelect = document.getElementById("wsDensitySelect");
        if (densitySelect) {
            densitySelect.addEventListener("change", function () {
                document.body.setAttribute("data-density", densitySelect.value);
                try { localStorage.setItem("manara_ws_density", densitySelect.value); } catch (err) { }
            });
        }

        var motionToggle = document.getElementById("wsReducedMotionToggle");
        if (motionToggle) {
            motionToggle.addEventListener("change", function () {
                var val = motionToggle.checked ? "reduced" : "full";
                document.body.setAttribute("data-motion", val);
                try { localStorage.setItem("manara_ws_motion", val); } catch (err) { }
            });
        }

        // Doctor consultation reply template buttons
        document.querySelectorAll("[data-reply-template]").forEach(function (btn) {
            btn.addEventListener("click", function () {
                var targetId = btn.getAttribute("data-reply-target");
                var text = btn.getAttribute("data-reply-template");
                var field = targetId ? document.getElementById(targetId) : null;
                if (field && text) {
                    field.value = text;
                    field.focus();
                }
            });
        });

        // MANARA Medical Guidance Assistant form submission
        var assistantForm = document.getElementById("medicalAssistantForm");
        if (assistantForm) {
            assistantForm.addEventListener("submit", function (e) {
                e.preventDefault();
                var input = document.getElementById("assistantPromptInput");
                var log = document.getElementById("assistantChatLog");
                var metaToken = document.querySelector("meta[name='csrf-token']");
                if (!input || !log) return;

                var prompt = input.value.trim();
                if (!prompt) return;

                var userBubble = document.createElement("div");
                userBubble.className = "assistant-bubble user";
                userBubble.textContent = prompt;
                log.appendChild(userBubble);
                input.value = "";
                log.scrollTop = log.scrollHeight;

                var headers = { "Content-Type": "application/json" };
                if (metaToken) {
                    headers["RequestVerificationToken"] = metaToken.getAttribute("content") || "";
                }

                fetch("/Consult/AskAssistant", {
                    method: "POST",
                    headers: headers,
                    body: JSON.stringify({ prompt: prompt })
                })
                .then(function (r) { return r.json(); })
                .then(function (data) {
                    var botBubble = document.createElement("div");
                    botBubble.className = "assistant-bubble bot" + (data.isUrgent ? " urgent" : "");
                    var html = "";
                    if (data.suggestedDepartment) {
                        html += '<div style="font-size:11px;font-weight:700;text-transform:uppercase;color:#1D4ED8;margin-bottom:4px;">Suggested Department: ' + data.suggestedDepartment + (data.suggestedDoctorName ? ' (' + data.suggestedDoctorName + ')' : '') + '</div>';
                    }
                    var textNode = document.createElement("div");
                    textNode.textContent = data.answer || "Please consult our medical desk.";
                    botBubble.innerHTML = html;
                    botBubble.appendChild(textNode);

                    if (data.disclaimer) {
                        var disc = document.createElement("div");
                        disc.className = "assistant-disclaimer";
                        disc.textContent = data.disclaimer;
                        botBubble.appendChild(disc);
                    }
                    log.appendChild(botBubble);
                    log.scrollTop = log.scrollHeight;
                })
                .catch(function () {
                    var errBubble = document.createElement("div");
                    errBubble.className = "assistant-bubble bot";
                    errBubble.textContent = "Unable to reach the guidance assistant right now. You may submit your question directly to a specialist using the consultation form.";
                    log.appendChild(errBubble);
                });
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
        initAuthPixelArtCanvas();
        initCascadingBooking();
    });

    function initCascadingBooking() {
        var deptSelect = document.getElementById("bookingDeptSelect");
        var docSelect = document.getElementById("bookingDoctorSelect");
        var cardContainer = document.getElementById("bookingDoctorCards");
        if (!deptSelect || !docSelect || !cardContainer) return;

        var cards = Array.prototype.slice.call(cardContainer.querySelectorAll("[data-select-doctor]"));
        var options = Array.prototype.slice.call(docSelect.querySelectorAll("option[data-dept-id]"));

        function applyDepartmentFilter() {
            var chosenDept = deptSelect.value;
            var currentDoc = docSelect.value;
            var currentStillValid = false;

            options.forEach(function (opt) {
                var match = !chosenDept || opt.getAttribute("data-dept-id") === chosenDept;
                opt.hidden = !match;
                opt.disabled = !match;
                if (match && opt.value === currentDoc) currentStillValid = true;
            });

            if (!currentStillValid && chosenDept) {
                docSelect.value = "";
            }

            cards.forEach(function (card) {
                var match = !chosenDept || card.getAttribute("data-dept-id") === chosenDept;
                card.style.display = match ? "flex" : "none";
                card.classList.toggle("selected", docSelect.value === card.getAttribute("data-select-doctor"));
            });
        }

        deptSelect.addEventListener("change", applyDepartmentFilter);

        docSelect.addEventListener("change", function () {
            var sel = docSelect.options[docSelect.selectedIndex];
            var selDept = sel ? sel.getAttribute("data-dept-id") : "";
            if (selDept && !deptSelect.value) {
                deptSelect.value = selDept;
            }
            cards.forEach(function (c) {
                c.classList.toggle("selected", c.getAttribute("data-select-doctor") === docSelect.value);
            });
        });

        cards.forEach(function (card) {
            card.addEventListener("click", function () {
                var id = card.getAttribute("data-select-doctor");
                var cardDept = card.getAttribute("data-dept-id");
                if (cardDept && !deptSelect.value) {
                    deptSelect.value = cardDept;
                    applyDepartmentFilter();
                }
                docSelect.value = id;
                cards.forEach(function (c) {
                    c.classList.toggle("selected", c.getAttribute("data-select-doctor") === id);
                });
            });
        });

        applyDepartmentFilter();
    }

    function initAuthPixelArtCanvas() {
        var canvas = document.getElementById("authPixelCanvas");
        if (!canvas || !canvas.getContext) return;
        if (window.matchMedia && window.matchMedia("(prefers-reduced-motion: reduce)").matches) return;

        var ctx = canvas.getContext("2d");
        var px = 5;
        var w = 0;
        var h = 0;
        var motifs = [];

        function resize() {
            var rect = canvas.parentElement ? canvas.parentElement.getBoundingClientRect() : canvas.getBoundingClientRect();
            w = canvas.width = Math.max(300, Math.floor(rect.width));
            h = canvas.height = Math.max(400, Math.floor(rect.height));
            motifs = [];
            var count = 18;
            for (var i = 0; i < count; i++) {
                motifs.push({
                    x: Math.floor((Math.random() * w) / px) * px,
                    y: Math.floor((Math.random() * h) / px) * px,
                    vy: 0.25 + Math.random() * 0.35,
                    kind: i % 3, // 0: pixel cross, 1: pixel heart/pulse block, 2: sparkle diamond
                    alpha: 0.14 + Math.random() * 0.22
                });
            }
        }

        function drawCross(x, y, a) {
            ctx.fillStyle = "rgba(186, 230, 253, " + a.toFixed(2) + ")";
            ctx.fillRect(x, y - px, px, px * 3);
            ctx.fillRect(x - px, y, px * 3, px);
        }

        function drawSpark(x, y, a) {
            ctx.fillStyle = "rgba(147, 197, 253, " + a.toFixed(2) + ")";
            ctx.fillRect(x, y, px, px);
            ctx.fillRect(x + px * 2, y + px, px, px);
            ctx.fillRect(x - px, y + px * 2, px, px);
        }

        var ecgOffset = 0;
        var ecgPattern = [0, 0, -1, -2, 0, 3, -5, 2, 0, -1, 0, 0, 0, 0, 0];

        function frame() {
            ctx.clearRect(0, 0, w, h);

            // Subtle pixelated ECG pulse wave along bottom third
            var baseY = Math.floor((h * 0.82) / px) * px;
            for (var x = 0; x < w; x += px) {
                var idx = Math.floor(((x / px) + ecgOffset) % ecgPattern.length);
                if (idx < 0) idx += ecgPattern.length;
                var dy = ecgPattern[idx] * px;
                ctx.fillStyle = "rgba(125, 211, 252, 0.26)";
                ctx.fillRect(x, baseY + dy, px - 1, px - 1);
            }
            ecgOffset += 0.16;

            for (var i = 0; i < motifs.length; i++) {
                var m = motifs[i];
                var snapY = Math.floor(m.y / px) * px;
                if (m.kind === 0) drawCross(m.x, snapY, m.alpha);
                else drawSpark(m.x, snapY, m.alpha);

                m.y -= m.vy;
                if (m.y < -px * 4) {
                    m.y = h + px * 2;
                    m.x = Math.floor((Math.random() * w) / px) * px;
                }
            }

            window.requestAnimationFrame(frame);
        }

        resize();
        window.addEventListener("resize", resize);
        window.requestAnimationFrame(frame);
    }

    window.manaraShowToast = showToast;
})();
