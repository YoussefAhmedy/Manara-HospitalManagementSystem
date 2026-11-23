/**
 * MANARA Design System & Interactive UI/UX Scripts
 * Unobtrusive, CSP-compliant JavaScript handlers.
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
        setTimeout(function () { el.remove(); }, 3800);
    }

    function openModal(title, bodyHtml, footHtml, isLarge) {
        var titleEl = document.getElementById("modalTitle");
        var bodyEl = document.getElementById("modalBody");
        var footEl = document.getElementById("modalFooter");
        var boxEl = document.getElementById("modalBox");
        var overlayEl = document.getElementById("modalOverlay");

        if (!titleEl || !bodyEl || !footEl || !boxEl || !overlayEl) return;

        titleEl.textContent = title;
        bodyEl.innerHTML = bodyHtml;
        footEl.innerHTML = footHtml;
        boxEl.className = "modal" + (isLarge ? " modal-lg" : "");
        overlayEl.classList.add("open");
    }

    function closeModal() {
        var overlayEl = document.getElementById("modalOverlay");
        if (overlayEl) {
            overlayEl.classList.remove("open");
        }
    }

    function openBookModal(dept, doc) {
        var defaultDept = dept || "Cardiology";
        var defaultDoc = doc || "Dr. Salma El-Sayed";
        var tomorrow = new Date(Date.now() + 86400000).toISOString().slice(0, 10);

        var body = '<form id="manaraBookForm" class="form-grid">' +
            '<div class="form-field full"><label class="form-label">Full Name *</label><input class="input" name="name" placeholder="Enter your full name..." required /></div>' +
            '<div class="form-field"><label class="form-label">Phone Number *</label><input class="input" name="phone" placeholder="+20 100 000 0000" required /></div>' +
            '<div class="form-field"><label class="form-label">Department</label><select class="select" name="dept"><option selected>' + defaultDept + '</option><option>Internal Medicine</option><option>Cardiology</option><option>Orthopedics</option><option>Pediatrics</option><option>Radiology</option><option>Laboratory</option></select></div>' +
            '<div class="form-field"><label class="form-label">Preferred Date *</label><input class="input" type="date" name="date" value="' + tomorrow + '" required /></div>' +
            '<div class="form-field"><label class="form-label">Preferred Time Slot</label><select class="select" name="slot"><option>09:30 AM</option><option>10:30 AM</option><option>11:30 AM</option><option>01:00 PM</option><option>02:30 PM</option></select></div>' +
            '<div class="form-field full"><label class="form-label">Consultation Notes / Reason for Visit</label><input class="input" name="notes" placeholder="Brief reason for consultation..." /></div>' +
            '</form>';

        var foot = '<button type="button" class="btn btn-outline btn-sm" id="btnCancelBook">Cancel</button>' +
            '<button type="button" class="btn btn-navy btn-sm" id="btnConfirmBook">Confirm Booking Request</button>';

        openModal("Book Hospital Appointment", body, foot);

        var cancelBtn = document.getElementById("btnCancelBook");
        if (cancelBtn) cancelBtn.addEventListener("click", closeModal);

        var confirmBtn = document.getElementById("btnConfirmBook");
        if (confirmBtn) {
            confirmBtn.addEventListener("click", function () {
                var form = document.getElementById("manaraBookForm");
                if (!form) return;
                var name = form.elements["name"].value.trim();
                var phone = form.elements["phone"].value.trim();
                if (!name || !phone) {
                    showToast("Name and phone number are required.", true);
                    return;
                }
                closeModal();
                showToast("Thank you, " + name + ". Your appointment request has been submitted. Our desk will confirm via SMS.");
            });
        }
    }

    // Role prefill on Sign-In page
    function prefillRole(accId, email, password, roleTitle) {
        var emailInput = document.getElementById("loginEmailInput");
        var passInput = document.getElementById("loginPassInput");
        var banner = document.getElementById("prefillBanner");
        var bannerText = document.getElementById("prefillBannerText");
        var clearBtn = document.getElementById("clearLoginBtn");

        switchAuthTab("login");

        if (emailInput) emailInput.value = email;
        if (passInput) passInput.value = password;

        if (banner && bannerText) {
            banner.style.display = "flex";
            bannerText.innerHTML = "Loaded credentials for <strong>" + roleTitle + "</strong> — review or click <strong>Sign In</strong>.";
        }
        if (clearBtn) clearBtn.style.display = "inline-flex";

        document.querySelectorAll(".role-account-card").forEach(function (card) {
            var isSelected = card.getAttribute("data-acc") === accId;
            card.classList.toggle("selected", isSelected);
            var statusSpan = card.querySelector(".card-status");
            if (statusSpan) {
                statusSpan.textContent = isSelected ? "Loaded ✓" : "Fill Login →";
            }
        });

        showToast("Credentials loaded for " + roleTitle + ". Click Sign In to open workspace.");
    }

    function clearAuthFields() {
        var emailInput = document.getElementById("loginEmailInput");
        var passInput = document.getElementById("loginPassInput");
        var banner = document.getElementById("prefillBanner");
        var clearBtn = document.getElementById("clearLoginBtn");

        if (emailInput) emailInput.value = "";
        if (passInput) passInput.value = "";
        if (banner) banner.style.display = "none";
        if (clearBtn) clearBtn.style.display = "none";

        document.querySelectorAll(".role-account-card").forEach(function (card) {
            card.classList.remove("selected");
            var statusSpan = card.querySelector(".card-status");
            if (statusSpan) {
                statusSpan.textContent = "Fill Login →";
            }
        });
    }

    function togglePasswordVisibility() {
        var passInput = document.getElementById("loginPassInput");
        var btn = document.getElementById("togglePwBtn");
        if (!passInput || !btn) return;
        if (passInput.type === "password") {
            passInput.type = "text";
            btn.textContent = "Hide";
        } else {
            passInput.type = "password";
            btn.textContent = "Show";
        }
    }

    function switchAuthTab(tab) {
        var loginBtn = document.getElementById("tabLoginBtn");
        var regBtn = document.getElementById("tabRegBtn");
        var loginPane = document.getElementById("loginFormPane");
        var regPane = document.getElementById("registerFormPane");

        if (!loginBtn || !regBtn || !loginPane || !regPane) return;

        if (tab === "register") {
            loginBtn.className = "btn btn-outline btn-sm";
            regBtn.className = "btn btn-navy btn-sm";
            loginPane.style.display = "none";
            regPane.style.display = "block";
        } else {
            loginBtn.className = "btn btn-navy btn-sm";
            regBtn.className = "btn btn-outline btn-sm";
            loginPane.style.display = "block";
            regPane.style.display = "none";
        }
    }

    // Attach listeners on DOMContentLoaded
    document.addEventListener("DOMContentLoaded", function () {
        // Mobile navigation toggle
        var mobileBtn = document.getElementById("mobileMenuBtn");
        var navCenter = document.getElementById("navCenter");
        if (mobileBtn && navCenter) {
            mobileBtn.addEventListener("click", function () {
                navCenter.classList.toggle("open");
            });
        }

        // Modal close button
        var modalClose = document.getElementById("modalCloseBtn");
        if (modalClose) {
            modalClose.addEventListener("click", closeModal);
        }

        // Global booking triggers
        document.querySelectorAll("[data-book-modal]").forEach(function (btn) {
            btn.addEventListener("click", function () {
                var dept = btn.getAttribute("data-book-dept");
                var doc = btn.getAttribute("data-book-doc");
                openBookModal(dept, doc);
            });
        });

        // Booking buttons with data-book-dept
        document.querySelectorAll("[data-book-dept]").forEach(function (btn) {
            btn.addEventListener("click", function () {
                openBookModal(btn.getAttribute("data-book-dept"), null);
            });
        });

        // Booking buttons with data-book-doc
        document.querySelectorAll("[data-book-doc]").forEach(function (btn) {
            btn.addEventListener("click", function () {
                openBookModal(null, btn.getAttribute("data-book-doc"));
            });
        });

        // Auth page interactions
        var tabLoginBtn = document.getElementById("tabLoginBtn");
        if (tabLoginBtn) tabLoginBtn.addEventListener("click", function () { switchAuthTab("login"); });

        var tabRegBtn = document.getElementById("tabRegBtn");
        if (tabRegBtn) tabRegBtn.addEventListener("click", function () { switchAuthTab("register"); });

        var clearBtn = document.getElementById("clearLoginBtn");
        if (clearBtn) clearBtn.addEventListener("click", clearAuthFields);

        var togglePw = document.getElementById("togglePwBtn");
        if (togglePw) togglePw.addEventListener("click", togglePasswordVisibility);

        var switchToRegLink = document.getElementById("switchToRegisterLink");
        if (switchToRegLink) switchToRegLink.addEventListener("click", function () { switchAuthTab("register"); });

        // Role cards click
        document.querySelectorAll(".role-account-card[data-acc]").forEach(function (card) {
            card.addEventListener("click", function () {
                var accId = card.getAttribute("data-acc");
                var email = card.getAttribute("data-email");
                var pass = card.getAttribute("data-pass");
                var title = card.getAttribute("data-title");
                prefillRole(accId, email, pass, title);
            });
        });

        // PACS viewer image filter buttons
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

        // Print buttons
        document.querySelectorAll("[data-action='print']").forEach(function (btn) {
            btn.addEventListener("click", function () {
                window.print();
            });
        });

        // Check query param for prefilling role
        var urlParams = new URLSearchParams(window.location.search);
        var prefillParam = urlParams.get("role");
        if (prefillParam) {
            var targetCard = document.querySelector('.role-account-card[data-acc="' + prefillParam + '"]');
            if (targetCard) {
                targetCard.click();
            }
        }
    });

    // Expose helpers globally if needed
    window.manaraShowToast = showToast;
    window.manaraOpenModal = openModal;
    window.manaraCloseModal = closeModal;
    window.manaraOpenBookModal = openBookModal;
})();
