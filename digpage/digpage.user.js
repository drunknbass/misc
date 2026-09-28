// ==UserScript==
// @name         DIG//PAGE
// @namespace    https://drunknbass.github.io/misc/digpage/
// @version      1.0.0
// @description  Press Alt+Shift+D to turn the page you're viewing into a Dig Dug-style level: tunnel through real content and pump real buttons until they pop. Esc restores the page.
// @author       drunknbass
// @license      MIT
// @homepageURL  https://drunknbass.github.io/misc/digpage/
// @downloadURL  https://drunknbass.github.io/misc/digpage/digpage.user.js
// @updateURL    https://drunknbass.github.io/misc/digpage/digpage.user.js
// @match        *://*/*
// @grant        none
// @run-at       document-idle
// @noframes
// ==/UserScript==
(function () {
  'use strict';
  function digpage() {
/*! DIG//PAGE v1.0: turns the page you're looking at into a Dig Dug-style level.
 *  Your digger tunnels through the real content, and real buttons/links/icons come alive as enemies you pump until they pop.
 *  Technique after Hugo Duprez's destroy.spritefusion.com (page -> cell grid, harmonic crater carve, dithered scorch, falling letters, detaching chunks).
 *  Original code, art and sound. No dependencies, no network requests, nothing stored. Esc restores the page.
 *  MIT License. https://drunknbass.github.io/misc/digpage/ */
(function () {
'use strict';
var W = window, D = document;
// running it again while a game is on screen stops that game (toggle)
var prevHost = D.querySelector('[data-digpage]');
if (prevHost) { try { prevHost.dispatchEvent(new CustomEvent('digpage-quit')); } catch (e) {} if (prevHost.parentNode) prevHost.parentNode.removeChild(prevHost); return; }

// ------------------------------------------------------------------ utils
var clamp = function (v, a, b) { return v < a ? a : v > b ? b : v; };
var seed = (Math.random() * 1e9) | 0;
function rnd() { seed = seed + 0x6D2B79F5 | 0; var t = Math.imul(seed ^ seed >>> 15, 1 | seed); t = t + Math.imul(t ^ t >>> 7, 61 | t) ^ t; return ((t ^ t >>> 14) >>> 0) / 4294967296; }
var rr = function (a, b) { return a + (b - a) * rnd(); };
function hash2(x, y) { var h = (x * 374761393 + y * 668265263) | 0; h = Math.imul(h ^ (h >>> 13), 1274126177); return ((h ^ (h >>> 16)) >>> 0) / 4294967296; }
function Spring(k, c, x) { this.k = k; this.c = c; this.x = x || 0; this.v = 0; }
Spring.prototype.step = function (t, dt) { this.v += (-(this.x - t) * this.k - this.v * this.c) * dt; this.x += this.v * dt; return this.x; };
Spring.prototype.kick = function (v) { this.v += v; };
function parseColor(s) {
  var m = /rgba?\(\s*([\d.]+)[,\s]+([\d.]+)[,\s]+([\d.]+)(?:\s*[,\/]\s*([\d.]+%?))?/.exec(s || '');
  if (!m) return null;
  var a = m[4] === undefined ? 1 : (m[4].slice(-1) === '%' ? parseFloat(m[4]) / 100 : parseFloat(m[4]));
  return [Math.round(+m[1]), Math.round(+m[2]), Math.round(+m[3]), a];
}
var rgb = function (c) { return 'rgb(' + c[0] + ',' + c[1] + ',' + c[2] + ')'; };
function css(el, o) { for (var k in o) el.style[k] = o[k]; return el; }
function mk(tag, o, parent) { var e = D.createElement(tag); if (o) css(e, o); if (parent) parent.appendChild(e); return e; }
function visible(el) {
  if (el.checkVisibility) return el.checkVisibility({ opacityProperty: true, visibilityProperty: true });
  var cs = getComputedStyle(el); return cs.visibility !== 'hidden' && cs.display !== 'none' && +cs.opacity > 0;
}
var bgSure = true;   // set by bgBehind: false when a background-image sits between the text and the first solid color
function bgBehind(el) {
  bgSure = true;
  for (var e = el; e && e.nodeType === 1; e = e.parentElement) {
    var cs = getComputedStyle(e), c = parseColor(cs.backgroundColor);
    if (c && c[3] > 0.5) return c;
    if (cs.backgroundImage && cs.backgroundImage !== 'none') bgSure = false;
  }
  return pageBg;
}
function lum(c) { return (0.2126 * c[0] + 0.7152 * c[1] + 0.0722 * c[2]) / 255; }
function colorDist(a, b) { return Math.abs(a[0] - b[0]) + Math.abs(a[1] - b[1]) + Math.abs(a[2] - b[2]); }

// ------------------------------------------------------------------ constants
var CELL = 4, T = 32;                          // 4px material cells (Hugo uses 2px), 32px Dig Dug tiles
var VOID_A = [26, 15, 48], VOID_B = [34, 21, 63], RIM = [74, 48, 128];  // Hugo's dark indigo crater void + lighter rim
var SCORCH = [40, 18, 48], SC_ALPHA = [0, 0.22, 0.38, 0.55, 0.72];
var SKIP = { SCRIPT: 1, STYLE: 1, NOSCRIPT: 1, TEMPLATE: 1, LINK: 1, META: 1, HEAD: 1, TITLE: 1, BR: 1, WBR: 1, OPTION: 1 };
var PLAYER_SPEED = 4.2, HOSE_MAX = 3.4, HOSE_SPEED = 18;
var KIND_SCORE = { button: 400, input: 500, icon: 300, image: 350, link: 200, text: 150 };

// ------------------------------------------------------------------ session state (rebuilt per screen)
var vw, vh, dpr, GW, GH, COLS, ROWS, offX, offY, NCELL;
var mat, burn, col, glyphAt, rockAt, glyphs, pageBg;
var removed = 0, holesDirty = false, patchDirty = false;
var host, root, layer, cv, ctx, cv2, ctx2, holeC, hctx, hImg, hd, patchC, pctx, actorsEl, hud = {}, msgEl;
var enemies = [], rocks = [], parts = [], letters = [], embersL = [], popups = [], flames = [];
var modified = [], listeners = [], raf = 0, running = false;
var player, hose, G, bonus = null, START;
var keys = {}, dirStack = [], pumpPressed = false, autopilot = false, paused = false;
var trauma = 0, lastT = 0, scanStats = {};
var DIRS = { ArrowUp: [0, -1], KeyW: [0, -1], ArrowDown: [0, 1], KeyS: [0, 1], ArrowLeft: [-1, 0], KeyA: [-1, 0], ArrowRight: [1, 0], KeyD: [1, 0] };

function saveStyle(el) {
  for (var i = 0; i < modified.length; i++) if (modified[i].el === el) return;
  modified.push({ el: el, had: el.hasAttribute('style'), css: el.style.cssText });
}
function restoreStyles() {
  for (var i = modified.length - 1; i >= 0; i--) {
    var m = modified[i];
    try { if (m.had) m.el.style.cssText = m.css; else m.el.removeAttribute('style'); } catch (e) {}
  }
  modified = [];
}
function hideEl(el) { saveStyle(el); el.style.setProperty('opacity', '0', 'important'); el.style.setProperty('transition', 'none', 'important'); }

// ------------------------------------------------------------------ grid helpers
var TXp = function (tx) { return offX + (tx + 0.5) * T; };      // tile -> px center
var TYp = function (ty) { return offY + (ty + 0.5) * T; };
var tileOfX = function (px) { return Math.round((px - offX) / T - 0.5); };
var tileOfY = function (py) { return Math.round((py - offY) / T - 0.5); };
var cellAt = function (px, py) { var cx = Math.floor(px / CELL), cy = Math.floor(py / CELL); return (cx < 0 || cy < 0 || cx >= GW || cy >= GH) ? -1 : cy * GW + cx; };
function airPx(px, py) { var i = cellAt(px, py); return i >= 0 && mat[i] === 0; }
function rockPx(px, py) { var i = cellAt(px, py); return i >= 0 && mat[i] === 9; }
function tileOpen(tx, ty) { return tx >= 0 && ty >= 0 && tx < COLS && ty < ROWS && airPx(TXp(tx), TYp(ty)); }
function passable(ax, ay, bx, by) {
  if (!tileOpen(ax, ay) || !tileOpen(bx, by)) return false;
  var mx = (TXp(ax) + TXp(bx)) / 2, my = (TYp(ay) + TYp(by)) / 2;
  return airPx(mx, my) && airPx(mx + (ax !== bx ? CELL : 0), my + (ay !== by ? CELL : 0)) && airPx(mx - (ax !== bx ? CELL : 0), my - (ay !== by ? CELL : 0));
}
function lineClear(x0, y0, x1, y1) {  // px
  var n = Math.ceil(Math.hypot(x1 - x0, y1 - y0) / 2);
  for (var k = 1; k < n; k++) { var x = x0 + (x1 - x0) * k / n, y = y0 + (y1 - y0) * k / n; if (!airPx(x, y)) return false; }
  return true;
}

// ------------------------------------------------------------------ page -> cells (after Hugo's demo: glyphs + paints serialized onto a cell grid)
function scanPage() {
  var t0 = performance.now();
  mat = new Uint8Array(NCELL); mat.fill(1);
  burn = new Uint8Array(NCELL); col = new Uint32Array(NCELL); glyphAt = new Int32Array(NCELL); glyphAt.fill(-1);
  rockAt = new Int8Array(NCELL); rockAt.fill(-1);
  glyphs = [];
  var htmlBg = parseColor(getComputedStyle(D.documentElement).backgroundColor), bodyBg = D.body ? parseColor(getComputedStyle(D.body).backgroundColor) : null;
  pageBg = (bodyBg && bodyBg[3] > 0.5) ? bodyBg : (htmlBg && htmlBg[3] > 0.5) ? htmlBg : [255, 255, 255, 1];
  if (lum(pageBg) < 0.35) { VOID_A = [8, 4, 16]; VOID_B = [14, 8, 26]; RIM = [150, 110, 230]; SCORCH = [215, 200, 235]; SC_ALPHA = [0, 0.14, 0.24, 0.34, 0.44]; }
  else { VOID_A = [26, 15, 48]; VOID_B = [34, 21, 63]; RIM = [74, 48, 128]; SCORCH = [40, 18, 48]; SC_ALPHA = [0, 0.22, 0.38, 0.55, 0.72]; }
  var colC = mk('canvas'); colC.width = GW; colC.height = GH;
  var cc = colC.getContext('2d', { willReadFrequently: true });
  cc.fillStyle = rgb(pageBg); cc.fillRect(0, 0, GW, GH);
  var tmp = mk('canvas'), tctx = tmp.getContext('2d', { willReadFrequently: true });
  var nEl = 0, nImg = 0, nImgRead = 0;
  function markRect(r, m) {
    var cx0 = Math.max(0, Math.floor(r.left / CELL)), cx1 = Math.min(GW - 1, Math.floor((r.right - 1) / CELL));
    var cy0 = Math.max(0, Math.floor(r.top / CELL)), cy1 = Math.min(GH - 1, Math.floor((r.bottom - 1) / CELL));
    for (var cy = cy0; cy <= cy1; cy++) for (var cx = cx0; cx <= cx1; cx++) mat[cy * GW + cx] = m;
  }
  function fillR(r, c, a) { cc.globalAlpha = a; cc.fillStyle = typeof c === 'string' ? c : rgb(c); cc.fillRect(r.left / CELL, r.top / CELL, r.width / CELL, r.height / CELL); cc.globalAlpha = 1; }
  function drawMedia(el, r) {
    nImg++;
    var w = Math.max(1, Math.round(r.width / CELL)), h = Math.max(1, Math.round(r.height / CELL));
    tmp.width = w; tmp.height = h;
    try { tctx.drawImage(el, 0, 0, w, h); tctx.getImageData(0, 0, 1, 1); cc.drawImage(tmp, r.left / CELL, r.top / CELL, r.width / CELL, r.height / CELL); nImgRead++; return true; }
    catch (e) { fillR(r, [150, 156, 168], 0.85); return false; }  // cross-origin pixels can't be read: neutral fill
  }
  var all = D.body ? D.body.getElementsByTagName('*') : [];
  for (var i = 0; i < all.length && nEl < 9000; i++) {
    var el = all[i];
    if (SKIP[el.tagName] || (el.ownerSVGElement)) continue;
    var r = el.getBoundingClientRect();
    if (r.width < 1 || r.height < 1 || r.bottom <= 0 || r.right <= 0 || r.top >= vh || r.left >= vw) continue;
    nEl++;
    var cs = getComputedStyle(el);
    if (cs.visibility === 'hidden' || +cs.opacity === 0 || cs.display === 'none') continue;
    if (performance.now() - t0 > 700) break;
    var tag = el.tagName.toUpperCase();
    var bg = parseColor(cs.backgroundColor);
    if (bg && bg[3] > 0.04) { fillR(r, bg, bg[3]); if (colorDist(bg, pageBg) > 24) markRect(r, 2); }
    if (cs.backgroundImage && cs.backgroundImage !== 'none') {
      var gm = /rgba?\([^)]*\)/.exec(cs.backgroundImage); var gc = gm && parseColor(gm[0]);
      if (gc) fillR(r, gc, 0.8); else fillR(r, [140, 140, 150], 0.35);
      markRect(r, 4);
    }
    var bw = parseFloat(cs.borderTopWidth) || 0;
    if (bw >= 1) { var bc = parseColor(cs.borderTopColor); if (bc && bc[3] > 0.2) { cc.strokeStyle = rgb(bc); cc.lineWidth = Math.max(0.5, bw / CELL); cc.strokeRect(r.left / CELL, r.top / CELL, r.width / CELL, r.height / CELL); } }
    if (tag === 'IMG' || tag === 'VIDEO' || tag === 'CANVAS') {
      if (tag !== 'IMG' || (el.complete && el.naturalWidth)) drawMedia(el, r); else fillR(r, [200, 200, 205], 0.6);
      markRect(r, 4);
    } else if (tag === 'SVG') {
      var sc = parseColor(cs.fill) || parseColor(cs.color) || [90, 90, 100, 1];
      fillR(r, sc, 0.55); markRect(r, 4);
    } else if (tag === 'IFRAME') { fillR(r, [180, 180, 190], 0.7); markRect(r, 4); }
  }
  // text: one glyph per grapheme via Range rects (same as Hugo's serializer)
  var tw = D.createTreeWalker(D.body || D.documentElement, NodeFilter.SHOW_TEXT, null), range = D.createRange(), info = new Map(), node;
  var MAXG = 9000;
  while ((node = tw.nextNode()) && glyphs.length < MAXG) {
    if (performance.now() - t0 > 1400) break;
    var txt = node.nodeValue; if (!txt || !/\S/.test(txt)) continue;
    var p = node.parentElement; if (!p || SKIP[p.tagName] || p.closest('svg,textarea,select')) continue;
    if (host && host.contains(p)) continue;
    range.selectNodeContents(node);
    var nr = range.getBoundingClientRect();
    if (nr.width < 1 || nr.bottom <= 0 || nr.top >= vh || nr.right <= 0 || nr.left >= vw) continue;
    var inf = info.get(p);
    if (!inf) {
      var pcs = getComputedStyle(p), c = parseColor(pcs.color);
      var bgc = bgBehind(p);
      inf = { ok: visible(p) && c && c[3] > 0.15, font: pcs.fontStyle + ' ' + pcs.fontWeight + ' ' + pcs.fontSize + ' ' + pcs.fontFamily, color: pcs.color, c: c, tt: pcs.textTransform, bg: bgc, sure: bgSure };
      info.set(p, inf);
    }
    if (!inf.ok) continue;
    var L = txt.length, k = 0, first = true;
    while (k < L && glyphs.length < MAXG) {
      var cc0 = txt.charCodeAt(k), len = (cc0 >= 0xD800 && cc0 < 0xDC00) ? 2 : 1, ch = txt.substr(k, len);
      if (/\s/.test(ch)) { k += len; first = true; continue; }
      range.setStart(node, k); range.setEnd(node, k + len); k += len;
      var q = range.getClientRects()[0];
      if (!q || q.width < 0.5 || q.height < 2) continue;
      if (q.top > vh + 40) break;                     // rest of this node is below the fold
      if (q.bottom <= 0 || q.right <= 0 || q.left >= vw) continue;
      if (inf.tt === 'uppercase') ch = ch.toUpperCase(); else if (inf.tt === 'lowercase') ch = ch.toLowerCase(); else if (inf.tt === 'capitalize' && first) ch = ch.toUpperCase();
      first = false;
      glyphs.push({ x: q.left, y: q.top, w: q.width, h: q.height, ch: ch, font: inf.font, color: inf.color, c: inf.c, bg: rgb(inf.bg), sure: inf.sure, alive: true });
    }
  }
  // colors
  var data = cc.getImageData(0, 0, GW, GH).data;
  for (var j = 0; j < NCELL; j++) col[j] = (data[j * 4] << 16) | (data[j * 4 + 1] << 8) | data[j * 4 + 2];
  for (var g = 0; g < glyphs.length; g++) {
    var gl = glyphs[g];
    var gx0 = Math.max(0, Math.floor(gl.x / CELL)), gx1 = Math.min(GW - 1, Math.floor((gl.x + gl.w) / CELL));
    var gy0 = Math.max(0, Math.floor((gl.y + gl.h * 0.12) / CELL)), gy1 = Math.min(GH - 1, Math.floor((gl.y + gl.h * 0.88) / CELL));
    for (var cy = gy0; cy <= gy1; cy++) for (var cx = gx0; cx <= gx1; cx++) {
      var ci = cy * GW + cx; mat[ci] = 3; glyphAt[ci] = g;
      var o = col[ci], a = 0.65;
      col[ci] = ((((o >> 16) & 255) * (1 - a) + gl.c[0] * a) << 16) | ((((o >> 8) & 255) * (1 - a) + gl.c[1] * a) << 8) | (((o & 255) * (1 - a) + gl.c[2] * a) | 0);
    }
  }
  scanStats = { elements: nEl, glyphs: glyphs.length, images: nImg, imagesReadable: nImgRead, ms: Math.round(performance.now() - t0) };
}

// ------------------------------------------------------------------ DOM snapshots (real elements lifted into the overlay)
var STYLE_SKIP = /^(transition|animation|will-change|cursor|pointer-events|user-select|-webkit-user-select|view-transition|content-visibility|contain)/;
function copyStyle(s, d, pseudo) {
  var cs = getComputedStyle(s, pseudo || null);
  for (var i = 0; i < cs.length; i++) { var p = cs[i]; if (STYLE_SKIP.test(p)) continue; d.style.setProperty(p, cs.getPropertyValue(p)); }
  return cs;
}
function pseudoSpan(s, which) {
  var pc = getComputedStyle(s, which), content = pc.content;
  if (!content || content === 'none' || content === 'normal') return null;
  var sp = D.createElement('span'); copyStyle(s, sp, which);
  var m = /^"(.*)"$/.exec(content); sp.textContent = m ? m[1].replace(/\\([0-9a-f]{1,6}) ?/gi, function (_, h) { return String.fromCodePoint(parseInt(h, 16)); }) : '';
  return sp;
}
function buildSnap(s, budget) {
  if (s.nodeType === 3) return D.createTextNode(s.nodeValue);
  if (s.nodeType !== 1 || budget.n <= 0) return null;
  var tag = s.tagName.toUpperCase();
  if (SKIP[tag]) return null;
  budget.n--;
  if (s instanceof SVGElement) {
    if (tag !== 'SVG') return s.cloneNode(true);
    var sv = s.cloneNode(true); copyStyle(s, sv);
    var a = s.querySelectorAll('*'), b = sv.querySelectorAll('*');
    for (var i = 0; i < a.length && i < 300; i++) { var c = getComputedStyle(a[i]); b[i].style.fill = c.fill; b[i].style.stroke = c.stroke; b[i].style.color = c.color; b[i].style.opacity = c.opacity; }
    return sv;
  }
  var cs = getComputedStyle(s); if (cs.display === 'none') return null;
  var d;
  if (tag === 'IMG') {   // paint the already-loaded image into a canvas (no new request), honoring object-fit
    var ir = s.getBoundingClientRect(), iw = Math.max(1, Math.round(ir.width)), ih = Math.max(1, Math.round(ir.height));
    d = D.createElement('canvas'); d.width = iw * 2; d.height = ih * 2;
    try {
      var nw = s.naturalWidth, nh = s.naturalHeight, fit = cs.objectFit, g = d.getContext('2d'), dx = 0, dy = 0, dw = d.width, dh = d.height;
      if (nw && nh && (fit === 'cover' || fit === 'contain' || fit === 'scale-down')) {
        var sc = fit === 'cover' ? Math.max(dw / nw, dh / nh) : Math.min(dw / nw, dh / nh); dw = nw * sc; dh = nh * sc; dx = (d.width - dw) / 2; dy = (d.height - dh) / 2;
      }
      if (nw && nh) g.drawImage(s, dx, dy, dw, dh);
    } catch (e) {}
  }
  else if (tag === 'CANVAS' || tag === 'VIDEO') {
    d = D.createElement('canvas'); d.width = Math.max(1, s.clientWidth); d.height = Math.max(1, s.clientHeight);
    try { d.getContext('2d').drawImage(s, 0, 0, d.width, d.height); } catch (e) {}
  }
  else d = D.createElement(/^inline/.test(cs.display) ? 'span' : 'div');   // plain tags only: no custom elements get upgraded, no scripts run
  copyStyle(s, d);
  if (tag === 'INPUT' || tag === 'TEXTAREA' || tag === 'SELECT') {
    var v = tag === 'SELECT' ? (s.options[s.selectedIndex] || {}).text : s.value;
    if (s.type === 'checkbox' || s.type === 'radio') v = s.checked ? '✓' : '';
    if (!v && s.placeholder) { v = s.placeholder; d.style.opacity = '0.75'; }
    d.textContent = v || ''; d.style.whiteSpace = 'nowrap'; d.style.overflow = 'hidden';
    if (tag !== 'TEXTAREA') { d.style.display = 'flex'; d.style.alignItems = 'center'; }
    return d;
  }
  if (tag === 'IMG' || tag === 'CANVAS' || tag === 'VIDEO' || tag === 'IFRAME') return d;
  var before = pseudoSpan(s, '::before'); if (before) d.appendChild(before);
  for (var ch = s.firstChild; ch; ch = ch.nextSibling) { var cd = buildSnap(ch, budget); if (cd) d.appendChild(cd); }
  var after = pseudoSpan(s, '::after'); if (after) d.appendChild(after);
  return d;
}
function snapshot(el, r, maxNodes) {
  var snap = buildSnap(el, { n: maxNodes }) || mk('div');
  if (snap.nodeType !== 1) { var w = mk('span'); w.appendChild(snap); snap = w; }
  var disp = snap.style.display;
  css(snap, { position: 'absolute', left: '0px', top: '0px', right: 'auto', bottom: 'auto', margin: '0px', transform: 'none', translate: 'none', rotate: 'none', scale: 'none',
    opacity: '1', visibility: 'visible', width: r.width + 'px', height: r.height + 'px', boxSizing: 'border-box', maxWidth: 'none', maxHeight: 'none', minWidth: '0px', minHeight: '0px',
    float: 'none', flex: 'none', zIndex: 'auto', pointerEvents: 'none', display: (/^inline/.test(disp) || disp === 'contents' || !disp) ? (disp === 'inline-flex' ? 'inline-flex' : 'inline-block') : disp });
  return snap;
}
function makeActor(el, r, maxNodes) {
  var wrap = mk('div', { position: 'absolute', left: '0px', top: '0px', width: r.width + 'px', height: r.height + 'px', transformOrigin: '50% 50%', pointerEvents: 'none', willChange: 'transform' }, actorsEl);
  var snap = snapshot(el, r, maxNodes); wrap.appendChild(snap);
  wrap.style.transform = 'translate(' + r.left + 'px,' + r.top + 'px)';
  return { wrap: wrap, snap: snap };
}
function elementColors(el) {
  var out = [], seen = {};
  function add(c) { if (!c || c[3] < 0.3) return; var k = c[0] + ',' + c[1] + ',' + c[2]; if (seen[k]) return; seen[k] = 1; out.push(rgb(c)); }
  var list = [el].concat(Array.prototype.slice.call(el.querySelectorAll('*'), 0, 40));
  for (var i = 0; i < list.length; i++) { var cs = getComputedStyle(list[i]); add(parseColor(cs.backgroundColor)); add(parseColor(cs.color)); add(parseColor(cs.borderTopColor)); if (list[i].tagName === 'svg' || list[i] instanceof SVGElement) add(parseColor(cs.fill)); }
  var im = el.tagName === 'IMG' ? el : el.querySelector && el.querySelector('img');
  if (im && im.complete && im.naturalWidth) { try { var c = mk('canvas'); c.width = 4; c.height = 4; var x = c.getContext('2d'); x.drawImage(im, 0, 0, 4, 4); var d = x.getImageData(0, 0, 4, 4).data; for (var j = 0; j < 16; j += 5) add([d[j * 4], d[j * 4 + 1], d[j * 4 + 2], 1]); } catch (e) {} }
  if (!out.length) out.push('#888');
  return out;
}

// ------------------------------------------------------------------ choose real elements: enemies (small interactive things) and rocks (big boxes/images)
function classify(el, r) {
  var tag = el.tagName.toUpperCase(), role = el.getAttribute('role');
  if (tag === 'INPUT') { var ty = (el.type || '').toLowerCase(); return (ty === 'submit' || ty === 'button' || ty === 'reset' || ty === 'image') ? 'button' : 'input'; }
  if (tag === 'SELECT' || tag === 'TEXTAREA') return 'input';
  if (tag === 'BUTTON' || role === 'button' || role === 'tab' || role === 'menuitem' || role === 'switch' || role === 'checkbox' || tag === 'SUMMARY') return 'button';
  if (tag === 'IMG' || tag === 'SVG') return (r.width <= 64 && r.height <= 64) ? 'icon' : 'image';
  if (tag === 'A') {
    var cs = getComputedStyle(el), bg = parseColor(cs.backgroundColor), bw = parseFloat(cs.borderTopWidth) || 0;
    var behind = el.parentElement ? bgBehind(el.parentElement) : pageBg;
    if ((bg && bg[3] > 0.3 && colorDist(bg, behind) > 30) || (bw >= 1 && parseFloat(cs.borderTopLeftRadius) > 2)) return 'button';
    if (el.querySelector('img,svg') && !(el.textContent || '').trim()) return 'icon';
    return 'link';
  }
  return 'text';
}
var PRI = { button: 0, input: 1, icon: 2, image: 2.5, link: 3, text: 4 };
function pickEnemies(n) {
  var sel = 'button,a[href],input:not([type=hidden]),select,textarea,[role=button],[role=tab],[role=menuitem],[role=switch],[role=checkbox],summary,img,svg';
  var extra = 'h1,h2,h3,h4,label,code,kbd,strong,b,em,time,small,li>span';
  var cands = [], used = new Set();
  function consider(list, forcedKind) {
    for (var i = 0; i < list.length; i++) {
      var el = list[i]; if (used.has(el) || el.ownerSVGElement || (host && host.contains(el))) continue; used.add(el);
      var r = el.getBoundingClientRect();
      if (r.width < 10 || r.height < 10 || r.width > 360 || r.height > 120 || r.width * r.height < 160) continue;
      if (r.left < 2 || r.top < 2 || r.right > vw - 2 || r.bottom > vh - 2) continue;
      if (el.getClientRects().length > 1 || !visible(el)) continue;
      var par = el.parentElement && el.parentElement.closest('button,a[href],[role=button],summary');
      if (par) { var pr = par.getBoundingClientRect(); if (pr.width >= 10 && pr.width <= 360 && pr.height <= 120) continue; }
      var kind = forcedKind || classify(el, r);
      cands.push({ el: el, r: r, kind: kind, pri: PRI[kind] + rnd() * 0.9 });
    }
  }
  consider(D.querySelectorAll(sel));
  if (cands.length < n + 2) consider(D.querySelectorAll(extra), 'text');
  cands.sort(function (a, b) { return a.pri - b.pri; });
  var chosen = [], skipped = [];
  for (var i = 0; i < cands.length && chosen.length < n; i++) {
    var c = cands[i], cx = c.r.left + c.r.width / 2, cy = c.r.top + c.r.height / 2;
    var tx = clamp(tileOfX(cx), 1, COLS - 2), ty = clamp(tileOfY(cy), 1, ROWS - 1);
    if (Math.abs(tx - START.x) <= 2 && ty <= START.y + 1) continue;
    var ok = true;
    for (var j = 0; j < chosen.length; j++) {
      var o = chosen[j];
      if (Math.abs(o.tx - tx) < 4 && Math.abs(o.ty - ty) < 2) { ok = false; break; }
      if (o.el.contains(c.el) || c.el.contains(o.el)) { ok = false; break; }
    }
    if (!ok) continue;
    c.tx = tx; c.ty = ty;
    var band = Math.floor(ty / (ROWS / 4)), inBand = 0;
    for (var b2 = 0; b2 < chosen.length; b2++) if (Math.floor(chosen[b2].ty / (ROWS / 4)) === band) inBand++;
    if (inBand >= Math.ceil(n / 3) && i < cands.length - 1) { skipped.push(c); continue; }
    c.tx = tx; c.ty = ty; chosen.push(c);
  }
  for (var s2 = 0; s2 < skipped.length && chosen.length < n; s2++) {   // fill up if the page is lopsided
    var sc = skipped[s2], ok2 = true;
    for (var j2 = 0; j2 < chosen.length; j2++) { var o2 = chosen[j2]; if ((Math.abs(o2.tx - sc.tx) < 4 && Math.abs(o2.ty - sc.ty) < 2) || o2.el.contains(sc.el) || sc.el.contains(o2.el)) { ok2 = false; break; } }
    if (ok2) chosen.push(sc);
  }
  return chosen;
}
function pickRocks(n, avoid, relaxed) {
  var minW = relaxed ? 60 : 80, minH = relaxed ? 34 : 50, minA = relaxed ? 3500 : 7000, maxW = relaxed ? 0.6 : 0.5;
  var sel = 'img,picture,video,canvas,svg,figure,pre,blockquote,table,article,aside,section,li,div,header,form,fieldset';
  var list = D.querySelectorAll(sel), cands = [];
  for (var i = 0; i < list.length && i < 6000; i++) {
    var el = list[i]; if (el.ownerSVGElement || (host && host.contains(el))) continue;
    var r = el.getBoundingClientRect();
    if (r.width < minW || r.height < minH || r.width > vw * maxW || r.height > vh * 0.45 || r.width * r.height < minA) continue;
    if (r.left < 4 || r.top < T * 0.8 || r.right > vw - 4 || r.bottom > vh - T * 1.3) continue;
    if (!visible(el)) continue;
    var tag = el.tagName.toUpperCase(), media = /^(IMG|PICTURE|VIDEO|CANVAS|SVG)$/.test(tag);
    if (tag === 'IMG' && !(el.complete && el.naturalWidth)) continue;
    if (!media) {
      var cs = getComputedStyle(el), bg = parseColor(cs.backgroundColor), behind = el.parentElement ? bgBehind(el.parentElement) : pageBg;
      var distinct = (bg && bg[3] > 0.5 && colorDist(bg, behind) > 18) || ((parseFloat(cs.borderTopWidth) || 0) >= 1 && (parseFloat(cs.borderBottomWidth) || 0) >= 1) || cs.boxShadow !== 'none' || (cs.backgroundImage !== 'none');
      if (!distinct) continue;
      if (el.getElementsByTagName('*').length > 160) continue;
    }
    cands.push({ el: el, r: r, media: media, pri: (media ? 0 : 1) + rnd() });
  }
  cands.sort(function (a, b) { return a.pri - b.pri; });
  var chosen = [];
  function hit(a, b, pad) { return a.left < b.right + pad && a.right > b.left - pad && a.top < b.bottom + pad && a.bottom > b.top - pad; }
  for (var k = 0; k < cands.length && chosen.length < n; k++) {
    var c = cands[k], ok = true;
    for (var j = 0; ok && j < avoid.length; j++) if (hit(c.r, avoid[j], T * 0.5)) ok = false;
    for (var q = 0; ok && q < chosen.length; q++) if (hit(c.r, chosen[q].r, T) || chosen[q].el.contains(c.el) || c.el.contains(chosen[q].el)) ok = false;
    if (ok) chosen.push(c);
  }
  if (!chosen.length && !relaxed) return pickRocks(n, avoid, true);   // nothing big enough: accept smaller boxes/buttons as rocks
  return chosen;
}

// ------------------------------------------------------------------ carving / scorch (Hugo's carve() + scorch())
function paintCell(i) {
  var o = i * 4, m = mat[i];
  if (m === 0) {
    var x = i % GW, y = (i / GW) | 0;
    var rim = (x > 0 && mat[i - 1] !== 0) || (x < GW - 1 && mat[i + 1] !== 0) || (y > 0 && mat[i - GW] !== 0) || (y < GH - 1 && mat[i + GW] !== 0);
    var c = rim ? RIM : hash2(x, y) < 0.5 ? VOID_A : VOID_B; hd[o] = c[0]; hd[o + 1] = c[1]; hd[o + 2] = c[2]; hd[o + 3] = 255;
  }
  else if (burn[i]) { hd[o] = SCORCH[0]; hd[o + 1] = SCORCH[1]; hd[o + 2] = SCORCH[2]; hd[o + 3] = (SC_ALPHA[burn[i]] * 255) | 0; }
  else hd[o + 3] = 0;
  holesDirty = true;
}
function removeCell(i) {
  var m = mat[i]; if (m === 0 || m === 9) return false;
  mat[i] = 0; removed++; paintCell(i);
  var x = i % GW;
  if (x > 0 && mat[i - 1] === 0) paintCell(i - 1); if (x < GW - 1 && mat[i + 1] === 0) paintCell(i + 1);
  if (i >= GW && mat[i - GW] === 0) paintCell(i - GW); if (i + GW < NCELL && mat[i + GW] === 0) paintCell(i + GW);
  var g = glyphAt[i]; if (g >= 0 && glyphs[g].alive) detachGlyph(g, false);
  return true;
}
function colOf(i) { var c = col[i]; return 'rgb(' + ((c >> 16) & 255) + ',' + ((c >> 8) & 255) + ',' + (c & 255) + ')'; }
// radius modulated by 3/7/13 sine harmonics with random phases -> chewed, organic edges
function carve(px, py, r, wobble, fx) {
  var p1 = rr(0, 6.28), p2 = rr(0, 6.28), p3 = rr(0, 6.28), a = rr(0.4, 1), b = rr(0.4, 1);
  var R = r * (1 + wobble * 1.6) + CELL;
  var cx0 = clamp(Math.floor((px - R) / CELL), 0, GW - 1), cx1 = clamp(Math.floor((px + R) / CELL), 0, GW - 1);
  var cy0 = clamp(Math.floor((py - R) / CELL), 0, GH - 1), cy1 = clamp(Math.floor((py + R) / CELL), 0, GH - 1);
  var n = 0;
  for (var cy = cy0; cy <= cy1; cy++) for (var cx = cx0; cx <= cx1; cx++) {
    var i = cy * GW + cx; if (mat[i] === 0 || mat[i] === 9) continue;
    var dx = (cx + 0.5) * CELL - px, dy = (cy + 0.5) * CELL - py, d = Math.hypot(dx, dy); if (d > R) continue;
    var th = Math.atan2(dy, dx);
    var lim = r * (1 + wobble * (a * Math.sin(3 * th + p1) * 0.6 + b * Math.sin(7 * th + p2) * 0.4 + Math.sin(13 * th + p3) * 0.25));
    if (d <= lim) {
      var cstr = fx ? colOf(i) : null;
      if (removeCell(i)) { n++; if (fx) fx(cx, cy, cstr); }
    }
  }
  return n;
}
// stepped, hash-dithered burn bands at 0.22/0.45/0.70/0.90
function scorch(px, py, r0, r1, strength) {
  var TH = [0.22, 0.45, 0.7, 0.9];
  var cx0 = clamp(Math.floor((px - r1) / CELL), 0, GW - 1), cx1 = clamp(Math.floor((px + r1) / CELL), 0, GW - 1);
  var cy0 = clamp(Math.floor((py - r1) / CELL), 0, GH - 1), cy1 = clamp(Math.floor((py + r1) / CELL), 0, GH - 1);
  for (var cy = cy0; cy <= cy1; cy++) for (var cx = cx0; cx <= cx1; cx++) {
    var i = cy * GW + cx; if (mat[i] === 0) continue;
    var d = Math.hypot((cx + 0.5) * CELL - px, (cy + 0.5) * CELL - py);
    if (d < r0 - CELL * 2 || d > r1) continue;
    var g = Math.max(0, (d - r0) / (r1 - r0));
    var s = strength * Math.pow(1 - g, 1.4) + (hash2(cx * 3, cy * 5) - 0.5) * 0.3;
    var band = 0; for (var k = 3; k >= 0; k--) if (s >= TH[k]) { band = k + 1; break; }
    if (band > burn[i]) { burn[i] = band; paintCell(i); }
  }
}
function rimEmbers(px, py, r, n) {
  for (var k = 0; k < n; k++) {
    var a = rr(0, 6.28), d = r * rr(0.95, 1.25), i = cellAt(px + Math.cos(a) * d, py + Math.sin(a) * d);
    if (i >= 0 && mat[i] !== 0 && embersL.length < 1600) embersL.push({ i: i, t: rr(0.5, 1.4), m: 0 });
  }
}

// ------------------------------------------------------------------ particles
function spawn(o) { if (parts.length > 2600) parts.shift(); parts.push(o); return o; }
function debris(x, y, color, n, spd, life) {
  for (var k = 0; k < n; k++) { var a = rr(0, 6.28), s = rr(0.3, 1) * spd;
    spawn({ t: 'sq', x: x, y: y, vx: Math.cos(a) * s, vy: Math.sin(a) * s - spd * 0.4, g: 900, life: rr(0.5, 1) * life, max: life, size: rr(2.5, 5), c: color }); }
}
function emberBurst(x, y, n, spd, cols) {
  cols = cols || ['#ffd23a', '#ff8a2b', '#ff4a1a'];
  for (var k = 0; k < n; k++) { var a = rr(0, 6.28), s = rr(0.2, 1) * spd;
    spawn({ t: 'em', x: x, y: y, vx: Math.cos(a) * s, vy: Math.sin(a) * s - 40, g: 120, life: rr(0.3, 0.9), max: 0.9, size: rr(1.5, 3.5), c: cols[k % cols.length] }); }
}
function smoke(x, y, n) { for (var k = 0; k < n; k++) spawn({ t: 'sm', x: x + rr(-10, 10), y: y + rr(-6, 6), vx: rr(-20, 20), vy: rr(-50, -20), g: -10, life: rr(0.7, 1.3), max: 1.3, size: rr(6, 14), c: '#4a3c52' }); }
function confetti(x, y, colors, n, spd) {
  for (var k = 0; k < n; k++) { var a = rr(0, 6.28), s = rr(0.3, 1) * spd;
    spawn({ t: 'cf', x: x, y: y, vx: Math.cos(a) * s, vy: Math.sin(a) * s - spd * 0.5, g: 520, drag: 1.4, life: rr(0.9, 1.8), max: 1.8, w: rr(4, 9), h: rr(3, 6), rot: rr(0, 6), vr: rr(-12, 12), flip: rr(6, 14), c: colors[k % colors.length] }); }
}
function flyLetters(text, font, color, x, y, spd) {
  var chars = (text || '').replace(/\s+/g, '').slice(0, 18);
  for (var k = 0; k < chars.length; k++) {
    var a = rr(3.4, 6.0);
    letters.push({ ch: chars[k], font: font, color: color, x: x + rr(-8, 8), y: y + rr(-6, 6), vx: Math.cos(a) * spd * rr(0.4, 1), vy: Math.sin(a) * spd * rr(0.5, 1), rot: 0, vr: rr(-10, 10), st: 'fall', t: 0, h: 12 });
  }
}
function detachGlyph(gi, burning) {
  var g = glyphs[gi]; g.alive = false;
  if (g.sure) { pctx.fillStyle = g.bg; pctx.fillRect(g.x - 0.5, g.y, g.w + 1, g.h); patchDirty = true; }   // cover the real glyph with its background
  else {   // background is an image/gradient we can't reproduce: the letter leaves a letter-sized hole instead
    var cx0 = Math.max(0, Math.floor(g.x / CELL)), cx1 = Math.min(GW - 1, Math.floor((g.x + g.w) / CELL)), cy0 = Math.max(0, Math.floor(g.y / CELL)), cy1 = Math.min(GH - 1, Math.floor((g.y + g.h) / CELL));
    for (var cy = cy0; cy <= cy1; cy++) for (var cx = cx0; cx <= cx1; cx++) { var ci = cy * GW + cx; if (mat[ci] !== 0 && mat[ci] !== 9) { glyphAt[ci] = -1; removeCell(ci); } }
  }
  if (letters.length < 700) letters.push({ ch: g.ch, font: g.font, color: g.color, x: g.x + g.w / 2, y: g.y + g.h / 2, vx: rr(-60, 60), vy: rr(-160, -40), rot: 0, vr: rr(-6, 6), st: burning ? 'burn' : 'fall', t: 0, h: g.h });
}

// ------------------------------------------------------------------ audio (tiny synth, original sounds)
var actx = null, muted = false;
function beep(f, dur, type, vol, slide) {
  if (muted) return;
  try {
    if (!actx) actx = new (W.AudioContext || W.webkitAudioContext)();
    if (actx.state === 'suspended') actx.resume();
    var o = actx.createOscillator(), g = actx.createGain(), t = actx.currentTime;
    o.type = type || 'square'; o.frequency.setValueAtTime(f, t); if (slide) o.frequency.exponentialRampToValueAtTime(Math.max(30, f + slide), t + dur);
    g.gain.setValueAtTime(vol || 0.05, t); g.gain.exponentialRampToValueAtTime(0.0001, t + dur);
    o.connect(g); g.connect(actx.destination); o.start(t); o.stop(t + dur);
  } catch (e) {}
}
var sfx = {
  pump: function (s) { beep(220 + s * 90, 0.09, 'square', 0.05, 120); },
  pop: function () { beep(160, 0.25, 'sawtooth', 0.08, -120); beep(900, 0.08, 'square', 0.04, -500); },
  rock: function () { beep(90, 0.35, 'triangle', 0.12, -50); },
  dig: function () { beep(70 + Math.random() * 30, 0.03, 'triangle', 0.02); },
  die: function () { beep(600, 0.9, 'square', 0.06, -520); },
  bonus: function () { beep(660, 0.1); setTimeout(function () { beep(990, 0.14); }, 90); },
  fire: function () { beep(120, 0.4, 'sawtooth', 0.04, 60); },
  hose: function () { beep(500, 0.05, 'square', 0.03, 300); },
  wobble: function () { beep(140, 0.15, 'triangle', 0.05, 30); }
};

// ------------------------------------------------------------------ overlay
function buildOverlay() {
  host = D.createElement('div');
  css(host, { position: 'fixed', left: '0px', top: '0px', width: vw + 'px', height: vh + 'px', zIndex: '2147483647', pointerEvents: 'auto', margin: '0px', padding: '0px', border: '0px', background: 'transparent', display: 'block', overflow: 'hidden' });
  host.setAttribute('data-digpage', '1');
  host.addEventListener('digpage-quit', function () { quit(); });
  root = host.attachShadow ? host.attachShadow({ mode: 'open' }) : host;
  layer = mk('div', { position: 'absolute', left: '0px', top: '0px', width: vw + 'px', height: vh + 'px' }, root);
  cv = mk('canvas', { position: 'absolute', left: '0px', top: '0px', width: vw + 'px', height: vh + 'px' }, layer);
  cv.width = Math.round(vw * dpr); cv.height = Math.round(vh * dpr); ctx = cv.getContext('2d');
  actorsEl = mk('div', { position: 'absolute', left: '0px', top: '0px', width: vw + 'px', height: vh + 'px', overflow: 'visible' }, layer);
  cv2 = mk('canvas', { position: 'absolute', left: '0px', top: '0px', width: vw + 'px', height: vh + 'px' }, layer);   // above the element-creatures: digger, hose, confetti
  cv2.width = cv.width; cv2.height = cv.height; ctx2 = cv2.getContext('2d');
  holeC = mk('canvas'); holeC.width = GW; holeC.height = GH; hctx = holeC.getContext('2d'); hImg = hctx.createImageData(GW, GH); hd = hImg.data;
  patchC = mk('canvas'); patchC.width = Math.round(vw * dpr); patchC.height = Math.round(vh * dpr); pctx = patchC.getContext('2d'); pctx.scale(dpr, dpr);
  var font = 'ui-monospace, "SF Mono", Menlo, Consolas, monospace';
  var bar = mk('div', { position: 'absolute', left: '50%', top: '8px', transform: 'translateX(-50%)', display: 'flex', gap: '16px', alignItems: 'center', padding: '6px 14px', borderRadius: '10px', background: 'rgba(18,10,34,0.86)', boxShadow: '0 4px 18px rgba(0,0,0,.35)', color: '#f4ecd8', font: '700 13px ' + font, letterSpacing: '1px', whiteSpace: 'nowrap', pointerEvents: 'none' }, root);
  function stat(label, color) { var s = mk('span', null, bar); var l = mk('span', { color: '#9a8fb0', marginRight: '6px' }, s); l.textContent = label; var v = mk('b', { color: color || '#ffd23a' }, s); return v; }
  var title = mk('span', { color: '#ff8a2b' }, bar); title.textContent = 'DIG//PAGE';
  hud.score = stat('SCORE'); hud.hi = stat('HI'); hud.round = stat('SCREEN'); hud.lives = stat('LIVES', '#7fd3ff'); hud.pct = stat('DESTROYED', '#ff8a2b'); hud.auto = mk('span', { color: '#7dff6a' }, bar);
  var hint = mk('div', { position: 'absolute', right: '10px', bottom: '8px', padding: '4px 10px', borderRadius: '8px', background: 'rgba(18,10,34,0.78)', color: '#cfc4e6', font: '600 11px ' + font, pointerEvents: 'none' }, root);
  hint.textContent = 'ARROWS/WASD dig · SPACE pump · ESC restore page · M mute · T autopilot';
  msgEl = mk('div', { position: 'absolute', left: '50%', top: '40%', transform: 'translate(-50%,-50%)', textAlign: 'center', color: '#ffd23a', font: '800 34px ' + font, letterSpacing: '4px', textShadow: '0 4px 0 #6b2a00, 0 0 24px rgba(255,138,43,.7)', whiteSpace: 'pre', pointerEvents: 'none' }, root);
  hud.sub = mk('div', { position: 'absolute', left: '50%', top: 'calc(40% + 40px)', transform: 'translateX(-50%)', color: '#f4ecd8', font: '700 14px ' + font, letterSpacing: '2px', textShadow: '0 2px 0 #000', whiteSpace: 'pre', pointerEvents: 'none', textAlign: 'center' }, root);
  D.documentElement.appendChild(host);
  // cookie walls / modal <dialog>s live in the browser's top layer, above any z-index: join the top layer too (popover API)
  try { if (host.showPopover) { css(host, { right: 'auto', bottom: 'auto', maxWidth: 'none', maxHeight: 'none', inset: '0px auto auto 0px', color: 'inherit' }); host.setAttribute('popover', 'manual'); host.showPopover(); } } catch (e) {}
}
var hudCache = {};
function setText(k, el, v) { if (hudCache[k] !== v) { hudCache[k] = v; el.textContent = v; } }

// ------------------------------------------------------------------ actors
function addFace(a, type, fit, r) {
  var k = 1 / fit;
  var back = mk('div', { position: 'absolute', left: (-5 * k) + 'px', top: (-4 * k) + 'px', right: (-5 * k) + 'px', bottom: (-4 * k) + 'px', borderRadius: (18 * k) + 'px',
    background: a.bg, border: (2.5 * k) + 'px solid ' + (type === 'pooka' ? '#e8342c' : '#3fbf4a'), boxShadow: '0 ' + (2 * k) + 'px ' + (5 * k) + 'px rgba(0,0,0,.35)' });
  a.wrap.insertBefore(back, a.wrap.firstChild);
  var face = mk('div', { position: 'absolute', left: (r.width / 2) + 'px', top: '0px', width: '0px', height: '0px' }, a.wrap);
  var eyes = [], pupils = [];
  for (var s = -1; s <= 1; s += 2) {
    var e = mk('div', { position: 'absolute', width: 14 * k + 'px', height: 14 * k + 'px', borderRadius: '50%', background: '#fff', boxSizing: 'border-box',
      border: (2.6 * k) + 'px solid ' + (type === 'pooka' ? '#ffd23a' : '#1c3a1c'), left: (s * 8 * k - 7 * k) + 'px', top: (-10 * k) + 'px', boxShadow: '0 ' + k + 'px ' + 2 * k + 'px rgba(0,0,0,.45)' }, face);
    var p = mk('div', { position: 'absolute', width: 5 * k + 'px', height: 5 * k + 'px', borderRadius: '50%', background: type === 'pooka' ? '#111' : '#b3121b', left: 3.5 * k + 'px', top: 3 * k + 'px' }, e);
    eyes.push(e); pupils.push(p);
  }
  var feet = [];
  for (var f = 0; f < 2; f++) feet.push(mk('div', { position: 'absolute', width: 11 * k + 'px', height: 6 * k + 'px', borderRadius: '50%', background: type === 'pooka' ? '#f4f1ea' : '#2a8a33', bottom: (-4 * k) + 'px', left: (r.width * (f ? 0.68 : 0.32) - 5.5 * k) + 'px', boxShadow: '0 1px 2px rgba(0,0,0,.4)' }, a.wrap));
  var wings = [];
  if (type === 'fygar') for (var w = 0; w < 2; w++) wings.push(mk('div', { position: 'absolute', width: 18 * k + 'px', height: 16 * k + 'px', left: (r.width * 0.15 + w * 8 * k) + 'px', top: (-13 * k) + 'px', background: 'rgba(200,255,210,0.9)', clipPath: 'polygon(0% 100%, 30% 0%, 60% 55%, 100% 20%, 80% 100%)', transformOrigin: '50% 100%' }, a.wrap));
  return { face: face, eyes: eyes, pupils: pupils, feet: feet, wings: wings, k: k, back: back };
}
function spawnEnemy(c, idx) {
  var type = (c.kind === 'icon' || c.kind === 'image' || idx % 3 === 2) ? 'fygar' : 'pooka';
  var a = makeActor(c.el, c.r, 70); a.bg = rgb(bgBehind(c.el));
  var fit = Math.min(1, (T * 3.2) / c.r.width, (T * 1.5) / c.r.height);
  var e = { el: c.el, r: c.r, kind: c.kind, type: type, x: c.tx, y: c.ty, sx: c.tx, sy: c.ty, dir: { x: 1, y: 0 }, target: null, state: 'walk',
    ghostIn: rr(8, 15), ghostT: 0, inflate: 0, deflateT: 0, alive: true, pumped: false, fireCd: rr(2, 4), fireWarm: 0, fireT: 0, speed: 2.0 + 0.15 * G.round,
    squash: new Spring(220, 10, 1), face: 1, fit: fit, wrap: a.wrap, snap: a.snap, colors: elementColors(c.el), popT: 0, intro: 0,
    font: getComputedStyle(c.el).font || '600 13px sans-serif', tcolor: getComputedStyle(c.el).color, text: (c.el.innerText || c.el.value || c.el.getAttribute('aria-label') || c.el.alt || '').trim() };
  e.faceParts = addFace(a, type, fit, c.r);
  hideEl(c.el);
  enemies.push(e);
}
function makeRock(c, id) {
  var rk = { id: id, el: c.el, r: c.r, media: c.media, state: 'idle', t: 0, dy: 0, vy: 0, crushed: [], wrap: null, fell: 0, fallDist: rr(4, 7) * T, colors: null };
  var r = c.r;
  var cx0 = Math.max(0, Math.floor(r.left / CELL)), cx1 = Math.min(GW - 1, Math.floor((r.right - 1) / CELL));
  var cy0 = Math.max(0, Math.floor(r.top / CELL)), cy1 = Math.min(GH - 1, Math.floor((r.bottom - 1) / CELL));
  for (var cy = cy0; cy <= cy1; cy++) for (var cx = cx0; cx <= cx1; cx++) { var i = cy * GW + cx; if (mat[i] === 0) continue; mat[i] = 9; rockAt[i] = id; }
  rocks.push(rk);
}

// ------------------------------------------------------------------ game flow
function carveTilePocket(tx, ty) { carve(TXp(tx), TYp(ty), T * 0.54, 0.1); }
function resetPositions() {
  player.x = START.x; player.y = START.y; player.face = { x: 1, y: 0 }; player.dieT = 0; player.hidden = false;
  hoseOff();
  enemies.forEach(function (e) { if (!e.alive) return; e.x = e.sx; e.y = e.sy; e.target = null; e.state = 'walk'; e.inflate = 0; e.fireWarm = 0; e.fireT = 0; e.ghostIn = rr(8, 15); });
  flames = []; setState('ready');
}
function setState(s) { G.state = s; G.stateT = 0; }
function addScore(n, x, y) {
  G.score += n; if (G.score > G.hi) { G.hi = G.score; }
  if (x !== undefined) popups.push({ text: String(n), x: x, y: y, t: 1.1 });
}

// ------------------------------------------------------------------ input
function press(code) {
  if (keys[code]) return; keys[code] = true;
  if (DIRS[code]) { var i = dirStack.indexOf(code); if (i >= 0) dirStack.splice(i, 1); dirStack.push(code); }
  if (code === 'Space') { pumpPressed = true; if (G.state === 'gameover' && G.stateT > 0.8) restartScreen(true); }
  if (code === 'Enter' && G.state === 'gameover' && G.stateT > 0.8) restartScreen(true);
  if (code === 'KeyR') restartScreen(true);
  if (code === 'KeyM') muted = !muted;
  if (code === 'KeyT') autopilot = !autopilot;
  if (code === 'KeyP') paused = !paused;
  if (code === 'Escape') quit();
}
function release(code) { keys[code] = false; var i = dirStack.indexOf(code); if (i >= 0) dirStack.splice(i, 1); }
var OURKEYS = { Space: 1, Escape: 1, Enter: 1, KeyR: 1, KeyM: 1, KeyT: 1, KeyP: 1 };
function onKey(e) {
  if (!running) return;
  var c = e.code; if (!(DIRS[c] || OURKEYS[c])) return;
  e.preventDefault(); e.stopImmediatePropagation();
  if (e.type === 'keydown') { if (!e.repeat) press(c); } else if (e.type === 'keyup') release(c);
}
function stopEvt(e) { if (running) { e.preventDefault(); e.stopPropagation(); } }
function listen(t, ev, fn, opt) { t.addEventListener(ev, fn, opt); listeners.push([t, ev, fn, opt]); }
function inputDir() {
  if (autopilot) return autoDir();
  var c = dirStack[dirStack.length - 1]; return c ? { x: DIRS[c][0], y: DIRS[c][1] } : null;
}

// ------------------------------------------------------------------ autopilot (T): hunts the nearest element and pumps it
var ap = { tgt: null, keep: 0, stuck: 0, lastX: 0, lastY: 0, alt: 0, altDir: null, tap: 0 };
function alignedTarget() {
  var best = null;
  enemies.forEach(function (e) {
    if (!e.alive || e.state === 'ghost') return;
    var dx = e.x - player.x, dy = e.y - player.y, px = TXp(player.x), py = TYp(player.y);
    if (Math.abs(dy) < 0.3 && Math.abs(dx) < HOSE_MAX && lineClear(px, py, TXp(e.x) - Math.sign(dx) * T * 0.4, py)) { if (!best || Math.abs(dx) < best.d) best = { e: e, d: Math.abs(dx), f: { x: Math.sign(dx) || 1, y: 0 } }; }
    if (Math.abs(dx) < 0.3 && Math.abs(dy) < HOSE_MAX && lineClear(px, py, px, TYp(e.y) - Math.sign(dy) * T * 0.4)) { if (!best || Math.abs(dy) < best.d) best = { e: e, d: Math.abs(dy), f: { x: 0, y: Math.sign(dy) || 1 } }; }
  });
  return best;
}
function autoDir() {
  if (hose.target && hose.target.alive) return null;
  var a = alignedTarget();
  if (a) { if (!hose.active) player.face = a.f; return null; }
  var tgt = ap.tgt && ap.tgt.alive && ap.tgt.state !== 'ghost' && ap.keep > 0 ? ap.tgt : null, bd = 1e9;
  if (!tgt) {
    enemies.forEach(function (e) { if (!e.alive || e.state === 'ghost') return; var d = Math.abs(e.x - player.x) + Math.abs(e.y - player.y) * 1.3; if (d < bd) { bd = d; tgt = e; } });
    ap.tgt = tgt; ap.keep = 2.5;
  }
  if (!tgt) return null;
  if (ap.alt > 0) return ap.altDir;
  // flee: something is close but we can't pump it (ghost or not lined up) -> back off along an open lane
  var threat = null;
  enemies.forEach(function (e) { if (!e.alive || e.inflate) return; var d = Math.hypot(e.x - player.x, e.y - player.y); if (d < (e.state === 'ghost' ? 1.5 : 1.1) && (!threat || d < threat.d)) threat = { e: e, d: d }; });
  if (threat) {
    var opts = [[1, 0], [-1, 0], [0, 1], [0, -1]].filter(function (d) { var nx = Math.round(player.x) + d[0], ny = Math.round(player.y) + d[1]; return nx >= 0 && ny >= 0 && nx < COLS && ny < ROWS && !rockPx(TXp(nx), TYp(ny)); });
    opts.sort(function (a, b) { var da = Math.hypot(player.x + a[0] - threat.e.x, player.y + a[1] - threat.e.y) + (tileOpen(Math.round(player.x) + a[0], Math.round(player.y) + a[1]) ? 0.6 : 0);
      var db = Math.hypot(player.x + b[0] - threat.e.x, player.y + b[1] - threat.e.y) + (tileOpen(Math.round(player.x) + b[0], Math.round(player.y) + b[1]) ? 0.6 : 0); return db - da; });
    if (opts.length) return { x: opts[0][0], y: opts[0][1] };
  }
  var st = apPath(Math.round(tgt.x), Math.round(tgt.y));
  if (st) return st;
  var dy = tgt.y - player.y, dx = tgt.x - player.x;
  if (Math.abs(dy) > 0.3) return { x: 0, y: Math.sign(dy) };
  return { x: Math.sign(dx) || 1, y: 0 };
}
// cheapest route to the target tile (open tunnel = 1, dirt = 3, rocks impassable); returns the first step
function apPath(gx, gy) {
  var sx = Math.round(player.x), sy = Math.round(player.y);
  if (sx === gx && sy === gy) return null;
  var N = COLS * ROWS, dist = new Float32Array(N).fill(1e9), from = new Int32Array(N).fill(-1), q = [sy * COLS + sx];
  dist[sy * COLS + sx] = 0;
  var D4 = [[1, 0], [-1, 0], [0, 1], [0, -1]];
  while (q.length) {
    var bi = 0; for (var k = 1; k < q.length; k++) if (dist[q[k]] < dist[q[bi]]) bi = k;
    var cur = q[bi]; q[bi] = q[q.length - 1]; q.pop();
    var cx = cur % COLS, cy = (cur - cx) / COLS;
    if (cx === gx && cy === gy) break;
    for (var d = 0; d < 4; d++) {
      var nx = cx + D4[d][0], ny = cy + D4[d][1]; if (nx < 0 || ny < 0 || nx >= COLS || ny >= ROWS) continue;
      if (rockPx(TXp(nx), TYp(ny)) || rockPx((TXp(nx) + TXp(cx)) / 2, (TYp(ny) + TYp(cy)) / 2)) continue;
      var ni = ny * COLS + nx, nd = dist[cur] + (tileOpen(nx, ny) ? 1 : 3);
      if (nd < dist[ni]) { if (dist[ni] >= 1e9) q.push(ni); dist[ni] = nd; from[ni] = cur; }
    }
  }
  var gi = gy * COLS + gx; if (from[gi] < 0) return null;
  var c = gi, s0 = sy * COLS + sx; while (from[c] !== s0 && from[c] >= 0) c = from[c];
  var fx = c % COLS, fy = (c - fx) / COLS;
  return { x: fx - sx, y: fy - sy };
}
function autoTick(dt) {
  if (!autopilot) return;
  ap.alt -= dt; ap.keep -= dt;
  var moved = Math.abs(player.x - ap.lastX) + Math.abs(player.y - ap.lastY);
  ap.lastX = player.x; ap.lastY = player.y;
  var want = autoDir();
  if (want && moved < 0.002) { ap.stuck += dt; if (ap.stuck > 0.35) { ap.stuck = 0; ap.alt = 0.7; ap.altDir = want.x ? { x: 0, y: player.y > ROWS / 2 ? -1 : 1 } : { x: rnd() < 0.5 ? 1 : -1, y: 0 }; } } else ap.stuck = 0;
  ap.tap -= dt;
  var a = alignedTarget();
  if ((a || (hose.target && hose.target.alive)) && ap.tap <= 0) { pumpPressed = true; ap.tap = 0.24; }
}

// ------------------------------------------------------------------ player
function moveAxisToward(axis, target, step) { var cur = player[axis], d = target - cur; if (Math.abs(d) <= step) { player[axis] = target; return step - Math.abs(d); } player[axis] += Math.sign(d) * step; return 0; }
function updatePlayer(dt) {
  autoTick(dt);
  var d = inputDir(); player.moving = false;
  if (d) {
    if (hose.active) hoseOff();
    var step = PLAYER_SPEED * (player.digging ? 0.75 : 1) * dt;
    var along = d.x !== 0 ? 'x' : 'y', cross = d.x !== 0 ? 'y' : 'x';
    if (Math.abs(player[cross] - Math.round(player[cross])) >= 1e-4) {    // finish the current lane before turning
      var f = player.face[cross]; var tgt = f > 0 ? Math.ceil(player[cross]) : f < 0 ? Math.floor(player[cross]) : Math.round(player[cross]);
      step = moveAxisToward(cross, tgt, step); player.moving = true;
    }
    if (step > 0 && Math.abs(player[cross] - Math.round(player[cross])) < 1e-4) {
      player[cross] = Math.round(player[cross]); player.face = { x: d.x, y: d.y };
      var dv = d[along], cur = player[along], next = clamp(cur + dv * step, 0, along === 'x' ? COLS - 1 : ROWS - 1);
      var probeX = TXp(along === 'x' ? next + dv * 0.5 : player.x), probeY = TYp(along === 'y' ? next + dv * 0.5 : player.y);
      if (rockPx(probeX, probeY)) next = dv > 0 ? Math.max(cur, Math.floor(next)) : Math.min(cur, Math.ceil(next));   // rocks (big elements) can't be dug
      if (next !== cur) player.moving = true;
      player[along] = next;
    }
  }
  var n = 0, px = TXp(player.x), py = TYp(player.y);
  if (player.moving) {
    n = carve(px, py, T * 0.5, 0.12, function (cx, cy, c) { if (rnd() < 0.35) spawn({ t: 'sq', x: (cx + 0.5) * CELL, y: (cy + 0.5) * CELL, vx: rr(-60, 60) - player.face.x * 60, vy: rr(-160, -40), g: 900, life: rr(0.4, 0.9), max: 0.9, size: rr(2.5, 4.5), c: c }); });
    if (n > 0) {
      scorch(px, py, T * 0.46, T * 0.82, 0.85);
      if (rnd() < 0.5) rimEmbers(px + player.face.x * T * 0.3, py + player.face.y * T * 0.3, T * 0.5, 2);
      if (rnd() < 0.3) sfx.dig();
    }
    var tx = Math.round(player.x), ty = Math.round(player.y), ti = ty * COLS + tx;
    if (tileOpen(tx, ty) && !G.tileDug[ti]) { G.tileDug[ti] = 1; if (n > 0) addScore(10); }
  }
  player.digging = n > 0;
  if (pumpPressed) { if (hose.target && hose.target.alive) pumpEnemy(hose.target); else if (!hose.active) fireHose(); }
  if (keys.Space && hose.target) { hose.holdT += dt; if (hose.holdT > 0.3) { hose.holdT = 0; pumpEnemy(hose.target); } } else hose.holdT = 0;
  updateHose(dt);
  if (bonus && Math.hypot(player.x - bonus.x, player.y - bonus.y) < 0.6) {
    addScore(bonus.value, TXp(bonus.x), TYp(bonus.y)); sfx.bonus(); emberBurst(TXp(bonus.x), TYp(bonus.y), 30, 220, ['#7dff6a', '#fff', '#ffd23a']); bonus = null;
  }
}
function fireHose() { hose.active = true; hose.len = 0; hose.dir = { x: player.face.x, y: player.face.y }; hose.target = null; hose.retract = false; sfx.hose(); hose.wob.kick(6); }
function hoseOff() { if (hose.target && hose.target.alive) hose.target.pumped = false; hose.active = false; hose.target = null; }
function updateHose(dt) {
  if (!hose.active) return;
  if (hose.target) { var e = hose.target; if (!e.alive) { hoseOff(); return; } hose.len = Math.max(0.3, Math.hypot(e.x - player.x, e.y - player.y) - 0.3); }
  else if (hose.retract) { hose.len -= HOSE_SPEED * 1.4 * dt; if (hose.len <= 0) hoseOff(); }
  else {
    hose.len += HOSE_SPEED * dt;
    var tx = player.x + hose.dir.x * (hose.len + 0.1), ty = player.y + hose.dir.y * (hose.len + 0.1);
    var blocked = !airPx(TXp(tx), TYp(ty)) || tx < -0.5 || ty < -0.5 || tx > COLS - 0.5 || ty > ROWS - 0.5;
    for (var i = 0; i < enemies.length; i++) { var en = enemies[i]; if (!en.alive || en.state === 'ghost') continue; if (Math.hypot(en.x - tx, en.y - ty) < 0.55) { hose.target = en; en.pumped = true; pumpEnemy(en); break; } }
    if (!hose.target && (blocked || hose.len >= HOSE_MAX)) { hose.retract = true; if (blocked) debris(TXp(tx), TYp(ty), '#e8dcc0', 4, 90, 0.4); }
  }
  hose.wob.step(0, dt);
}
function pumpEnemy(e) {
  e.inflate = Math.min(4, e.inflate + 1); e.deflateT = 0.9; e.squash.kick(3.5); hose.wob.kick(5); sfx.pump(e.inflate); shake(0.05);
  if (e.inflate >= 4) popEnemy(e);
}
function popEnemy(e) {
  e.alive = false; e.popT = 0.14;
  var px = TXp(e.x), py = TYp(e.y);
  var stratum = clamp(Math.floor(py / vh * 4), 0, 3);
  var pts = Math.round((KIND_SCORE[e.kind] || 200) * [1, 1.5, 2, 2.5][stratum] / 10) * 10;
  if (e.type === 'fygar' && Math.abs(player.y - e.y) < 0.3) pts *= 2;
  addScore(pts, px, py - 20);
  confetti(px, py, e.colors, 70, 380);
  flyLetters(e.text, e.font, e.tcolor, px, py, 320);
  carve(px, py, T * 0.8, 0.18); scorch(px, py, T * 0.8, T * 1.6, 1.05); rimEmbers(px, py, T * 0.8, 16);
  emberBurst(px, py, 40, 300); smoke(px, py, 8);
  sfx.pop(); shake(0.3);
  if (hose.target === e) hoseOff();
  G.popped++;
}

// ------------------------------------------------------------------ enemies
var N4 = [[1, 0], [-1, 0], [0, 1], [0, -1]];
function bfsFirstStep(sx, sy, gx, gy) {
  if (sx === gx && sy === gy) return null;
  var prev = new Int32Array(COLS * ROWS); prev.fill(-1); var s0 = sy * COLS + sx, q = [s0], h = 0; prev[s0] = s0;
  while (h < q.length) {
    var c = q[h++], cx = c % COLS, cy = (c / COLS) | 0;
    if (cx === gx && cy === gy) { var n = c; while (prev[n] !== s0) n = prev[n]; return [n % COLS, (n / COLS) | 0]; }
    for (var k = 0; k < 4; k++) { var nx = cx + N4[k][0], ny = cy + N4[k][1]; if (nx < 0 || ny < 0 || nx >= COLS || ny >= ROWS) continue; var ni = ny * COLS + nx; if (prev[ni] >= 0 || !passable(cx, cy, nx, ny)) continue; prev[ni] = c; q.push(ni); }
  }
  return null;
}
function chooseTarget(e) {
  var tx = Math.round(e.x), ty = Math.round(e.y);
  var st = G.state === 'play' ? bfsFirstStep(tx, ty, Math.round(player.x), Math.round(player.y)) : null;
  e.hasPath = !!st;
  if (st && rnd() < 0.85) e.target = { x: st[0], y: st[1] };
  else {
    var opts = N4.filter(function (d) { return passable(tx, ty, tx + d[0], ty + d[1]); });
    var fwd = opts.filter(function (d) { return !(d[0] === -e.dir.x && d[1] === -e.dir.y); });
    var pick = fwd.length ? fwd : opts; if (!pick.length) { e.target = null; return; }
    var d = pick[(rnd() * pick.length) | 0]; e.target = { x: tx + d[0], y: ty + d[1] };
  }
  e.dir = { x: Math.sign(e.target.x - tx), y: Math.sign(e.target.y - ty) }; if (e.dir.x) e.face = e.dir.x;
}
function ghostCount() { var n = 0; enemies.forEach(function (o) { if (o.alive && o.state === 'ghost') n++; }); return n; }
function updateEnemy(e, dt) {
  if (!e.alive) return;
  if (e.inflate > 0) { if (!e.pumped || hose.target !== e) { e.deflateT -= dt; if (e.deflateT <= 0) { e.inflate--; e.deflateT = 0.7; e.squash.kick(-2); } } return; }
  var play = G.state === 'play';
  if (e.type === 'fygar' && e.state === 'walk') {
    if (e.fireT > 0) { e.fireT -= dt; updateFire(e, dt); return; }
    if (e.fireWarm > 0) { e.fireWarm -= dt; if (e.fireWarm <= 0) { e.fireT = 0.8; sfx.fire(); } return; }
    e.fireCd -= dt;
    var ddy = Math.abs(player.y - e.y), ddx = player.x - e.x;
    if (play && e.fireCd <= 0 && ddy < 0.35 && Math.abs(ddx) < 4.2 && Math.sign(ddx) === e.face && rnd() < 0.03) { e.fireWarm = 0.55; e.fireCd = rr(3, 5.5); return; }
  }
  if (e.state === 'walk') {
    if (play) e.ghostIn -= dt * (e.hasPath ? 1 : 1.4);
    if (e.ghostIn <= 0 && play && ghostCount() >= 2) e.ghostIn = rr(1, 3);
    if (e.ghostIn <= 0 && play) { e.state = 'ghost'; e.ghostT = 0; e.target = null; return; }
    var step = e.speed * dt * (play ? 1 : 0.5), guard = 0;
    while (step > 0 && guard++ < 4) {
      if (!e.target) { chooseTarget(e); if (!e.target) break; }
      var dx = e.target.x - e.x, dy = e.target.y - e.y, d = Math.abs(dx) + Math.abs(dy);
      if (d <= step) { e.x = e.target.x; e.y = e.target.y; step -= d; e.target = null; }
      else { e.x += Math.sign(dx) * Math.min(step, Math.abs(dx)); e.y += Math.sign(dy) * Math.min(step, Math.abs(dy)); step = 0; }
    }
  } else if (e.state === 'ghost') {
    e.ghostT += dt;
    var gx = player.x - e.x, gy = player.y - e.y, gd = Math.hypot(gx, gy) || 1, sp = e.speed * 0.62 * dt;
    e.x = clamp(e.x + gx / gd * sp, 0, COLS - 1); e.y = clamp(e.y + gy / gd * sp, 0, ROWS - 1); if (gx) e.face = Math.sign(gx);
    var rx = Math.round(e.x), ry = Math.round(e.y);
    if (e.ghostT > 1.4 && tileOpen(rx, ry) && Math.hypot(e.x - rx, e.y - ry) < 0.18) { e.x = rx; e.y = ry; e.state = 'walk'; e.target = null; e.ghostIn = rr(8, 15); }
  }
}
function updateFire(e, dt) {
  var k = clamp(1 - Math.max(0, e.fireT - 0.55) / 0.25, 0, 1), len = 3 * k;
  var x0 = e.x + e.face * 0.5, x1 = e.x + e.face * (0.5 + len), y = TYp(e.y);
  for (var i = 0; i < 5; i++) { var t = Math.random(), x = TXp(x0 + (x1 - x0) * t);
    spawn({ t: 'em', x: x, y: y + rr(-6, 6), vx: e.face * rr(40, 120), vy: rr(-40, 10), g: -30, life: rr(0.15, 0.35), max: 0.35, size: rr(3, 8) * (1 - t * 0.4), c: ['#ffe45a', '#ff8a2b', '#ff3a1a'][i % 3] }); }
  var fx = TXp(x0 + (x1 - x0) * Math.random());
  scorch(fx, y, 4, T * 0.45, 0.5);
  var ci = cellAt(fx, y + rr(-8, 8)); if (ci >= 0 && glyphAt[ci] >= 0 && glyphs[glyphAt[ci]].alive) detachGlyph(glyphAt[ci], true);   // fire burns letters
  if (G.state === 'play' && Math.abs(player.y - e.y) < 0.4 && (player.x - Math.min(x0, x1)) >= -0.3 && (player.x - Math.max(x0, x1)) <= 0.3) killPlayer('fire');
}

// ------------------------------------------------------------------ rocks: big elements detach, fall and shatter (like the detaching chunks in Hugo's demo)
function rockUndermined(rk) {
  var r = rk.r, ty = tileOfY(r.bottom + T * 0.5);
  if (ty >= ROWS) return false;
  var ppx = TXp(player.x), ppy = TYp(player.y);
  if (G.state === 'play' && ppx > r.left - T * 0.9 && ppx < r.right + T * 0.9 && ppy > r.bottom - T * 0.2 && ppy < r.bottom + T * 1.6) return false;   // like the arcade: it waits until you step out from under it
  for (var tx = Math.max(0, tileOfX(r.left + 4)); tx <= Math.min(COLS - 1, tileOfX(r.right - 4)); tx++) if (tileOpen(tx, ty)) return true;
  return false;
}
function updateRock(rk, dt) {
  var r = rk.r;
  if (rk.state === 'idle') {
    if (rockUndermined(rk)) { rk.state = 'wobble'; rk.t = 0; var a = makeActor(rk.el, r, rk.media ? 1 : 180); rk.wrap = a.wrap; hideEl(rk.el); rk.colors = elementColors(rk.el); sfx.wobble(); }
  } else if (rk.state === 'wobble') {
    rk.t += dt;
    var wt = 0.9 + Math.min(0.8, r.width / T * 0.05), w = Math.sin(rk.t * 38) * (1 - rk.t / (wt + 0.2));
    rk.wrap.style.transform = 'translate(' + (r.left + w * 3) + 'px,' + r.top + 'px) rotate(' + (w * 1.2) + 'deg)';
    if (rk.t > wt) {
      rk.state = 'falling'; rk.vy = 0;
      for (var i = 0; i < NCELL; i++) if (rockAt[i] === rk.id) { rockAt[i] = -1; mat[i] = 1; removeCell(i); }   // leave a hole where the element was
    }
  } else if (rk.state === 'falling') {
    var prevB = r.bottom + rk.dy;
    rk.vy = Math.min(rk.vy + 1700 * dt, 950); rk.dy += rk.vy * dt; rk.fell = rk.dy;
    var top = r.top + rk.dy, bot = r.bottom + rk.dy, impact = false;
    var cy0 = Math.max(0, Math.floor(prevB / CELL) - 1), cy1 = Math.min(GH - 1, Math.floor(bot / CELL));
    for (var cy = cy0; cy <= cy1; cy++) {
      var jag = Math.floor(hash2(rk.id * 31, cy) * 3);
      var cx0 = Math.max(0, Math.floor(r.left / CELL) - jag), cx1 = Math.min(GW - 1, Math.floor((r.right - 1) / CELL) + jag);
      for (var cx = cx0; cx <= cx1; cx++) {
        var ci = cy * GW + cx;
        if (mat[ci] === 9 && rockAt[ci] !== rk.id) { impact = true; continue; }
        var cstr = colOf(ci);
        if (removeCell(ci) && rnd() < 0.12) spawn({ t: 'sq', x: (cx + 0.5) * CELL, y: (cy + 0.5) * CELL, vx: rr(-120, 120), vy: rr(-200, -60), g: 900, life: rr(0.4, 0.8), max: 0.8, size: rr(2.5, 4.5), c: cstr });
      }
    }
    enemies.forEach(function (e) {
      if (!e.alive || e.state === 'ghost') return; var ex = TXp(e.x), ey = TYp(e.y);
      if (ex > r.left && ex < r.right && ey > top && ey < bot + T * 0.25) { e.alive = false; e.crushedBy = rk; rk.crushed.push(e); }
    });
    var px = TXp(player.x), py = TYp(player.y);
    if (G.state === 'play' && px > r.left + 4 && px < r.right - 4 && py > top && py < bot + T * 0.2) killPlayer('rock');
    rk.crushed.forEach(function (e) { e.y = tileOfY(bot) - 0.1; });
    rk.wrap.style.transform = 'translate(' + r.left + 'px,' + top + 'px)';
    if (impact || rk.dy >= rk.fallDist || bot >= vh - 2) shatterRock(rk, top, bot);
  }
}
var domFrags = [];
function updateDomFrags(dt) {
  for (var i = domFrags.length - 1; i >= 0; i--) {
    var f = domFrags[i]; f.life -= dt; f.vy += 1100 * dt; f.x += f.vx * dt; f.y += f.vy * dt; f.rot += f.vr * dt;
    if (f.life <= 0 || f.y > vh + 200) { f.el.remove(); domFrags.splice(i, 1); continue; }
    f.el.style.transform = 'translate(' + f.x + 'px,' + f.y + 'px) rotate(' + f.rot + 'deg)';
    f.el.style.opacity = String(Math.min(1, f.life / 0.4));
  }
}
function shatterRock(rk, top, bot) {
  rk.state = 'dead'; G.rocksDropped++;
  var r = rk.r, w = r.width, h = r.height, cx = r.left + w / 2;
  if (rk.media && rk.el.tagName === 'IMG' && rk.el.naturalWidth) {       // shatter into real image fragments
    var img = rk.el, nx = 6, ny = 4, nw = img.naturalWidth, nh = img.naturalHeight;
    for (var i = 0; i < nx; i++) for (var j = 0; j < ny; j++) {
      var fx = r.left + (i + 0.5) * w / nx, fy = top + (j + 0.5) * h / ny;
      spawn({ t: 'fr', img: img, sx: i * nw / nx, sy: j * nh / ny, sw: nw / nx, sh: nh / ny, w: w / nx, h: h / ny, x: fx, y: fy, vx: (fx - cx) * rr(2, 5) + rr(-40, 40), vy: rr(-380, -120), g: 1100, life: rr(1.1, 1.7), max: 1.7, rot: 0, vr: rr(-8, 8) });
    }
  } else {
    // shatter the element's own DOM snapshot: clipped clones fly apart as real pieces of the box
    if (rk.wrap) {
      var nxp = Math.max(2, Math.min(6, Math.round(w / 70))), nyp = Math.max(1, Math.min(4, Math.round(h / 45)));
      for (var pi = 0; pi < nxp; pi++) for (var pj = 0; pj < nyp; pj++) {
        var x0 = pi * w / nxp, y0 = pj * h / nyp, pw = w / nxp, ph = h / nyp;
        var cl = rk.wrap.cloneNode(true);
        cl.style.clipPath = 'inset(' + y0 + 'px ' + (w - x0 - pw) + 'px ' + (h - y0 - ph) + 'px ' + x0 + 'px)';
        cl.style.transformOrigin = (x0 + pw / 2) + 'px ' + (y0 + ph / 2) + 'px';
        actorsEl.appendChild(cl);
        var fcx = r.left + x0 + pw / 2;
        domFrags.push({ el: cl, x: r.left, y: top, vx: (fcx - cx) * rr(1.5, 4) + rr(-40, 40), vy: rr(-420, -140), rot: 0, vr: rr(-300, 300), life: rr(1.1, 1.6), max: 1.6 });
      }
    }
    var cols = (rk.colors || []).filter(function (c) { var m = /(\d+),\s*(\d+),\s*(\d+)/.exec(c); return !m || (+m[1] + +m[2] + +m[3]) < 720; });
    if (!cols.length) cols = ['#8a7fa8', '#5b4d7a', '#c9b8ff'];
    for (var k = 0; k < 60; k++) spawn({ t: 'sq', x: r.left + rnd() * w, y: top + rnd() * h, vx: rr(-260, 260), vy: rr(-420, -80), g: 1100, life: rr(0.8, 1.5), max: 1.5, size: rr(4, 10), c: cols[k % cols.length] });
    flyLetters((rk.el.innerText || '').slice(0, 40), getComputedStyle(rk.el).font, getComputedStyle(rk.el).color, cx, (top + bot) / 2, 360);
  }
  carve(cx, bot, Math.min(w * 0.5, T * 1.4), 0.2); scorch(cx, bot, Math.min(w * 0.5, T * 1.4), Math.min(w * 0.5, T * 1.4) + T, 1); rimEmbers(cx, bot, T * 1.2, 24);
  smoke(cx, bot - 10, 14); emberBurst(cx, bot, 30, 260); sfx.rock(); shake(0.45);
  if (rk.wrap) rk.wrap.remove();
  if (rk.crushed.length) {
    var table = [0, 1000, 2500, 4000, 6000, 8000, 10000, 12000, 15000];
    addScore(table[Math.min(rk.crushed.length, 8)], cx, bot - 20);
    rk.crushed.forEach(function (e) { e.wrap.remove(); confetti(TXp(e.x), TYp(e.y), e.colors, 30, 250); G.popped++; });
  }
  if (G.rocksDropped >= 2 && !G.bonusSpawned) spawnBonus();
}
function spawnBonus() {
  G.bonusSpawned = true;
  var img = null;
  bonus = { x: START.x, y: START.y, t: 10, value: 1000 + 200 * (G.round - 1), img: img };
  emberBurst(TXp(START.x), TYp(START.y), 24, 200, ['#7dff6a', '#fff']);
}
function killPlayer(why) { if (G.state !== 'play') return; G.deathWhy = why || '?'; setState('dying'); player.dieT = 0; hoseOff(); sfx.die(); shake(0.3); emberBurst(TXp(player.x), TYp(player.y), 30, 260, ['#fff', '#7fd3ff', '#ffd23a']); }
function shake(a) { trauma = Math.min(1, trauma + a); }

function killGlyphsIn(r) {
  for (var i = 0; i < glyphs.length; i++) { var g = glyphs[i]; var x = g.x + g.w / 2, y = g.y + g.h / 2; if (x >= r.left && x <= r.right && y >= r.top && y <= r.bottom) g.alive = false; }
}

// ------------------------------------------------------------------ sync DOM actors
function syncEnemy(e, dt) {
  var fp = e.faceParts, t = G.t;
  if (!e.alive) {
    if (e.popT > 0) { e.popT -= dt; var s = e.fit * (1 + 4 * 0.28) * (1.15 + (0.14 - e.popT) * 3); e.wrap.style.transform = 'translate(' + (TXp(e.x) - e.r.width / 2) + 'px,' + (TYp(e.y) - e.r.height / 2) + 'px) scale(' + s + ')'; e.wrap.style.opacity = String(Math.max(0, e.popT / 0.14)); if (e.popT <= 0) e.wrap.remove(); }
    else if (e.crushedBy && e.crushedBy.state !== 'dead') e.wrap.style.transform = 'translate(' + (TXp(e.x) - e.r.width / 2) + 'px,' + (TYp(e.y) - e.r.height / 2) + 'px) scale(' + (e.fit * 1.4) + ',' + (e.fit * 0.3) + ')';
    return;
  }
  var px = TXp(e.x), py = TYp(e.y), ghost = e.state === 'ghost';
  var sq = e.squash.step(1, dt), inf = e.inflate;
  var k = 1 + inf * 0.28, sx = e.fit * k / Math.sqrt(sq), sy = e.fit * k * sq;
  var bob = ghost ? Math.sin(t * 5) * 3 : (inf ? 0 : -Math.abs(Math.sin(t * 12 + e.sx)) * 3);
  if (G.state === 'ready' && e.intro < 1) {           // peel off the page: from the element's real spot/size to its creature form
    e.intro = Math.min(1, e.intro + dt / 0.9); var q = e.intro * e.intro * (3 - 2 * e.intro);
    px = (e.r.left + e.r.width / 2) * (1 - q) + px * q; py = (e.r.top + e.r.height / 2) * (1 - q) + py * q; sx = 1 * (1 - q) + sx * q; sy = 1 * (1 - q) + sy * q;
  }
  var wob = inf ? Math.sin(t * 30) * inf * 1.2 : 0;
  e.wrap.style.transform = 'translate(' + (px - e.r.width / 2) + 'px,' + (py - e.r.height / 2 + bob) + 'px) rotate(' + wob + 'deg) scale(' + sx + ',' + sy + ')';
  e.snap.style.opacity = ghost ? '0.16' : '1'; fp.back.style.opacity = ghost ? '0.12' : '1';
  e.wrap.style.filter = inf ? ('saturate(' + (1 + inf * 0.45) + ') brightness(' + (1 + inf * 0.07) + ') drop-shadow(0 0 ' + (inf * 3) + 'px rgba(255,110,60,0.85))') : (ghost ? 'none' : 'drop-shadow(0 2px 2px rgba(0,0,0,0.35))');
  if (e.type === 'fygar' && e.fireWarm > 0) e.wrap.style.filter = 'brightness(1.6) drop-shadow(0 0 6px #ff3a1a)';
  fp.face.style.left = (e.r.width * (0.5 + 0.2 * e.face)) + 'px';
  for (var i = 0; i < 2; i++) { fp.pupils[i].style.left = (3.5 + e.face * 2) * fp.k + 'px'; fp.feet[i].style.transform = 'translateX(' + ((inf || ghost) ? 0 : Math.sin(t * 14 + i * 3.1) * 4 * fp.k) + 'px)'; fp.feet[i].style.opacity = ghost ? '0' : '1'; }
  fp.wings.forEach(function (w, j) { w.style.transform = 'rotate(' + (Math.sin(t * (ghost ? 6 : 18) + j) * 25) + 'deg)'; });
}

// ------------------------------------------------------------------ render (canvas)
function drawPlayer(c) {
  var px = TXp(player.x), py = TYp(player.y), f = player.face, t = G.t;
  var sq = player.squash.step(player.digging ? 1 + Math.sin(t * 30) * 0.07 : 1, 1 / 60);
  var bob = player.moving ? -Math.abs(Math.sin(t * 16)) * 2.5 : 0;
  c.save(); c.translate(px, py + bob);
  if (G.state === 'dying') { var kk = Math.max(0.01, 1 - player.dieT / 1.3); c.rotate(player.dieT * 14); c.scale(kk, kk); }
  if (f.y) c.rotate(f.y < 0 ? -Math.PI / 2 : Math.PI / 2); else if (f.x < 0) c.scale(-1, 1);
  c.scale(1 / Math.sqrt(sq), sq);
  var g = c.createRadialGradient(8, -2, 2, 8, -2, 40); g.addColorStop(0, 'rgba(255,190,110,0.35)'); g.addColorStop(1, 'rgba(255,190,110,0)');
  c.fillStyle = g; c.beginPath(); c.arc(8, -2, 40, 0, 7); c.fill();                                    // headlamp glow
  c.fillStyle = '#1b1b2a'; var st = player.moving ? Math.sin(t * 16) * 2.5 : 0; c.fillRect(-6 + st, 9, 7, 4); c.fillRect(-1 - st, 9, 7, 4);  // boots
  c.fillStyle = '#8a8f9c'; c.fillRect(-12, -5, 5, 11);                                                     // pack
  c.fillStyle = '#f4f1ea'; c.beginPath(); if (c.roundRect) c.roundRect(-8, -6, 15, 16, 5); else c.rect(-8, -6, 15, 16); c.fill();   // suit
  c.fillStyle = '#2f6bff'; c.beginPath(); c.arc(0, -8, 8, 0, 7); c.fill();                                 // helmet
  c.fillStyle = '#9fe8ff'; c.fillRect(2, -11, 6, 5);                                                       // visor
  c.fillStyle = '#ff3b3b'; c.fillRect(6, 1, 8, 3);                                                         // pump nozzle
  c.restore();
}
function drawHose(c) {
  if (!hose.active) return;
  var px = TXp(player.x) + hose.dir.x * 12, py = TYp(player.y) + hose.dir.y * 12 + 2, len = hose.len * T - 4, w = Math.abs(hose.wob.x);
  var ex = px + hose.dir.x * len, ey = py + hose.dir.y * len;
  c.strokeStyle = '#fff'; c.lineWidth = 3 + w * 0.6; c.lineCap = 'round';
  c.beginPath(); c.moveTo(px, py);
  var n = 10; for (var i = 1; i <= n; i++) { var q = i / n, wig = Math.sin(q * 12 + G.t * 20) * w * 1.5 * Math.sin(q * Math.PI); c.lineTo(px + hose.dir.x * len * q - hose.dir.y * wig, py + hose.dir.y * len * q + hose.dir.x * wig); }
  c.stroke(); c.fillStyle = '#ff3b3b'; c.beginPath(); c.arc(ex, ey, 4 + w * 0.5, 0, 7); c.fill();
}
function render(dt) {
  var c = ctx; c.setTransform(dpr, 0, 0, dpr, 0, 0); c.clearRect(0, 0, vw, vh);
  var s = trauma * trauma * 5, tt = performance.now() / 1000;
  var sx = s * (Math.sin(tt * 91) + Math.sin(tt * 37)) * 0.5, sy = s * (Math.sin(tt * 83 + 1) + Math.sin(tt * 29)) * 0.5;
  layer.style.transform = (sx || sy) ? 'translate(' + sx.toFixed(2) + 'px,' + sy.toFixed(2) + 'px)' : '';
  c.drawImage(patchC, 0, 0, vw, vh);
  if (holesDirty) { hctx.putImageData(hImg, 0, 0); holesDirty = false; }
  c.imageSmoothingEnabled = false; c.drawImage(holeC, 0, 0, GW * CELL, GH * CELL); c.imageSmoothingEnabled = true;
  // ember rims (flicker)
  for (var i = embersL.length - 1; i >= 0; i--) {
    var em = embersL[i]; em.t -= dt;
    if (em.t <= 0 || mat[em.i] === 0) { if (em.t <= 0 && mat[em.i] !== 0 && burn[em.i] < 2) { burn[em.i] = 2; paintCell(em.i); } embersL.splice(i, 1); continue; }
    var k = Math.min(1, em.t), cx = (em.i % GW) * CELL, cy = ((em.i / GW) | 0) * CELL;
    c.fillStyle = k > 0.7 ? '#ffd65a' : k > 0.35 ? '#ff8a2b' : '#b8321e'; c.globalAlpha = (0.55 + Math.random() * 0.45) * Math.min(1, k * 2); c.fillRect(cx, cy, CELL, CELL);
  }
  c.globalAlpha = 1;
  // letters (falling / burning to ash)
  c.textAlign = 'center'; c.textBaseline = 'middle';
  for (var j = letters.length - 1; j >= 0; j--) {
    var L = letters[j], a = 1, color = L.color;
    if (L.st === 'burn') { var bt = L.t; color = bt < 0.5 ? L.color : bt < 1.3 ? '#ff8a2b' : '#3b2d33'; a = bt > 1.8 ? Math.max(0, 1 - (bt - 1.8)) : 1; }
    c.save(); c.translate(L.x, L.y); c.rotate(L.rot); c.globalAlpha = a; c.font = L.font; c.fillStyle = color;
    if (L.st === 'burn' && L.t > 0.4 && L.t < 1.3) { c.shadowColor = '#ff6a1a'; c.shadowBlur = 8; }
    c.fillText(L.ch, 0, 0); c.restore();
  }
  // particles, digger, hose, popups: drawn on the top canvas so they sit above the element-creatures
  c = ctx2; c.setTransform(dpr, 0, 0, dpr, 0, 0); c.clearRect(0, 0, vw, vh);
  for (var p = 0; p < parts.length; p++) {
    var o = parts[p], lk = Math.max(0, o.life / o.max);
    if (o.t === 'sq') { c.fillStyle = o.c; c.globalAlpha = Math.min(1, lk * 2); c.fillRect(o.x - o.size / 2, o.y - o.size / 2, o.size, o.size); }
    else if (o.t === 'em') { c.globalCompositeOperation = 'lighter'; c.fillStyle = o.c; c.globalAlpha = Math.min(1, lk * 1.8) * (0.6 + Math.random() * 0.4); c.fillRect(o.x - o.size / 2, o.y - o.size / 2, o.size, o.size); c.globalCompositeOperation = 'source-over'; }
    else if (o.t === 'sm') { c.fillStyle = o.c; c.globalAlpha = lk * 0.45; c.beginPath(); c.arc(o.x, o.y, o.size * (1.6 - lk * 0.6), 0, 7); c.fill(); }
    else if (o.t === 'cf') { c.save(); c.translate(o.x, o.y); c.rotate(o.rot); c.scale(1, Math.cos(o.life * o.flip)); c.fillStyle = o.c; c.globalAlpha = Math.min(1, lk * 2); c.fillRect(-o.w / 2, -o.h / 2, o.w, o.h); c.restore(); }
    else if (o.t === 'fr') { c.save(); c.translate(o.x, o.y); c.rotate(o.rot); c.globalAlpha = Math.min(1, lk * 2); try { c.drawImage(o.img, o.sx, o.sy, o.sw, o.sh, -o.w / 2, -o.h / 2, o.w, o.h); } catch (e2) {} c.restore(); }
  }
  c.globalAlpha = 1;
  if (bonus) {
    var bx = TXp(bonus.x), by = TYp(bonus.y) + Math.sin(G.t * 4) * 3;
    if (bonus.t > 2 || Math.sin(G.t * 20) > 0) {
      c.fillStyle = 'rgba(125,255,106,0.25)'; c.beginPath(); c.arc(bx, by, 18, 0, 7); c.fill();
      if (bonus.img && bonus.img.complete && bonus.img.naturalWidth) { try { c.drawImage(bonus.img, bx - 12, by - 12, 24, 24); } catch (e3) {} }
      else { c.fillStyle = '#ff7a1a'; c.beginPath(); c.moveTo(bx - 7, by - 6); c.lineTo(bx + 7, by - 6); c.lineTo(bx, by + 12); c.fill(); c.fillStyle = '#3fcf4a'; c.fillRect(bx - 5, by - 13, 3, 7); c.fillRect(bx + 2, by - 13, 3, 7); }
    }
  }
  drawHose(c);
  if (!player.hidden) drawPlayer(c);
  // score popups
  c.font = '800 15px ui-monospace, Menlo, monospace'; c.lineWidth = 4; c.strokeStyle = '#000';
  for (var q = popups.length - 1; q >= 0; q--) { var pp = popups[q]; pp.t -= dt; pp.y -= dt * 30; c.globalAlpha = Math.min(1, pp.t * 2); c.strokeText(pp.text, pp.x, pp.y); c.fillStyle = '#fff'; c.fillText(pp.text, pp.x, pp.y); if (pp.t <= 0) popups.splice(q, 1); }
  c.globalAlpha = 1;
}
function updateParticles(dt) {
  for (var i = parts.length - 1; i >= 0; i--) {
    var o = parts[i]; o.life -= dt; if (o.life <= 0) { parts.splice(i, 1); continue; }
    o.vy += o.g * dt; if (o.drag) { var k = Math.exp(-o.drag * dt); o.vx *= k; o.vy *= k; }
    o.x += o.vx * dt; o.y += o.vy * dt; if (o.vr) o.rot += o.vr * dt;
  }
  for (var j = letters.length - 1; j >= 0; j--) {
    var L = letters[j];
    if (L.st === 'fall' || (L.st === 'burn' && L.moving !== false)) {
      L.vy += 1300 * dt; L.x += L.vx * dt; L.y += L.vy * dt; L.rot += L.vr * dt;
      var below = cellAt(L.x, L.y + L.h * 0.45);
      if (below >= 0 && mat[below] !== 0 && L.vy > 0 && L.t > 0.05) { L.vy *= -0.28; L.vx *= 0.5; L.vr *= 0.5; if (Math.abs(L.vy) < 70) { L.vy = 0; L.moving = false; L.st = 'burn'; } }
    } else if (L.st === 'burn') {
      var b2 = cellAt(L.x, L.y + L.h * 0.5); if (b2 >= 0 && mat[b2] === 0) L.moving = true;
    }
    L.t += dt;
    if (L.st === 'burn' && Math.random() < 0.02) spawn({ t: 'em', x: L.x, y: L.y, vx: rr(-10, 10), vy: rr(-60, -20), g: -20, life: 0.5, max: 0.5, size: 2, c: '#ff8a2b' });
    if ((L.st === 'burn' && L.t > 3.2) || L.y > vh + 60 || L.t > 8) letters.splice(j, 1);
  }
}

// ------------------------------------------------------------------ main loop
// keep keyboard focus in the page: an ad/consent <iframe> that grabs focus would swallow Esc and the arrows
var prevFocus = null;
function grabFocus() {
  if (!running || !host) return;
  var a = D.activeElement;
  if (prevFocus === null) prevFocus = a || false;
  if (a && a !== host && (a.tagName === 'IFRAME' || a.tagName === 'FRAME' || a.tagName === 'INPUT' || a.tagName === 'TEXTAREA' || a.isContentEditable || a.tagName === 'EMBED' || a.tagName === 'OBJECT' || a === D.body || a === D.documentElement)) {
    try { host.tabIndex = -1; host.focus({ preventScroll: true }); } catch (e) {}
  }
}
function step(dt) {
  if (D.activeElement !== host) grabFocus();
  G.t += dt; G.stateT += dt;
  if (G.state === 'ready' && G.stateT > 1.4) setState('play');
  if (G.state === 'play') {
    updatePlayer(dt);
    enemies.forEach(function (e) {
      updateEnemy(e, dt);
      if (e.alive && e.state !== 'ghost' && e.inflate === 0 && Math.hypot(e.x - player.x, e.y - player.y) < 0.6) killPlayer('touch:' + e.text.slice(0, 12));
    });
    if (enemies.length && enemies.every(function (e) { return !e.alive; }) && !rocks.some(function (r) { return r.state === 'falling'; })) { setState('clear'); sfx.bonus(); }
  }
  pumpPressed = false;
  rocks.forEach(function (r) { if (r.state !== 'dead') updateRock(r, dt); });
  if (G.state === 'dying') { player.dieT += dt; if (player.dieT > 1.6) { G.lives--; if (G.lives <= 0) { setState('gameover'); player.hidden = true; } else resetPositions(); } }
  if (G.state === 'clear' && G.stateT > 2.6) { nextScreen(); return; }
  if (bonus) { bonus.t -= dt; if (bonus.t <= 0) bonus = null; }
  enemies.forEach(function (e) { syncEnemy(e, dt); });
  updateParticles(dt); updateDomFrags(dt);
  trauma = Math.max(0, trauma - dt * 1.8);
  // HUD
  setText('s', hud.score, String(G.score)); setText('h', hud.hi, String(G.hi)); setText('r', hud.round, String(G.round));
  setText('l', hud.lives, G.lives > 0 ? '♥'.repeat(G.lives) : '-'); setText('p', hud.pct, (removed / NCELL * 100).toFixed(1) + '%'); setText('a', hud.auto, autopilot ? 'AUTO' : '');
  var m = '', sub = '';
  if (G.state === 'ready') { m = 'SCREEN ' + G.round + '\nREADY!'; sub = enemies.length + ' elements came alive'; }
  else if (G.state === 'clear') { m = 'PAGE CLEARED!'; sub = 'scrolling to the next screen...'; }
  else if (G.state === 'gameover') { m = 'GAME OVER'; sub = 'ENTER to dig again · ESC to restore the page'; }
  else if (paused) m = 'PAUSED';
  setText('m', msgEl, m); setText('sub', hud.sub, sub);
}
function frame(now) {
  if (!running) return;
  try {
    var dt = Math.min(1 / 30, (now - lastT) / 1000 || 0); lastT = now;
    if (!paused && !hold) step(dt);
    if (running) render(paused || hold ? 0 : dt);
  } catch (err) { fail(err); return; }
  raf = requestAnimationFrame(frame);
}

// ------------------------------------------------------------------ lifecycle
function init(carry) {
  var html = D.documentElement;
  vw = html.clientWidth || innerWidth; vh = Math.min(innerHeight, html.clientHeight || innerHeight); dpr = Math.min(2, W.devicePixelRatio || 1);
  GW = Math.ceil(vw / CELL); GH = Math.ceil(vh / CELL); NCELL = GW * GH; COLS = Math.floor(vw / T); ROWS = Math.floor(vh / T);
  offX = Math.floor((vw - COLS * T) / 2); offY = Math.floor((vh - ROWS * T) / 2);
  if (COLS < 12 || ROWS < 8) throw new Error('window too small (need about 400x260)');
  enemies = []; rocks = []; parts = []; letters = []; embersL = []; popups = []; flames = []; modified = []; removed = 0; bonus = null; hudCache = {};
  keys = {}; dirStack = []; paused = false;
  var hi = 0;   // high score lives only for this session: nothing is written to the site's storage
  G = { state: 'ready', t: 0, stateT: 0, score: carry ? carry.score : 0, hi: Math.max(hi, carry ? carry.hi : 0), lives: carry ? carry.lives : 3, round: carry ? carry.round : 1, rocksDropped: 0, bonusSpawned: false, popped: 0, tileDug: new Uint8Array(COLS * ROWS) };
  START = { x: Math.floor(COLS / 2), y: Math.max(3, Math.floor(ROWS * 0.4)) };
  scanPage();
  buildOverlay();
  for (var i = 0; i < NCELL; i++) if (mat[i] === 0) paintCell(i);
  player = { x: START.x, y: START.y, face: { x: 1, y: 0 }, moving: false, digging: false, squash: new Spring(260, 14, 1), dieT: 0, hidden: false };
  hose = { active: false, len: 0, dir: { x: 1, y: 0 }, target: null, retract: false, wob: new Spring(300, 9), holdT: 0 };
  // rocks first (big elements), then a start shaft column that misses them, then enemies
  var rockC = pickRocks(Math.min(4, 3 + (G.round >> 2)), []);
  var shaftHits = function (tx) { var l = TXp(tx) - T * 0.7, r2 = TXp(tx) + T * 0.7, b = TYp(START.y) + T * 0.6; return rockC.some(function (c) { return c.r.left < r2 && c.r.right > l && c.r.top < b; }); };
  for (var dx = 0; dx < COLS / 2; dx++) { if (!shaftHits(START.x + dx)) { START.x += dx; break; } if (!shaftHits(START.x - dx)) { START.x -= dx; break; } }
  player.x = START.x;
  var chosen = pickEnemies(Math.min(9, 5 + G.round)).filter(function (c) {
    var box = { left: Math.min(c.r.left, TXp(c.tx - 1.5)), right: Math.max(c.r.right, TXp(c.tx + 1.5)), top: Math.min(c.r.top, TYp(c.ty - 0.5)), bottom: Math.max(c.r.bottom, TYp(c.ty + 0.5)) };
    return !rockC.some(function (k) { return k.r.left < box.right && k.r.right > box.left && k.r.top < box.bottom && k.r.bottom > box.top; });
  });
  rockC.forEach(function (c, k) { killGlyphsIn(c.r); makeRock(c, k); });
  chosen.forEach(function (c, k) {
    killGlyphsIn(c.r);
    var horiz = c.tx > 0 && c.tx < COLS - 1;
    for (var d = -1; d <= 1; d++) carveTilePocket(horiz ? c.tx + d : c.tx, horiz ? c.ty : clamp(c.ty + d, 0, ROWS - 1));
    spawnEnemy(c, k);
  });
  for (var ty = 0; ty <= START.y; ty++) carveTilePocket(START.x, ty);
  removed = 0;
  running = true;
  listen(W, 'keydown', onKey, true); listen(W, 'keyup', onKey, true); listen(W, 'keypress', function (e) { if (running && (DIRS[e.code] || OURKEYS[e.code])) { e.preventDefault(); e.stopImmediatePropagation(); } }, true);
  listen(W, 'wheel', stopEvt, { capture: true, passive: false }); listen(W, 'touchmove', stopEvt, { capture: true, passive: false });
  var sy0 = W.scrollY, sx0 = W.scrollX; listen(W, 'scroll', function () { if (running && (W.scrollY !== sy0 || W.scrollX !== sx0)) W.scrollTo(sx0, sy0); }, true);
  listen(W, 'blur', function () { keys = {}; dirStack = []; setTimeout(grabFocus, 0); });
  grabFocus();
  if (D.activeElement && D.activeElement.blur) try { D.activeElement.blur(); } catch (e) {}
  lastT = performance.now(); raf = requestAnimationFrame(frame);
}
function teardown() {
  running = false; cancelAnimationFrame(raf);
  listeners.forEach(function (l) { l[0].removeEventListener(l[1], l[2], l[3]); }); listeners = [];
  restoreStyles(); domFrags = [];
  if (host && host.parentNode) host.parentNode.removeChild(host);
  host = null;
  try { if (prevFocus && prevFocus.isConnected && prevFocus !== D.body && prevFocus.tagName !== 'IFRAME') prevFocus.focus({ preventScroll: true }); } catch (e) {}
  prevFocus = null;
}
function quit() { teardown(); try { if (actx) actx.close(); } catch (e) {} actx = null; }
function restartScreen(fresh) { var carry = fresh ? { score: 0, lives: 3, round: 1, hi: G.hi } : null; teardown(); setTimeout(function () { launch(carry); }, 30); }
function nextScreen() {
  var carry = { score: G.score, lives: G.lives, round: G.round + 1, hi: G.hi };
  teardown();
  var y0 = W.scrollY; W.scrollBy(0, Math.round(vh * 0.85));
  setTimeout(function () { launch(carry); }, W.scrollY !== y0 ? 500 : 60);
}
function fail(err) {
  try { teardown(); } catch (e) {}
  var msg = 'DIG//PAGE could not start here: ' + (err && err.message || err) + '\nThis site may block bookmarklets or inline styles (CSP / Trusted Types). Try the userscript version or another page.';
  try {
    var box = mk('div', { position: 'fixed', left: '50%', top: '20px', transform: 'translateX(-50%)', zIndex: '2147483647', background: '#1a0f30', color: '#ffd23a', padding: '12px 16px', borderRadius: '10px', font: '600 13px ui-monospace, Menlo, monospace', whiteSpace: 'pre-wrap', maxWidth: '560px', boxShadow: '0 6px 24px rgba(0,0,0,.5)' });
    box.textContent = msg; D.documentElement.appendChild(box); setTimeout(function () { box.remove(); }, 7000);
  } catch (e) { try { alert(msg); } catch (e2) {} }
}
function launch(carry) { try { init(carry); } catch (err) { fail(err); } }
var hold = false;

launch(null);
})();

  }
  window.addEventListener('keydown', function (e) {
    if (e.altKey && e.shiftKey && e.code === 'KeyD') { e.preventDefault(); digpage(); }
  }, true);
})();
