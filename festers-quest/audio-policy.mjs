// Compatibility bridge for the pinned official @rive-app/canvas 2.43.1 runtime.
// Its public Rive.volume controls artboard volume, but it does not expose a
// method to resume the miniaudio playback AudioContext after Safari interrupts it.
export function playbackState(miniaudio) {
  const devices = miniaudio?.devices?.filter(device =>
    device?.N && device.state === miniaudio.device_state?.started) || [];
  if (!devices.length) return 'missing';
  return devices.every(device => device.N.state === 'running') ? 'running' : 'suspended';
}

// Call synchronously inside a trusted click or key event, before any await.
// The runtime's own unlock resumes its playback contexts and notifies native
// audio after resume; calling AudioContext.resume() alone skips that callback.
export function requestPlaybackUnlock(miniaudio) {
  if (typeof miniaudio?.unlock !== 'function') return false;
  miniaudio.unlock();
  return true;
}

// Rive's public artboard volume does not silence already playing scripted
// Audio.play voices immediately. Suspend the runtime's existing playback
// contexts on pause, background, or shutdown; the next user gesture can unlock them.
export function suspendPlayback(miniaudio) {
  const devices = miniaudio?.devices?.filter(device =>
    device?.N && device.state === miniaudio.device_state?.started) || [];
  return Promise.allSettled(devices.map(device => device.N.suspend()));
}

export async function waitForPlayback(getMiniaudio, delay = ms => new Promise(resolve => setTimeout(resolve, ms))) {
  for (let attempt = 0; attempt < 8; attempt++) {
    if (playbackState(getMiniaudio()) === 'running') return true;
    await delay(120);
  }
  return playbackState(getMiniaudio()) === 'running';
}
