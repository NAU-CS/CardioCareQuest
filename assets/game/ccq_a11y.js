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

  function enhance(root) {
    try { enhanceMeters(root || document); } catch (e) { /* never break a game */ }
    try { enhanceHeaderButtons(root || document); } catch (e) { /* never break a game */ }
  }

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
