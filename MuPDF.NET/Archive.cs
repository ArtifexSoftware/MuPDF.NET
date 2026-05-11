using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

namespace MuPDF.NET
{
    /// <summary>
    /// PyMuPDF <c>Archive</c> port: multi-archive with mounted sub-archives (dirs, tree buffers, zip/tar, nested archives).
    /// </summary>
    public class Archive : IDisposable
    {
        private mupdf.FzArchive? _nativeArchive;
        private bool _disposed;
        private readonly List<Dictionary<string, object?>> _subarchives = new List<Dictionary<string, object?>>();
        /// <summary>Holds buffers/streams that must outlive mounts (in-memory zip/tar).</summary>
        private readonly List<IDisposable> _heldDisposables = new List<IDisposable>();

        internal mupdf.FzArchive NativeArchive
        {
            get
            {
                if (_disposed) throw new ObjectDisposedException(nameof(Archive));
                return _nativeArchive!;
            }
        }

        /// <summary>Empty multi-archive (PyMuPDF <c>Archive()</c>).</summary>
        public Archive()
        {
            _nativeArchive = mupdf.mupdf.fz_new_multi_archive();
        }

        /// <summary>
        /// PyMuPDF <c>Archive(*args)</c> with one or two arguments: forwarded to <see cref="Add(object, string)"/>.
        /// </summary>
        public Archive(object first, string? second = null)
            : this()
        {
            if (second != null)
                Add(first, second);
            else
                Add(first);
        }

        /// <summary>
        /// <c>Archive(dirname [, mountPath])</c> / <c>Archive(file, mountPath)</c> — same as <see cref="Add(string, string)"/> after construction.
        /// </summary>
        public Archive(string path, string? mountPath = null)
            : this()
        {
            Add(path, mountPath);
        }

        /// <summary><c>Archive(data, name)</c> — buffer as tree entry (PyMuPDF <c>add(data, name)</c>).</summary>
        public Archive(byte[] data, string name)
            : this()
        {
            Add(data, name, null);
        }

        /// <summary><c>Archive(archive [, mountPath])</c> — mount nested archive.</summary>
        public Archive(Archive other, string? mountPath = null)
            : this()
        {
            if (other == null) throw new ArgumentNullException(nameof(other));
            Add(other, mountPath);
        }

        internal Archive(mupdf.FzArchive archive)
        {
            _nativeArchive = archive;
        }

        /// <summary>Number of recorded sub-archive metadata entries (PyMuPDF <c>len(entry_list)</c>).</summary>
        public int EntryCount => _subarchives.Count;

        /// <summary>PyMuPDF <c>entry_list</c>: dicts with <c>fmt</c>, <c>entries</c>, <c>path</c>.</summary>
        public List<Dictionary<string, object?>> EntryList => _subarchives;

        private static string? NormalizeMountPath(string? path)
        {
            if (string.IsNullOrEmpty(path)) return null;
            return path;
        }

        /// <summary>
        /// MuPDF takes ownership of the native <c>fz_tree</c> in <see cref="mupdf.FzTree.fz_new_tree_archive"/>;
        /// detach the managed wrapper so it does not <c>fz_drop_tree</c> on dispose/GC.
        /// </summary>
        private static void ReleaseFzTreeOwnedByArchive(mupdf.FzTree tree)
        {
            if (tree == null) return;
            const BindingFlags bf = BindingFlags.Instance | BindingFlags.NonPublic;
            var type = typeof(mupdf.FzTree);
            var ownField = type.GetField("swigCMemOwn", bf);
            var ptrField = type.GetField("swigCPtr", bf);
            ownField?.SetValue(tree, false);
            ptrField?.SetValue(tree, new HandleRef(null, IntPtr.Zero));
        }

        private void MountSubArchive(mupdf.FzArchive sub, string? mountPath)
        {
            NativeArchive.fz_mount_multi_archive(sub, NormalizeMountPath(mountPath));
        }

        private void AddDirInternal(string folder, string? mountPath)
        {
            var sub = mupdf.mupdf.fz_open_directory(folder);
            MountSubArchive(sub, mountPath);
        }

