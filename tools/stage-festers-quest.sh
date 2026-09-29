#!/bin/sh
# Copy a finished, reviewed static demo into this Pages checkout.
set -eu
if [ "$#" -ne 1 ]; then
  echo "Usage: tools/stage-festers-quest.sh STATIC_DEMO_DIRECTORY" >&2
  exit 2
fi
source_dir=$(cd "$1" && pwd -P)
repo_dir=$(cd "$(dirname "$0")/.." && pwd -P)
destination="$repo_dir/festers-quest"
case "$source_dir/" in
  "$repo_dir/"*) echo "Source must be outside the Pages checkout" >&2; exit 2 ;;
esac
if [ -e "$source_dir/.not-publishable" ]; then
  echo "Refusing layout-only demo marked .not-publishable: $source_dir" >&2
  exit 2
fi
if [ -e "$destination" ]; then
  echo "Destination already exists; review it before replacing files: $destination" >&2
  exit 2
fi
for required in index.html app.js controller.mjs style.css game.riv preview.png RUNTIME-NOTICE.md runtime/LICENSE runtime/rive.js runtime/rive.wasm runtime/rive_fallback.wasm release.json; do
  if [ ! -s "$source_dir/$required" ]; then
    echo "Release file missing or empty: $source_dir/$required" >&2
    exit 2
  fi
done
# This checks file consistency with the builder's metadata. It does not
# authenticate a Rive signature; the official CLI publish and browser QA do that.
python3 - "$source_dir" <<'PY'
import hashlib
import json
import sys
from pathlib import Path

source = Path(sys.argv[1])
known_runtime_hashes = {
    '@rive-app/webgl2': {
        'rive.js': 'c8f8309c148d5773e79bb218c76dcd2bcc3079a0e98d1fb544ce32c2778eb0a6',
        'rive.wasm': '3120562f4eeba0b916245f2afb41224b7db6d661486a284b00d9a8bcd046e8ef',
        'rive_fallback.wasm': '6e61277a0d7f878067ad15a2c471c86640068ac49e2005aa7e3aa2ad046cb220',
    },
    '@rive-app/canvas': {
        'rive.js': '60928b9d4d0e88e430effe7b3e090e1e4e7b8b2852a6f020bf488952405c0949',
        'rive.wasm': 'cc0827eddd7ce5d37a8691b0e587b856972b4fe3d36841864ab27e7fa4da0ef8',
        'rive_fallback.wasm': '87cd41a73965b680c74ecec4e6db53090b02684c5fac19d7f82754f41829b0b9',
    },
}
def require(condition, message):
    if not condition:
        raise ValueError(message)

try:
    release = json.loads((source / 'release.json').read_text())
    game = release['game']
    require(release['schema'] == 1, 'unsupported schema')
    require(game['file'] == 'game.riv', 'unexpected game filename')
    require(game['bytes'] == (source / 'game.riv').stat().st_size, 'game byte count')
    require(game['sha256'] == hashlib.sha256((source / 'game.riv').read_bytes()).hexdigest(), 'game SHA-256')
    runtime = release['runtime']
    require(runtime['package'] in known_runtime_hashes, 'unexpected runtime package')
    require(runtime['version'] == '2.43.1', 'unexpected runtime version')
    expected_hashes = known_runtime_hashes[runtime['package']]
    require(runtime['filesSha256'] == expected_hashes, 'runtime metadata hashes')
    runtime_files = {path.name for path in (source / 'runtime').iterdir()}
    require(runtime_files == set(expected_hashes) | {'LICENSE'}, 'unexpected runtime file list')
    for name, expected in expected_hashes.items():
        actual = hashlib.sha256((source / 'runtime' / name).read_bytes()).hexdigest()
        require(expected == actual, f'{name} SHA-256')
except (KeyError, OSError, ValueError, TypeError) as error:
    raise SystemExit(f'Release metadata does not match staged files: {error}')
PY
cp -R "$source_dir" "$destination"
echo "Copied $source_dir to $destination"
echo "Review with: git -C '$repo_dir' status --short"
