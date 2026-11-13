from pathlib import Path

NATIVE = Path(__file__).parent / "native"
OUT = Path(__file__).parent.parent / "MuPDF.NET"
PAIRS = [
    (NATIVE / "win-x64" / "mupdf.cs", OUT / "mupdf_windows.cs",
     "#if WINDOWS && (NET472 || NET48 || NET5_0_OR_GREATER)\n"),
    (NATIVE / "linux-x64" / "mupdf.cs", OUT / "mupdf_linux.cs",
     "#if !WINDOWS || NETSTANDARD2_0 || NET461\n"),
]

for src, dest, ifdef in PAIRS:
    t = src.read_text(encoding="utf-8")
    t = t.replace('DllImport("mupdfcsharp.dll"', 'DllImport("mupdfcsharp"')
    t = t.replace('DllImport("mupdfcsharp.so"', 'DllImport("mupdfcsharp"')
    needle = "\nnamespace mupdf"
    if needle not in t:
        raise SystemExit(f"namespace mupdf not found in {src}")
    if ifdef.strip() not in t:
        t = t.replace(needle, "\n" + ifdef + "namespace mupdf", 1)
    t = t.rstrip()
    if not t.endswith("#endif"):
        t += "\n#endif\n"
    else:
        t += "\n"
    dest.write_text(t, encoding="utf-8", newline="\n")
    print(f"{dest.name}: {dest.stat().st_size:,} bytes from {src.name}")
