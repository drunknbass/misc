import { bindController } from './controller.mjs';

(() => {
  'use strict';

  // NES controller bits consumed by the ROM's existing $4016 reader.
  const bits = new Map([
    ['KeyZ', 1], ['KeyX', 2], ['ShiftLeft', 4], ['ShiftRight', 4],
    ['Enter', 8], ['ArrowUp', 16], ['ArrowDown', 32],
    ['ArrowLeft', 64], ['ArrowRight', 128],
  ]);
  const $ = id => document.getElementById(id);
  const canvas = $('game');
  const cover = $('load-cover');
  const loadTitle = $('load-title');
  const loadDetail = $('load-detail');
  const statusText = $('status-text');
  const statusLight = $('status-light');
  const themeLabel = $('theme-label');
  const themeButton = $('theme-button');
  const muteButton = $('mute-button');
  const pauseButton = $('pause-button');
  const directionPad = $('direction-pad');
  const controlButtons = [...document.querySelectorAll('[data-bit]')];
  const controls = [themeButton, muteButton, pauseButton, ...controlButtons];
  // Safari can still dispatch proprietary pinch gestures despite viewport limits.
  // Prevent only those gestures; ordinary pointer events keep reaching the pad
  // and buttons, including simultaneous direction and action touches.
  for (const type of ['gesturestart', 'gesturechange']) {
    document.addEventListener(type, event => event.preventDefault(), { passive: false });
  }
  document.querySelector('.shell').addEventListener('selectstart', event => event.preventDefault());
  const heldKeys = new Set();
  const diagnosticNames = ['frames', 'mode', 'fault', 'playerWorldX', 'playerWorldY',
    'graphicsTheme', 'hostButtons', 'hostMuted', 'audioStarted', 'audioMissing'];
  const diagnosticOutput = new URLSearchParams(location.search).get('diagnostics') === '1'
    ? document.createElement('output') : null;
  if (diagnosticOutput) {
    diagnosticOutput.className = 'diagnostics';
    diagnosticOutput.textContent = 'Rive diagnostics: waiting for game file';
    document.querySelector('.game-panel').append(diagnosticOutput);
  }
  let diagnosticTimer = null;
  let progressTimer = null;
  let lastDiagnosticSnapshot = 'No ROM frame sampled';
  let touchMask = 0;
  let game = null;
  let vm = null;
  let buttonsProperty = null;
  let muteProperty = null;
  let themeProperty = null;
  let frameProperty = null;
  let userPaused = false;
  let ready = false;
  let failed = false;

  function setStatus(message, kind = '') {
    statusText.textContent = message;
    statusLight.className = `status-light ${kind}`;
  }

  function fail(message, detail) {
    if (failed) return;
    failed = true;
    ready = false;
    if (diagnosticOutput && vm) {
      try {
        lastDiagnosticSnapshot = diagnosticNames.map(name =>
          `${name}: ${vm.number(name)?.value ?? 'unavailable'}`).join(' · ');
      } catch { /* Retain the last successful sample if the runtime is gone. */ }
    }
    for (const control of controls) control.disabled = true;
    cover.hidden = false;
    cover.classList.add('error');
    loadTitle.textContent = message;
    loadDetail.textContent = detail;
    setStatus(message, 'error');
    if (diagnosticOutput) diagnosticOutput.textContent = `Stopped: ${message} — ${detail} · ${lastDiagnosticSnapshot}`;
    if (diagnosticTimer !== null) clearInterval(diagnosticTimer);
    if (progressTimer !== null) clearInterval(progressTimer);
    try {
      releaseControls();
      if (game) { game.volume = 0; game.pause(); }
    } catch (error) { console.error('Game shutdown after failure:', error); }
    console.error(`Fester's Quest demo: ${message} ${detail}`);
  }

  function controllerMask() {
    let mask = touchMask;
    for (const code of heldKeys) mask |= bits.get(code) || 0;
    return mask;
  }

  function syncButtons() {
    if (buttonsProperty) buttonsProperty.value = controllerMask();
    for (const button of controlButtons) {
      const bit = Number(button.dataset.bit);
      button.classList.toggle('held', (controllerMask() & bit) !== 0);
    }
  }

  function releaseControls() {
    heldKeys.clear();
    controller.release();
    syncButtons();
  }

  const controller = bindController({
    pad: directionPad,
    buttons: controlButtons,
    enabled: () => ready && !userPaused,
    onChange: mask => { touchMask = mask; syncButtons(); },
  });

  function setTheme(modern) {
    if (!themeProperty) return;
    themeProperty.value = modern ? 1 : 0;
    themeButton.setAttribute('aria-pressed', String(modern));
    themeButton.firstChild.textContent = modern ? 'Original graphics ' : 'Modern graphics ';
    themeLabel.textContent = modern ? 'Modern graphics' : 'Original graphics';
  }

  function setMute(muted) {
    if (!muteProperty) return;
    muteProperty.value = muted ? 1 : 0;
    if (game) game.volume = muted || userPaused || document.hidden ? 0 : 1;
    muteButton.setAttribute('aria-pressed', String(muted));
    muteButton.firstChild.textContent = muted ? 'Sound off ' : 'Sound on ';
  }

  function setPaused(paused) {
    userPaused = paused;
    releaseControls();
    if (game) {
      if (paused || document.hidden) { game.volume = 0; game.pause(); }
      else { game.play(); game.volume = muteProperty?.value >= 0.5 ? 0 : 1; }
    }
    pauseButton.setAttribute('aria-pressed', String(paused));
    pauseButton.firstChild.textContent = paused ? 'Resume ' : 'Pause ';
    if (ready) setStatus(paused ? 'Paused' : 'Resuming game', paused ? '' : 'ready');
  }

  window.addEventListener('keydown', event => {
    // Let focused HTML controls keep their native Enter/Space activation.
    if (event.target instanceof HTMLButtonElement &&
        (event.code === 'Enter' || event.code === 'Space')) return;
    const bit = bits.get(event.code);
    if (bit) {
      event.preventDefault();
      if (ready && !userPaused) {
        heldKeys.add(event.code);
        syncButtons();
      }
      return;
    }
    if (!ready || event.repeat) return;
    if (event.code === 'KeyT') {
      event.preventDefault();
      setTheme(themeProperty.value < 0.5);
    } else if (event.code === 'KeyM') {
      event.preventDefault();
      setMute(muteProperty.value < 0.5);
    } else if (event.code === 'KeyP') {
      event.preventDefault();
      setPaused(!userPaused);
    }
  });
  window.addEventListener('keyup', event => {
    if (!bits.has(event.code)) return;
    event.preventDefault();
    heldKeys.delete(event.code);
    syncButtons();
  });
  window.addEventListener('blur', releaseControls);
  window.addEventListener('orientationchange', releaseControls);
  window.addEventListener('resize', releaseControls);
  document.addEventListener('visibilitychange', () => {
    if (document.hidden) {
      releaseControls();
      if (game) { game.volume = 0; game.pause(); }
      if (ready) setStatus('Paused while hidden');
    } else if (ready && !userPaused) {
      game?.play();
      if (game) game.volume = muteProperty?.value >= 0.5 ? 0 : 1;
      setStatus('Resuming game');
    }
  });
  window.addEventListener('pagehide', () => {
    if (diagnosticTimer !== null) clearInterval(diagnosticTimer);
    if (progressTimer !== null) clearInterval(progressTimer);
    releaseControls();
    controller.dispose();
    game?.cleanup();
    game = null;
  });
  window.addEventListener('pageshow', event => {
    if (event.persisted) location.reload();
  });

  themeButton.addEventListener('click', () => setTheme(themeProperty.value < 0.5));
  muteButton.addEventListener('click', () => setMute(muteProperty.value < 0.5));
  pauseButton.addEventListener('click', () => setPaused(!userPaused));

  function loaded() {
    if (failed || !game) return;
    vm = game.viewModelInstance;
    if (!vm) {
      fail('Game controls are unavailable', 'The Rive view model did not bind. Please reload.');
      return;
    }
    buttonsProperty = vm.number('hostButtons');
    muteProperty = vm.number('hostMuted');
    themeProperty = vm.number('graphicsTheme');
    frameProperty = vm.number('frames');
    if (!buttonsProperty || !muteProperty || !themeProperty || !frameProperty) {
      fail('Game controls are unavailable', 'This game file is missing its browser input bridge.');
      return;
    }
    buttonsProperty.value = 0;
    for (const control of controls) control.disabled = false;
    setTheme(themeProperty.value >= 0.5);
    setMute(muteProperty.value >= 0.5);
    game.resizeDrawingSurfaceToCanvas();
    cover.hidden = true;
    ready = true;
    setStatus('Starting game');
    if (diagnosticOutput) {
      const fields = diagnosticNames.map(name => [name, vm.number(name)]);
      const refresh = () => {
        lastDiagnosticSnapshot = fields.map(([name, field]) =>
          `${name}: ${field?.value ?? 'unavailable'}`).join(' · ');
        diagnosticOutput.textContent = lastDiagnosticSnapshot;
      };
      refresh();
      diagnosticTimer = setInterval(refresh, 250);
    }

    // A signed file must keep advancing the Luau ROM interpreter, not merely show art.
    const faultProperty = vm.number('fault');
    let lastFrame = frameProperty.value;
    let unchangedSince = performance.now();
    progressTimer = setInterval(() => {
      if (!ready) return;
      const frame = frameProperty.value;
      if (faultProperty?.value > 0) {
        fail('Game interpreter stopped', `ROM fault ${faultProperty.value} at frame ${frame}. Reload to restart.`);
        return;
      }
      if (userPaused || document.hidden) { lastFrame = frame; unchangedSince = performance.now(); return; }
      if (frame !== lastFrame) {
        lastFrame = frame;
        unchangedSince = performance.now();
        if (statusText.textContent === 'Starting game' || statusText.textContent === 'Resuming game') {
          setStatus('Playing', 'ready');
        }
      } else if (performance.now() - unchangedSince > 4000) {
        fail('Game stopped advancing', `ROM frame ${frame} has not changed. Reload to restart.`);
      }
    }, 500);
  }

  function initialize() { try {
    if (!window.rive?.Rive) throw new Error('The local Rive runtime did not load.');
    const { Rive, RuntimeLoader, Layout, Fit, Alignment } = window.rive;
    RuntimeLoader.setWasmUrl(new URL('./runtime/rive.wasm', document.baseURI).href);
    RuntimeLoader.setWasmFallbackUrl(new URL('./runtime/rive_fallback.wasm', document.baseURI).href);
    game = new Rive({
      src: new URL('./game.riv', document.baseURI).href,
      canvas,
      artboard: 'Fester ROM CPU Probe',
      stateMachine: 'Probe',
      autoplay: true,
      autoBind: true,
      enableRiveAssetCDN: false,
      shouldDisableRiveListeners: true,
      layout: new Layout({ fit: Fit.Contain, alignment: Alignment.Center }),
      onLoad: () => queueMicrotask(loaded),
      onLoadError: event => fail('Game file could not load', String(event?.data || 'Check the connection and reload.')),
    });
    const resize = new ResizeObserver(() => game?.resizeDrawingSurfaceToCanvas());
    resize.observe($('screen'));
  } catch (error) {
    fail('Game runtime could not start', error.message || String(error));
  } }
  canvas.addEventListener('webglcontextlost', () => {
    fail('Graphics context lost', 'The browser stopped rendering. Reload the page to restart the game.');
  });
  if (document.readyState === 'loading') document.addEventListener('DOMContentLoaded', initialize, { once: true });
  else initialize();
})();
