// CardioCare Quest: accessibility helpers for every Twine game.
//
// The Flutter hosts (TwineGameHost, TwineQuestionnaireHost) inject this file into every game after
// ccq_bridge.js. It changes nothing a sighted player sees. It only adds what screen readers,
// keyboards, switch access and voice control need:
//
//   1. Page language          -> <html lang="en"> when the game does not set one
//   2. Screen-change notice   -> after each passage, focus moves to the new screen's heading (or its
//                                first line of text) and a short summary is announced
//   3. Meters with values     -> .meter-bar becomes a progress bar that says its label and percent
//   4. Header Home / Menu     -> real buttons: named, reachable by Tab, work with Enter and Space,
//                                and at least 44x44 to touch (the icon itself looks the same)
//
// Safe to inject more than once, and it does nothing on pages without SugarCube.
(function () {
  'use strict';
  if (window.__ccqA11y) { return; }
  window.__ccqA11y = true;

  // ---------- 1. Page language ----------
  if (!document.documentElement.getAttribute('lang')) {
    document.documentElement.setAttribute('lang', 'en');
  }

  // ---------- shared: a visually hidden live region ----------
  function liveRegion() {
    var el = document.getElementById('ccq-sr-announce');
    if (el) { return el; }
    el = document.createElement('div');
    el.id = 'ccq-sr-announce';
    el.setAttribute('aria-live', 'polite');
    el.setAttribute('aria-atomic', 'true');
    // Standard "visually hidden" pattern: still read by screen readers.
    el.style.cssText = 'position:absolute;width:1px;height:1px;margin:-1px;padding:0;' +
      'overflow:hidden;clip:rect(0 0 0 0);clip-path:inset(50%);white-space:nowrap;border:0;';
    document.body.appendChild(el);
    return el;
  }

  function announce(text) {
    if (!text) { return; }
    var el = liveRegion();
    el.textContent = '';
    // A short delay makes screen readers treat it as a new message.
    setTimeout(function () { el.textContent = text; }, 120);
  }

  function visible(el) {
    if (!el) { return false; }
    var r = el.getBoundingClientRect();
    var cs = window.getComputedStyle(el);
    return r.width > 0 && r.height > 0 && cs.visibility !== 'hidden' && cs.display !== 'none';
  }

  function cleanText(s) {
    return (s || '').replace(/\s+/g, ' ').trim();
  }

  // ---------- 2. Screen-change notice and focus ----------
  // Picks what a screen reader should land on: the first visible heading, else the first line of
  // real text in the passage.
  function focusTarget(root) {
    var heads = root.querySelectorAll('h1, h2, h3');
    for (var i = 0; i < heads.length; i++) { if (visible(heads[i]) && cleanText(heads[i].textContent)) { return heads[i]; } }
    var candidates = root.querySelectorAll('.scene, .narrative, .result-verdict, .final-headline, .question, .question-prompt, p, .content > div');
    for (var j = 0; j < candidates.length; j++) {
      var c = candidates[j];
      if (visible(c) && cleanText(c.textContent).length > 3 && !c.querySelector('a, button, input')) { return c; }
    }
    return null;
  }

  var firstDisplay = true;
  function onPassageShown() {
    var root = document.getElementById('passages') || document.body;
    enhance(root);
    if (firstDisplay) { firstDisplay = false; return; } // do not pull focus on the opening screen
    var target = focusTarget(root);
    if (target) {
      if (!target.hasAttribute('tabindex')) { target.setAttribute('tabindex', '-1'); }
      target.style.outline = 'none'; // programmatic focus only; no ring on a heading
      try { target.focus({ preventScroll: true }); } catch (e) { target.focus(); }
      announce(cleanText(target.textContent).slice(0, 160));
    }
  }

  // ---------- 3. Meters with values ----------
  function enhanceMeters(root) {
    var rows = root.querySelectorAll('.meter-row');
    for (var i = 0; i < rows.length; i++) {
      var label = rows[i].querySelector('.meter-label');
      var bar = rows[i].querySelector('.meter-bar');
      var fill = rows[i].querySelector('.meter-bar-fill');
      if (!bar || !fill) { continue; }
      var m = (fill.getAttribute('style') || '').match(/width:\s*([\d.]+)%/);
      var pct = m ? Math.round(parseFloat(m[1])) : 0;
      var name = label ? cleanText(label.textContent) : 'Progress';
      bar.setAttribute('role', 'progressbar');
      bar.setAttribute('aria-label', name);
      bar.setAttribute('aria-valuemin', '0');
      bar.setAttribute('aria-valuemax', '100');
      bar.setAttribute('aria-valuenow', String(pct));
      bar.setAttribute('aria-valuetext', name + ' ' + pct + ' percent');
      if (label) { label.setAttribute('aria-hidden', 'true'); } // the bar now says it
    }
  }

  // ---------- 4. Header Home / Menu as real buttons ----------
  function makeButton(el, name) {
    if (!el || el.getAttribute('data-ccq-a11y') === '1') { return; }
    el.setAttribute('data-ccq-a11y', '1');
    if (!/^(BUTTON|A)$/.test(el.tagName) || !el.hasAttribute('href')) {
      el.setAttribute('role', 'button');
      if (!el.hasAttribute('tabindex')) { el.setAttribute('tabindex', '0'); }
    }
    if (!el.getAttribute('aria-label')) { el.setAttribute('aria-label', name); }
    el.addEventListener('keydown', function (ev) {
      if (ev.key === 'Enter' || ev.key === ' ' || ev.key === 'Spacebar') { ev.preventDefault(); el.click(); }
    });
    // Grow the touch area to 44x44 without moving the icon: extra padding, cancelled by margin.
    var r = el.getBoundingClientRect();
    var padX = Math.max(0, Math.ceil((44 - r.width) / 2));
    var padY = Math.max(0, Math.ceil((44 - r.height) / 2));
    if (padX || padY) {
      el.style.padding = padY + 'px ' + padX + 'px';
      el.style.margin = (-padY) + 'px ' + (-padX) + 'px';
      el.style.boxSizing = 'content-box';
    }
  }

  function enhanceHeaderButtons(root) {
    var homes = document.querySelectorAll('.ccq-home-btn');
    for (var i = 0; i < homes.length; i++) { makeButton(homes[i], 'Go to the CardioCare Quest home screen'); }
    var menus = document.querySelectorAll('.menu-icon');
    for (var j = 0; j < menus.length; j++) { makeButton(menus[j], 'Open menu'); }
  }

  // ---------- 5. Reduce motion (issue #23) ----------
  // When the phone asks for less motion, decorative animation stops. The two
  // breathing guides (Quiet Minute, Vascular Village stretch) keep their pace
  // but fade instead of growing and shrinking. "Breathe in / out" prompts and
  // timer fill bars only change opacity or fill, so they keep running.
  function addReducedMotionStyles() {
    if (document.getElementById('ccq-reduced-motion')) { return; }
    var css =
      '@media (prefers-reduced-motion: reduce) {' +
      '  *:not([class*="prompt"]):not([class*="fill"]):not([class*="Fill"]):not(.breath-circle):not(.stretch-circle),' +
      '  *:not([class*="prompt"]):not([class*="fill"]):not([class*="Fill"]):not(.breath-circle):not(.stretch-circle)::before,' +
      '  *:not([class*="prompt"]):not([class*="fill"]):not([class*="Fill"]):not(.breath-circle):not(.stretch-circle)::after {' +
      '    animation-duration: 0.01ms !important; animation-iteration-count: 1 !important;' +
      '    transition-duration: 0.01ms !important; scroll-behavior: auto !important; }' +
      '  .breath-circle { animation: ccq-calm-fade 10s ease-in-out infinite !important; }' +
      '  .stretch-circle { animation: ccq-calm-fade 8s ease-in-out infinite !important; }' +
      '}' +
      '@keyframes ccq-calm-fade { 0%, 100% { opacity: 0.45; } 50% { opacity: 1; } }';
    var style = document.createElement('style');
    style.id = 'ccq-reduced-motion';
    style.textContent = css;
    (document.head || document.documentElement).appendChild(style);
  }

  // ---------- 6. A heading on every screen (issue #21) ----------
  // Screen-reader users jump between headings. Screens that have none get
  // their header title marked as a level-1 heading (nothing visible changes).
  function ensureHeading(root) {
    var scope = root && root.querySelector ? root : document;
    var real = scope.querySelectorAll('h1, h2, h3, [role="heading"]');
    for (var i = 0; i < real.length; i++) { if (visible(real[i]) && cleanText(real[i].textContent)) { return; } }
    var candidates = ['.phone .header > span', '.phone .header', '.header-title', '.title', '.q-text', '.scene'];
    for (var j = 0; j < candidates.length; j++) {
      var el = scope.querySelector(candidates[j]);
      if (el && visible(el) && cleanText(el.textContent)) {
        el.setAttribute('role', 'heading');
        el.setAttribute('aria-level', '1');
        return;
      }
    }
    // No title on screen at all (e.g. Zombie Chase): add a hidden heading
    // with the game's name so the screen still has a starting point.
    var passage = document.querySelector('#passages .passage') || document.getElementById('passages');
    if (passage && !passage.querySelector('.ccq-sr-heading') && document.title) {
      var h = document.createElement('h1');
      h.className = 'ccq-sr-heading';
      h.textContent = document.title;
      h.style.cssText = 'position:absolute;width:1px;height:1px;margin:-1px;padding:0;' +
        'overflow:hidden;clip:rect(0 0 0 0);clip-path:inset(50%);white-space:nowrap;border:0;';
      passage.insertBefore(h, passage.firstChild);
    }
  }

  // ---------- 7. Keyboard for role="button" (issue #20) ----------
  // Some game controls are links without href marked role="button". Native
  // buttons react to Enter and Space; these need the same.
  document.addEventListener('keydown', function (ev) {
    var t = ev.target;
    if (!t || !t.getAttribute || t.getAttribute('role') !== 'button') { return; }
    if (t.tagName === 'BUTTON' || t.getAttribute('data-ccq-a11y') === '1') { return; } // handled already
    if (ev.key === 'Enter' || ev.key === ' ' || ev.key === 'Spacebar') { ev.preventDefault(); t.click(); }
  });

  function enhance(root) {
    try { enhanceMeters(root || document); } catch (e) { /* never break a game */ }
    try { enhanceHeaderButtons(root || document); } catch (e) { /* never break a game */ }
    try { ensureHeading(root || document); } catch (e) { /* never break a game */ }
  }
  try { addReducedMotionStyles(); } catch (e) { /* never break a game */ }

  // Run now, after every SugarCube passage, and when the bridge adds header icons later.
  enhance(document);
  if (window.jQuery && window.SugarCube) {
    window.jQuery(document).on(':passagedisplay.ccqa11y', function () { setTimeout(onPassageShown, 60); });
  }
  try {
    var mo = new MutationObserver(function () { enhanceHeaderButtons(document); });
    mo.observe(document.body, { childList: true, subtree: true });
  } catch (e) { /* older WebView: the passage hook still covers it */ }

  window.CCQA11y = { announce: announce, enhance: enhance };
})();
