#!/usr/bin/env sh
set -eu
script_dir=$(CDPATH= cd -- "$(dirname -- "$0")" && pwd)
if command -v python3 >/dev/null 2>&1; then
    exec python3 "$script_dir/setup_layout_python.py" "$@"
elif command -v python >/dev/null 2>&1; then
    exec python "$script_dir/setup_layout_python.py" "$@"
else
    echo "Python not found. Install Python 3.10+ or set PYTHON to an interpreter." >&2
    exit 1
fi
