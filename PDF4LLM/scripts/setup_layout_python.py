#!/usr/bin/env python3
"""Create the local Python venv used by PDF4LLM's pymupdf.layout bridge.

Run once per machine (or after upgrading PDF4LLM).

From a NuGet consumer project:

    dotnet msbuild -t:PDF4LLMSetupLayoutPython

From the MuPDF.NET repo:

    python PDF4LLM/scripts/setup_layout_python.py

Windows:

    powershell -File setup_layout_python.ps1
"""

from __future__ import annotations

import argparse
import os
import shutil
import subprocess
import sys
import textwrap
from pathlib import Path

SCRIPT_DIR = Path(__file__).resolve().parent
REQUIREMENTS = SCRIPT_DIR / "requirements-layout.txt"


def default_venv_path() -> Path:
    """Must match PDF4LLM.Layout.LayoutPythonPaths.UserLocalVenvRoot()."""
    if sys.platform == "win32":
        local = os.environ.get("LOCALAPPDATA")
        base = Path(local) if local else Path.home() / "AppData" / "Local"
        return base / "PDF4LLM" / ".venv-layout"
    return Path.home() / ".local" / "share" / "pdf4llm" / ".venv-layout"


def venv_python(venv_root: Path) -> Path:
    if sys.platform == "win32":
        return venv_root / "Scripts" / "python.exe"
    return venv_root / "bin" / "python"


def run(cmd: list[str]) -> None:
    print("+", " ".join(cmd))
    try:
        subprocess.check_call(cmd)
    except subprocess.CalledProcessError as exc:
        print(f"Command failed with exit code {exc.returncode}: {' '.join(cmd)}", file=sys.stderr)
        raise SystemExit(exc.returncode) from exc


def check_linux_prerequisites(base: str) -> None:
    if sys.platform == "win32":
        return

    if subprocess.run(
        [base, "-m", "venv", "--help"],
        stdout=subprocess.DEVNULL,
        stderr=subprocess.DEVNULL,
    ).returncode != 0:
        print("Python venv module is not available.", file=sys.stderr)
        print("On Debian/Ubuntu install:", file=sys.stderr)
        print("  sudo apt install python3-venv python3-pip", file=sys.stderr)
        raise SystemExit(1)


def pip_available(py: Path) -> bool:
    return subprocess.run(
        [str(py), "-m", "pip", "--version"],
        stdout=subprocess.DEVNULL,
        stderr=subprocess.DEVNULL,
    ).returncode == 0


def ensure_pip(py: Path, venv_root: Path) -> None:
    if pip_available(py):
        return

    print("+", str(py), "-m ensurepip --upgrade")
    try:
        subprocess.check_call([str(py), "-m", "ensurepip", "--upgrade"])
    except subprocess.CalledProcessError:
        pass

    if pip_available(py):
        return

    print("pip is not available in the layout venv.", file=sys.stderr)
    print(
        "On Debian/Ubuntu install system packages, remove the broken venv, and retry:",
        file=sys.stderr,
    )
    print("  sudo apt install python3-pip python3-venv", file=sys.stderr)
    print(f"  rm -rf {venv_root}", file=sys.stderr)
    raise SystemExit(1)


def create_venv(base: str, venv_root: Path) -> None:
    cmd = [base, "-m", "venv", str(venv_root)]
    if sys.version_info >= (3, 12):
        try:
            run(cmd + ["--upgrade-deps"])
            return
        except SystemExit:
            if venv_root.is_dir():
                shutil.rmtree(venv_root)
            print("Retrying venv creation without --upgrade-deps ...", file=sys.stderr)
    run(cmd)


def required_layout_version() -> str:
    for line in REQUIREMENTS.read_text(encoding="utf-8").splitlines():
        line = line.strip()
        if not line or line.startswith("#"):
            continue
        if line.startswith("pymupdf-layout=="):
            return line.split("==", 1)[1].strip()
    print("pymupdf-layout pin missing from requirements-layout.txt", file=sys.stderr)
    raise SystemExit(1)


