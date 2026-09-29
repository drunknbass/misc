// Browser controller input only. NES state and polling stay inside the Rive ROM.
export function directionBit(rect, x, y) {
  if (x < rect.left || x > rect.right || y < rect.top || y > rect.bottom) return 0;
  const dx = (x - (rect.left + rect.right) / 2) / (rect.width / 2);
  const dy = (y - (rect.top + rect.bottom) / 2) / (rect.height / 2);
  const threshold = 0.28;
  return (dx > threshold ? 128 : dx < -threshold ? 64 : 0) |
    (dy > threshold ? 32 : dy < -threshold ? 16 : 0);
}

export function combineBitmask(values) {
  let mask = 0;
  for (const bit of values) mask |= bit;
  return mask;
}

export function bindController({ pad, buttons, onChange, enabled }) {
  const pointers = new Map();
  const clicks = new Set();
  const clickTimers = new Set();
  const events = new AbortController();
  const options = { signal: events.signal };
  const emit = () => onChange(combineBitmask([...pointers.values(), ...clicks]));

  function release() {
    pointers.clear();
    clicks.clear();
    for (const timer of clickTimers) clearTimeout(timer);
    clickTimers.clear();
    emit();
  }

  function start(event, element, bit) {
    if (!enabled()) return;
    event.preventDefault();
    element.setPointerCapture(event.pointerId);
    pointers.set(event.pointerId, bit);
    emit();
  }

  function end(event) {
    if (!pointers.has(event.pointerId)) return;
    pointers.delete(event.pointerId);
    emit();
  }

  pad.addEventListener('pointerdown', event => {
    start(event, pad, directionBit(pad.getBoundingClientRect(), event.clientX, event.clientY));
  }, options);
  pad.addEventListener('pointermove', event => {
    if (!pointers.has(event.pointerId)) return;
    event.preventDefault();
    const next = directionBit(pad.getBoundingClientRect(), event.clientX, event.clientY);
    if (next !== pointers.get(event.pointerId)) { pointers.set(event.pointerId, next); emit(); }
  }, options);
  for (const type of ['pointerup', 'pointercancel', 'lostpointercapture']) pad.addEventListener(type, end, options);
  pad.addEventListener('contextmenu', event => event.preventDefault(), options);

  for (const button of buttons) {
    const bit = Number(button.dataset.bit);
    if (!pad.contains(button)) {
      button.addEventListener('pointerdown', event => start(event, button, bit), options);
      for (const type of ['pointerup', 'pointercancel', 'lostpointercapture']) button.addEventListener(type, end, options);
    }
    button.addEventListener('contextmenu', event => event.preventDefault(), options);
    button.addEventListener('click', event => {
      // Keyboard/assistive clicks have detail 0. Pointer activation was handled
      // by the held pointer and must not extend the press after pointerup.
      if (!enabled() || event.detail !== 0) return;
      clicks.add(bit);
      emit();
      const timer = setTimeout(() => {
        clickTimers.delete(timer);
        clicks.delete(bit);
        emit();
      }, 120);
      clickTimers.add(timer);
    }, options);
  }

  return {
    get mask() { return combineBitmask([...pointers.values(), ...clicks]); },
    release,
    dispose() { release(); events.abort(); },
  };
}
