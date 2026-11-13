#!/usr/bin/env python3
"""Create the local Python venv used by MuPDF.NET's pymupdf 2.1 layout worker.

Run once per machine (or after upgrading MuPDF.NET).

From a NuGet consumer project:

    dotnet msbuild -t:MuPDFNetSetupLayoutPython

From the MuPDF.NET repo:

    python MuPDF.NET/PDF4LLM/scripts/setup_layout_python.py

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
# pymupdf 2.1 wheels declare Requires-Python >=3.11 (Ubuntu 22.04 python3 is 3.10).
MIN_PYTHON = (3, 11)
VERSIONED_PYTHON_NAMES = ("python3.13", "python3.12", "python3.11")


def default_venv_path() -> Path:
    """Must match LayoutPythonPaths.UserLocalVenvRoot()."""
    if sys.platform == "win32":
        local = os.environ.get("LOCALAPPDATA")
        base = Path(local) if local else Path.home() / "AppData" / "Local"
        return base / "MuPDF.NET" / ".venv-layout"
    return Path.home() / ".local" / "share" / "mupdf.net" / ".venv-layout"


def legacy_venv_path() -> Path:
    """Pre-4.0 venv folder from the deprecated PDF4LLM package (still probed at runtime)."""
    if sys.platform == "win32":
        local = os.environ.get("LOCALAPPDATA")
        base = Path(local) if local else Path.home() / "AppData" / "Local"
        return base / "MuPDF.NET.PDF4LLM" / ".venv-layout"
    return Path.home() / ".local" / "share" / "mupdf4llm.net" / ".venv-layout"


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


def format_py_version(ver: tuple[int, int] | None) -> str:
    if ver is None:
        return "unknown"
    return f"{ver[0]}.{ver[1]}"


def interpreter_version(exe: str | Path) -> tuple[int, int] | None:
    try:
        out = subprocess.check_output(
            [str(exe), "-c", "import sys; print(f'{sys.version_info.major}.{sys.version_info.minor}')"],
            text=True,
            stderr=subprocess.DEVNULL,
        ).strip()
        major, minor = out.split(".", 1)
        return int(major), int(minor)
    except (subprocess.CalledProcessError, FileNotFoundError, OSError, ValueError):
        return None


def print_setup_guide(required: str, venv_root: Path | None = None) -> None:
    """Full install steps for verify/setup failures (matches README layout section)."""
    venv_root = venv_root or default_venv_path()
    print(
        textwrap.dedent(
            f"""
            Install pymupdf {required} layout once per machine.

            Debian/Ubuntu block pip into system Python (PEP 668 / externally-managed-environment).
            Do not run: python3 -m pip install pymupdf=={required}
            Create a venv and pip into that interpreter.

            Preferred (creates the venv and installs):
              dotnet msbuild -t:MuPDFNetSetupLayoutPython

            Manual install:
              sudo apt install python3.11 python3.11-venv python3.11-full   # Ubuntu 22.04 (python3 is 3.10)
              sudo apt install python3-venv python3-pip python3-full        # Ubuntu 24.04+
              rm -rf {venv_root}
              python3 -m venv {venv_root}
              {venv_python(venv_root)} -m pip install pymupdf=={required} pymupdf-layout=={required}

            On Ubuntu 22.04 use python3.11 -m venv instead of python3 -m venv.

            Then check:
              dotnet msbuild -t:MuPDFNetVerifyLayoutPython

            Or set MUPDF_NET_PYTHON to a Python 3.11+ interpreter that already has pymupdf {required}.
            """
        ).strip(),
        file=sys.stderr,
    )


def print_debian_help(venv_root: Path) -> None:
    print(
        textwrap.dedent(
            f"""
            On Debian/Ubuntu, pymupdf 2.1 needs Python 3.11+ (Ubuntu 22.04's python3 is 3.10):
              sudo apt install python3.11 python3.11-venv python3.11-full
            If python3 is already 3.11+, install:
              sudo apt install python3-venv python3-pip python3-full

            Then remove any broken layout venv and retry:
              rm -rf {venv_root}
              dotnet msbuild -t:MuPDFNetSetupLayoutPython
            """
        ).strip(),
        file=sys.stderr,
    )


def print_python311_help(exe: str | None = None, ver: tuple[int, int] | None = None) -> None:
    venv_root = default_venv_path()
    current = f"{exe} ({format_py_version(ver)})" if exe else "python3"
    print(
        textwrap.dedent(
            f"""
            pymupdf 2.1 requires Python 3.11 or newer.
            Selected interpreter is too old: {current}

            Install Python 3.11+ and retry:
              sudo apt install python3.11 python3.11-venv python3.11-full
              rm -rf {venv_root}
              dotnet msbuild -t:MuPDFNetSetupLayoutPython

            Or set MUPDF_NET_PYTHON to a Python 3.11+ interpreter.
            """
        ).strip(),
        file=sys.stderr,
    )


def find_base_python(explicit: str | None) -> str:
    """Pick a Python 3.11+ interpreter. Ubuntu 22.04's `python3` is 3.10."""
    if explicit:
        ver = interpreter_version(explicit)
        if ver is None:
            print(f"Not a usable Python interpreter: {explicit}", file=sys.stderr)
            raise SystemExit(1)
        if ver < MIN_PYTHON:
            print_python311_help(explicit, ver)
            raise SystemExit(1)
        return explicit

    candidates: list[str] = []
    env = os.environ.get("MUPDF_NET_PYTHON") or os.environ.get("MuPDF4LLM_NET_PYTHON")
    if env:
        candidates.append(env)
    for name in VERSIONED_PYTHON_NAMES:
        found = shutil.which(name)
        if found:
            candidates.append(found)
    candidates.append(sys.executable)
    for name in ("python3", "python"):
        found = shutil.which(name)
        if found:
            candidates.append(found)

    seen: set[str] = set()
    for raw in candidates:
        try:
            resolved = str(Path(raw).resolve()) if Path(raw).is_file() else raw
        except OSError:
            resolved = raw
        if resolved in seen:
            continue
        seen.add(resolved)
        if not Path(raw).is_file() and shutil.which(raw) is None:
            continue
        ver = interpreter_version(raw)
        if ver is not None and ver >= MIN_PYTHON:
            return raw

    print_python311_help(sys.executable, interpreter_version(sys.executable))
    raise SystemExit(1)


def check_linux_prerequisites(base: str) -> None:
    if sys.platform == "win32":
        return

    if subprocess.run(
        [base, "-m", "venv", "--help"],
        stdout=subprocess.DEVNULL,
        stderr=subprocess.DEVNULL,
    ).returncode != 0:
        print("Python venv module is not available.", file=sys.stderr)
        print_debian_help(default_venv_path())
        raise SystemExit(1)


def is_isolated_venv(py: Path, venv_root: Path) -> bool:
    """True when *py* is a real virtualenv interpreter (PEP 668 does not apply)."""
    if not py.is_file() or not (venv_root / "pyvenv.cfg").is_file():
        return False
    try:
        out = subprocess.check_output(
            [
                str(py),
                "-c",
                "import sys; print('1' if sys.prefix != sys.base_prefix else '0')",
            ],
            text=True,
            stderr=subprocess.DEVNULL,
        ).strip()
    except (subprocess.CalledProcessError, FileNotFoundError, OSError):
        return False
    return out == "1"


def remove_venv(venv_root: Path) -> None:
    if venv_root.is_dir():
        print(f"Removing broken layout venv: {venv_root}", file=sys.stderr)
        shutil.rmtree(venv_root)


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
    print_debian_help(venv_root)
    raise SystemExit(1)


def create_venv(base: str, venv_root: Path) -> None:
    # Avoid --upgrade-deps on Unix: on Debian/Ubuntu it can fail with PEP 668
    # during bootstrap and leave a half-broken tree that looks like a venv.
    cmd = [base, "-m", "venv", str(venv_root)]
    if sys.platform == "win32" and sys.version_info >= (3, 12):
        try:
            run(cmd + ["--upgrade-deps"])
            return
        except SystemExit:
            if venv_root.is_dir():
                shutil.rmtree(venv_root)
            print("Retrying venv creation without --upgrade-deps ...", file=sys.stderr)
    run(cmd)


def ensure_venv(base: str, venv_root: Path) -> Path:
    py = venv_python(venv_root)
    ver = interpreter_version(py) if py.is_file() else None

    if venv_root.is_dir() and ver is not None and ver < MIN_PYTHON:
        print(
            f"Existing layout venv is Python {format_py_version(ver)}; "
            f"pymupdf 2.1 needs {MIN_PYTHON[0]}.{MIN_PYTHON[1]}+.",
            file=sys.stderr,
        )
        remove_venv(venv_root)
        py = venv_python(venv_root)

    if venv_root.is_dir() and not is_isolated_venv(py, venv_root):
        print(
            "Existing layout directory is not an isolated Python venv "
            "(common after a failed Debian/Ubuntu bootstrap).",
            file=sys.stderr,
        )
        remove_venv(venv_root)

    if not venv_root.is_dir():
        venv_root.parent.mkdir(parents=True, exist_ok=True)
        create_venv(base, venv_root)
        py = venv_python(venv_root)

    if not py.is_file():
        print(f"venv exists but interpreter not found: {py}", file=sys.stderr)
        print_debian_help(venv_root)
        raise SystemExit(1)

    if not is_isolated_venv(py, venv_root):
        print(
            "Created layout venv is not isolated; Python cannot install packages into it.",
            file=sys.stderr,
        )
        print_debian_help(venv_root)
        raise SystemExit(1)

    return py


def run_pip(py: Path, args: list[str]) -> subprocess.CompletedProcess[str]:
    cmd = [str(py), "-m", "pip", *args]
    print("+", " ".join(cmd))
    return subprocess.run(cmd, text=True, capture_output=True)


def print_pip_output(proc: subprocess.CompletedProcess[str]) -> None:
    if proc.stdout:
        sys.stdout.write(proc.stdout)
    if proc.stderr:
        sys.stderr.write(proc.stderr)


def is_externally_managed_failure(proc: subprocess.CompletedProcess[str]) -> bool:
    blob = f"{proc.stdout}\n{proc.stderr}".lower()
    return "externally-managed-environment" in blob or "pep 668" in blob


def pip_install(py: Path, venv_root: Path, base: str, args: list[str]) -> None:
    """Run pip in the layout venv; recreate once if PEP 668 indicates a broken tree."""
    proc = run_pip(py, args)
    print_pip_output(proc)
    if proc.returncode == 0:
        return

    if is_externally_managed_failure(proc):
        print(
            "pip reported externally-managed-environment inside the layout venv; recreating...",
            file=sys.stderr,
        )
        remove_venv(venv_root)
        py = ensure_venv(base, venv_root)
        ensure_pip(py, venv_root)
        proc = run_pip(py, args)
        print_pip_output(proc)
        if proc.returncode == 0:
            return
        if is_externally_managed_failure(proc):
            print_debian_help(venv_root)
            raise SystemExit(proc.returncode)

    blob = f"{proc.stdout}\n{proc.stderr}".lower()
    if "requires-python" in blob or "no matching distribution found for pymupdf" in blob:
        print_python311_help(str(py), interpreter_version(py))

    print(
        f"Command failed with exit code {proc.returncode}: "
        f"{py} -m pip {' '.join(args)}",
        file=sys.stderr,
    )
    raise SystemExit(proc.returncode)


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
    env = os.environ.get("MUPDF_NET_PYTHON") or os.environ.get("MuPDF4LLM_NET_PYTHON") or os.environ.get("PYTHON")
    if env:
        candidate = Path(env)
        if candidate.is_file():
            return candidate

    if venv_root is not None:
        py = venv_python(venv_root)
        if py.is_file():
            return py

    for root in (default_venv_path(), legacy_venv_path()):
        py = venv_python(root)
        if py.is_file():
            return py

    return None


def layout_version_probe() -> str:
    # pymupdf 2.1 keeps layout inside the pymupdf wheel; version may be on
    # pymupdf.layout or on pymupdf itself.
    return (
        "import pymupdf, pymupdf.layout as L\n"
        "print(\n"
        "    getattr(L, 'version', None)\n"
        "    or getattr(L, '__version__', None)\n"
        "    or getattr(pymupdf, 'VersionBind', None)\n"
        "    or getattr(pymupdf, '__version__', '')\n"
        "    or ''\n"
        ")"
    )


def verify_layout_python(py: Path, required: str) -> int:
    ver = interpreter_version(py)
    if ver is not None and ver < MIN_PYTHON:
        print(
            f"Layout Python is {format_py_version(ver)}; "
            f"pymupdf {required} needs {MIN_PYTHON[0]}.{MIN_PYTHON[1]}+.",
            file=sys.stderr,
        )
        print_python311_help(str(py), ver)
        return 1

    try:
        out = subprocess.check_output(
            [str(py), "-c", layout_version_probe()],
            text=True,
            stderr=subprocess.STDOUT,
        ).strip()
    except FileNotFoundError:
        print(f"Layout Python interpreter not found: {py}", file=sys.stderr)
        print_setup_guide(required)
        return 1
    except subprocess.CalledProcessError as exc:
        detail = (exc.output or "").strip()
        print(f"pymupdf {required} layout is not installed.", file=sys.stderr)
        if detail:
            print(detail, file=sys.stderr)
        print_setup_guide(required)
        return 1

    if not out:
        print(
            f"pymupdf.layout imported from {py} but reported no version; "
            f"MuPDF.NET requires pymupdf {required}.",
            file=sys.stderr,
        )
        print_setup_guide(required)
        return 1

    if normalize_version(out) != normalize_version(required):
        print(
            f"pymupdf.layout {out} is installed; MuPDF.NET requires pymupdf {required}.",
            file=sys.stderr,
        )
        print_setup_guide(required)
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
        help="Python 3.11+ used to create the venv (default: python3.13/3.12/3.11 on PATH, else this interpreter)",
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
            print_setup_guide(required, venv_root)
            return 1
        print(f"Checking layout Python: {py}")
        return verify_layout_python(py, required)

    if not REQUIREMENTS.is_file():
        print(f"Missing {REQUIREMENTS}", file=sys.stderr)
        return 1

    base = find_base_python(args.base_python)
    venv_root = (args.venv or default_venv_path()).resolve()

    print(f"Using Python: {base}")
    print(f"Requirements: {REQUIREMENTS}")
    print(f"Layout venv:  {venv_root}")

    check_linux_prerequisites(base)
    py = ensure_venv(base, venv_root)
    ensure_pip(py, venv_root)
    pip_install(py, venv_root, base, ["install", "--upgrade", "pip"])
    # Re-resolve after possible recreate inside pip_install.
    py = venv_python(venv_root)
    pip_install(py, venv_root, base, ["install", "-r", str(REQUIREMENTS)])
    py = venv_python(venv_root)

    if not args.skip_verify:
        if verify_layout_python(py, required) != 0:
            return 1

    print(
        textwrap.dedent(
            f"""
            Layout Python environment ready:
              {venv_root}
              interpreter: {py}

            MuPDF.NET discovers this venv automatically when present.
            To use another interpreter, set MUPDF_NET_PYTHON:
              {py}
            """
        ).strip()
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