def normalize_version(version: str) -> str:
    return (version or "").strip().split("-", 1)[0]


def resolve_layout_python(venv_root: Path | None) -> Path | None:
    env = os.environ.get("PDF4LLM_PYTHON") or os.environ.get("PYTHON")
    if env:
        candidate = Path(env)
        if candidate.is_file():
            return candidate

    if venv_root is not None:
        py = venv_python(venv_root)
        if py.is_file():
            return py

    for root in (default_venv_path(),):
        py = venv_python(root)
        if py.is_file():
            return py

    return None


def verify_layout_python(py: Path, required: str) -> int:
    try:
        out = subprocess.check_output(
            [str(py), "-c", "import pymupdf.layout; print(pymupdf.layout.version)"],
            text=True,
            stderr=subprocess.STDOUT,
        ).strip()
    except (subprocess.CalledProcessError, FileNotFoundError):
        print("pymupdf-layout is not installed.", file=sys.stderr)
        print(
            "Install with:\n"
            "  dotnet msbuild -t:PDF4LLMSetupLayoutPython",
            file=sys.stderr,
        )
        return 1

    if normalize_version(out) != normalize_version(required):
        print(
            f"pymupdf-layout {out} is installed; PDF4LLM requires pymupdf-layout {required}.",
            file=sys.stderr,
        )
        print(
            "Refresh with:\n"
            "  dotnet msbuild -t:PDF4LLMSetupLayoutPython",
            file=sys.stderr,
        )
        return 1

    print(f"Verified pymupdf-layout {out}")
    return 0


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument(
        "--venv",
        type=Path,
        default=None,
        help=f"venv directory (default: {default_venv_path()})",
    )
    parser.add_argument(
        "--python",
        dest="base_python",
        default=None,
        help="Python used to create the venv (default: this interpreter)",
    )
    parser.add_argument("--skip-verify", action="store_true")
    parser.add_argument(
        "--verify-only",
        action="store_true",
        help="check pymupdf-layout version without creating or updating the venv",
    )
    args = parser.parse_args()

    required = required_layout_version()

    if args.verify_only:
        venv_root = (args.venv or default_venv_path()).resolve()
        py = resolve_layout_python(venv_root if venv_root.is_dir() else None)
        if py is None:
            print("No layout Python interpreter found.", file=sys.stderr)
            print(
                "Install with:\n"
                "  dotnet msbuild -t:PDF4LLMSetupLayoutPython",
                file=sys.stderr,
            )
            return 1
        print(f"Checking layout Python: {py}")
        return verify_layout_python(py, required)

    if not REQUIREMENTS.is_file():
        print(f"Missing {REQUIREMENTS}", file=sys.stderr)
        return 1

    base = args.base_python or sys.executable
    venv_root = (args.venv or default_venv_path()).resolve()
    py = venv_python(venv_root)

    print(f"Using Python: {base}")
    print(f"Requirements: {REQUIREMENTS}")
    print(f"Layout venv:  {venv_root}")

    check_linux_prerequisites(base)

    if not venv_root.is_dir():
        venv_root.parent.mkdir(parents=True, exist_ok=True)
        create_venv(base, venv_root)
    elif not py.is_file():
        print(f"venv exists but interpreter not found: {py}", file=sys.stderr)
        return 1

    ensure_pip(py, venv_root)
    run([str(py), "-m", "pip", "install", "--upgrade", "pip"])
    run([str(py), "-m", "pip", "install", "-r", str(REQUIREMENTS)])

    if not args.skip_verify:
        if verify_layout_python(py, required) != 0:
            return 1

    print(
        textwrap.dedent(
            f"""
            Layout Python environment ready:
              {venv_root}
              interpreter: {py}

            PDF4LLM discovers this venv automatically when present.
            To use another interpreter, set PDF4LLM_PYTHON:
              {py}
            """
        ).strip()
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