        private void AddTreeItemInternal(byte[] memory, string name, string? mountPath)
        {
            var buf = Helpers.BufferFromBytes(memory);
            var tree = new mupdf.FzTree();
            var sub = tree.fz_new_tree_archive();
            ReleaseFzTreeOwnedByArchive(tree);
            sub.fz_tree_archive_add_buffer(name, buf);
            buf.Dispose();
            MountSubArchive(sub, mountPath);
        }

        private static bool ArchivePathsEqual(string? a, string? b) =>
            (a == null && b == null) || (a != null && b != null && string.Equals(a, b, StringComparison.Ordinal));

        private void RecordSubarch(string fmt, List<string> entries, string? path)
        {
            var subarch = new Dictionary<string, object?>
            {
                ["fmt"] = fmt,
                ["entries"] = entries,
                ["path"] = path,
            };
            if (fmt == "tree" && _subarchives.Count > 0)
            {
                var last = _subarchives[_subarchives.Count - 1];
                last.TryGetValue("path", out var lpObj);
                var lp = lpObj as string;
                if (last.TryGetValue("fmt", out var lf) && lf is string lfStr && lfStr == "tree"
                    && ArchivePathsEqual(lp, path))
                {
                    if (last["entries"] is List<string> el)
                        el.AddRange(entries);
                    return;
                }
            }
            _subarchives.Add(subarch);
        }

        /// <summary>
        /// Add a directory (mount path optional), a file as tree data (requires <paramref name="mountPath"/> non-empty),
        /// or forward to other <see cref="Add"/> overloads via <see cref="Add(object, string)"/>.
        /// </summary>
        public void Add(string path, string? mountPath = null)
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentException("path required", nameof(path));
            var full = Path.GetFullPath(path);
            if (Directory.Exists(full))
            {
                AddDirInternal(full, mountPath);
                var dirNames = new List<string>();
                foreach (var e in Directory.GetFileSystemEntries(full))
                {
                    var n = Path.GetFileName(e);
                    if (!string.IsNullOrEmpty(n)) dirNames.Add(n);
                }
                RecordSubarch("dir", dirNames, mountPath);
                return;
            }
            if (File.Exists(full))
            {
                if (string.IsNullOrEmpty(mountPath))
                    throw new ArgumentException("Need mountPath (virtual name) for file content.", nameof(mountPath));
                var bytes = File.ReadAllBytes(full);
                AddTreeItemInternal(bytes, mountPath, null);
                RecordSubarch("tree", new List<string> { mountPath }, null);
                return;
            }
            throw new ArgumentException($"Not a file or directory: '{path}'", nameof(path));
        }

