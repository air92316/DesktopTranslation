/* ================================================
   DesktopTranslation Website — Script
   ================================================ */

(function () {
  "use strict";

  // --- Theme Toggle ---
  const THEME_KEY = "dt-theme";
  const root = document.documentElement;
  const toggleBtn = document.getElementById("themeToggle");

  function getSystemTheme() {
    return window.matchMedia("(prefers-color-scheme: dark)").matches
      ? "dark"
      : "light";
  }

  function applyTheme(theme) {
    root.setAttribute("data-theme", theme);
  }

  function initTheme() {
    const saved = localStorage.getItem(THEME_KEY);
    applyTheme(saved || getSystemTheme());
  }

  if (toggleBtn) {
    toggleBtn.addEventListener("click", function () {
      const current = root.getAttribute("data-theme");
      const next = current === "dark" ? "light" : "dark";
      applyTheme(next);
      localStorage.setItem(THEME_KEY, next);
    });
  }

  // Follow system changes when no manual override
  window
    .matchMedia("(prefers-color-scheme: dark)")
    .addEventListener("change", function (e) {
      if (!localStorage.getItem(THEME_KEY)) {
        applyTheme(e.matches ? "dark" : "light");
      }
    });

  initTheme();

  // --- Scroll Reveal ---
  var revealElements = document.querySelectorAll(".reveal");

  if (revealElements.length > 0 && "IntersectionObserver" in window) {
    var observer = new IntersectionObserver(
      function (entries) {
        entries.forEach(function (entry) {
          if (entry.isIntersecting) {
            entry.target.classList.add("visible");
            observer.unobserve(entry.target);
          }
        });
      },
      { threshold: 0.15, rootMargin: "0px 0px -40px 0px" }
    );

    revealElements.forEach(function (el) {
      observer.observe(el);
    });
  } else {
    // Fallback: show everything
    revealElements.forEach(function (el) {
      el.classList.add("visible");
    });
  }

  // --- Smooth scroll for nav links (fallback for browsers without CSS scroll-behavior) ---
  document.querySelectorAll('a[href^="#"]').forEach(function (anchor) {
    anchor.addEventListener("click", function (e) {
      var targetId = this.getAttribute("href");
      if (targetId === "#") return;
      var target = document.querySelector(targetId);
      if (target) {
        e.preventDefault();
        target.scrollIntoView({ behavior: "smooth", block: "start" });
      }
    });
  });
})();