        /// <summary>Add in-memory item as tree buffer (PyMuPDF bytes path; <paramref name="name"/> is entry name).</summary>
        public void Add(byte[] data, string name, string? mountPath = null)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("Need name for binary content.", nameof(name));
            AddTreeItemInternal(data, name, mountPath);
            RecordSubarch("tree", new List<string> { name }, mountPath);
        }

        /// <summary>Mount another multi-archive (PyMuPDF nested <c>Archive</c>).</summary>
        public void Add(Archive other, string? mountPath = null)
        {
            if (other == null) throw new ArgumentNullException(nameof(other));
            MountSubArchive(other.NativeArchive, mountPath);
            RecordSubarch("multi", new List<string>(), mountPath);
        }

        /// <summary>Mount a zip file from disk (<c>type_ == 1</c> in PyMuPDF).</summary>
        public void AddZipFile(string zipPath, string? mountPath = null)
        {
            if (string.IsNullOrEmpty(zipPath) || !File.Exists(zipPath))
                throw new ArgumentException("zip path must exist", nameof(zipPath));
            var sub = mupdf.mupdf.fz_open_zip_archive(Path.GetFullPath(zipPath));
            MountSubArchive(sub, mountPath);
            var names = ListZipEntryNames(File.ReadAllBytes(Path.GetFullPath(zipPath)));
            RecordSubarch("zip", names, mountPath);
        }

        /// <summary>Mount a tar file from disk.</summary>
        public void AddTarFile(string tarPath, string? mountPath = null)
        {
            if (string.IsNullOrEmpty(tarPath) || !File.Exists(tarPath))
                throw new ArgumentException("tar path must exist", nameof(tarPath));
            var sub = mupdf.mupdf.fz_open_tar_archive(Path.GetFullPath(tarPath));
            MountSubArchive(sub, mountPath);
            var names = ListTarEntryNames(File.ReadAllBytes(tarPath));
            RecordSubarch("tar", names, mountPath);
        }

        /// <summary>Mount zip from raw bytes (in-memory).</summary>
        public void AddZipBytes(byte[] data, string? mountPath = null)
        {
            if (data == null || data.Length == 0) throw new ArgumentException("data required", nameof(data));
            var buf = Helpers.BufferFromBytes(data);
            var stm = buf.fz_open_buffer();
            _heldDisposables.Add(buf);
            _heldDisposables.Add(stm);
            var sub = stm.fz_open_zip_archive_with_stream();
            MountSubArchive(sub, mountPath);
            var names = ListZipEntryNames(data);
            RecordSubarch("zip", names, mountPath);
        }

        /// <summary>Mount tar from raw bytes.</summary>
        public void AddTarBytes(byte[] data, string? mountPath = null)
        {
            if (data == null || data.Length == 0) throw new ArgumentException("data required", nameof(data));
            var buf = Helpers.BufferFromBytes(data);
            var stm = buf.fz_open_buffer();
            _heldDisposables.Add(buf);
            _heldDisposables.Add(stm);
            var sub = stm.fz_open_tar_archive_with_stream();
            MountSubArchive(sub, mountPath);
            RecordSubarch("tar", ListTarEntryNames(data), mountPath);
        }

        /// <summary>
        /// PyMuPDF-style heterogeneous add: string path, raw bytes, <c>Tuple&lt;byte[], string&gt;</c>, file path tuple,
        /// nested <see cref="Archive"/>, <see cref="MemoryStream"/>, or sequence of items.
        /// </summary>
        public void Add(object content, string? mountPath = null)
        {
            switch (content)
            {
                case null:
                    throw new ArgumentNullException(nameof(content));
                case string s:
                    Add(s, mountPath);
                    return;
                case byte[] bytes:
                    if (string.IsNullOrEmpty(mountPath))
                        throw new ArgumentException("Need mountPath (virtual name) for raw bytes.", nameof(mountPath));
                    Add(bytes, mountPath, null);
                    return;
                case MemoryStream ms:
                    if (string.IsNullOrEmpty(mountPath))
                        throw new ArgumentException("Need mountPath (virtual name) for stream content.", nameof(mountPath));
                    Add(ms.ToArray(), mountPath, null);
                    return;
                case Archive arch:
                    Add(arch, mountPath);
                    return;
                case Tuple<byte[], string> t:
                    Add(t.Item1, t.Item2, mountPath);
                    return;
                case Tuple<string, string> pathAndName:
                {
                    var fp = Path.GetFullPath(pathAndName.Item1);
                    if (!File.Exists(fp))
                        throw new ArgumentException($"Not a file: '{pathAndName.Item1}'", nameof(content));
                    AddTreeItemInternal(File.ReadAllBytes(fp), pathAndName.Item2, mountPath);
                    RecordSubarch("tree", new List<string> { pathAndName.Item2 }, mountPath);
                    return;
                }
                case System.Collections.IEnumerable seq when content is not string:
                    foreach (var item in seq)
                        Add(item, mountPath);
                    return;
                default:
                    throw new ArgumentException($"Unrecognised type for Archive.Add: {content.GetType().Name}", nameof(content));
            }
        }

        /// <summary>PyMuPDF <c>read_entry</c>.</summary>
        public byte[] Read(string name) => ReadEntry(name);

        /// <summary>PyMuPDF <c>read_entry</c>.</summary>
        public byte[] ReadEntry(string name)
        {
            var buf = NativeArchive.fz_read_archive_entry(name);
            try
            {
                return Helpers.BufferToBytes(buf);
            }
            finally
            {
                buf?.Dispose();
            }
        }

        /// <summary>PyMuPDF <c>has_entry</c>.</summary>
        public bool Has(string name) => HasEntry(name);

        /// <summary>PyMuPDF <c>has_entry</c>.</summary>
        public bool HasEntry(string name) => NativeArchive.fz_has_archive_entry(name) != 0;

        public void Dispose()
        {
            if (!_disposed)
            {
                try
                {
                    _nativeArchive?.Dispose();
                }
                finally
                {
                    for (int i = _heldDisposables.Count - 1; i >= 0; i--)
                    {
                        try { _heldDisposables[i]?.Dispose(); } catch { /* ignore */ }
                    }
                    _heldDisposables.Clear();
                    _nativeArchive = null;
                    _disposed = true;
                }
            }
            GC.SuppressFinalize(this);
        }

        ~Archive() { Dispose(); }

        public override string ToString() => $"Archive, sub-archives: {EntryCount}";

        /// <summary>
        /// Central-directory scan (no <see cref="System.IO.Compression.ZipArchive"/>), so net472/net48 avoid
        /// <c>ZipArchiveMode</c> / extra reference assembly issues; matches typical <c>namelist()</c> output.
        /// </summary>
        private static List<string> ListZipEntryNames(byte[] zip)
        {
            var names = new List<string>();
            if (zip == null || zip.Length < 22)
                return names;
            const uint sigEocd = 0x06054b50u;
            const uint sigCd = 0x02014b50u;
            int eocd = -1;
            int scanMin = Math.Max(0, zip.Length - 65557);
            for (int i = zip.Length - 22; i >= scanMin; i--)
            {
                if (BitConverter.ToUInt32(zip, i) == sigEocd)
                {
                    eocd = i;
                    break;
                }
            }
            if (eocd < 0)
                return names;
            uint cdOffset = BitConverter.ToUInt32(zip, eocd + 16);
            if (cdOffset == 0xffffffffu || cdOffset >= zip.Length)
                return names;
            int p = (int)cdOffset;
            Encoding cp437;
            try
            {
                cp437 = Encoding.GetEncoding(437);
            }
            catch (ArgumentException)
            {
                cp437 = Encoding.UTF8;
            }
            while (p + 46 <= zip.Length && BitConverter.ToUInt32(zip, p) == sigCd)
            {
                int n = BitConverter.ToUInt16(zip, p + 28);
                int m = BitConverter.ToUInt16(zip, p + 30);
                int k = BitConverter.ToUInt16(zip, p + 32);
                if (p + 46 + n > zip.Length || n < 0 || m < 0 || k < 0)
                    break;
                if (n > 0)
                {
                    int gp = BitConverter.ToUInt16(zip, p + 8);
                    var enc = (gp & 0x800) != 0 ? Encoding.UTF8 : cp437;
                    var name = enc.GetString(zip, p + 46, n);
                    names.Add(name.Replace('\\', '/'));
                }
                p += 46 + n + m + k;
            }
            return names;
        }

        /// <summary>Best-effort USTAR/GNU tar name list (PyMuPDF <c>getnames()</c>).</summary>
        private static List<string> ListTarEntryNames(byte[] data)
        {
            var names = new List<string>();
            if (data == null || data.Length < 512)
                return names;
            var enc = Encoding.ASCII;
            int pos = 0;
            while (pos + 512 <= data.Length)
            {
                if (IsTarZeroBlock(data, pos))
                {
                    pos += 512;
                    if (pos + 512 <= data.Length && IsTarZeroBlock(data, pos))
                        break;
                    continue;
                }

                string name = enc.GetString(data, pos, 100).TrimEnd('\0', ' ');
                string prefix = enc.GetString(data, pos + 345, 155).TrimEnd('\0', ' ');
                long size = ParseTarOctal(data, pos + 124, 12);
                if (size < 0) size = 0;

                if (!string.IsNullOrEmpty(name) && name != "././@LongLink")
                {
                    string full = string.IsNullOrEmpty(prefix)
                        ? name
                        : prefix.TrimEnd('/') + "/" + name.TrimStart('/');
                    names.Add(full.Replace('\\', '/'));
                }

                int padded = (int)(((size + 511) / 512) * 512);
                pos += 512 + padded;
                if (padded < 0 || pos > data.Length)
                    break;
            }
            return names;
        }

        private static bool IsTarZeroBlock(byte[] d, int o)
        {
            for (int i = 0; i < 512; i++)
                if (d[o + i] != 0) return false;
            return true;
        }

        private static long ParseTarOctal(byte[] b, int o, int len)
        {
            long v = 0;
            int end = Math.Min(o + len, b.Length);
            for (int i = o; i < end; i++)
            {
                byte c = b[i];
                if (c == 0 || c == (byte)' ') continue;
                if (c < (byte)'0' || c > (byte)'7') break;
                v = (v << 3) + (c - (byte)'0');
            }
            return v;
        }
    }
}
