using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

namespace MuPDF.NET
{
    /// <summary>
    /// Multi-archive container for locating resources used by <see cref="Story"/> and related APIs.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Ports An archive combines multiple sub-archives (folders,
    /// zip/tar files, in-memory blobs, nested archives) into one searchable namespace for HTML
    /// and CSS assets.
    /// </para>
    /// <para>
    /// Legacy MuPDF.NET constructors (<c>Archive(string)</c>, <c>Archive(byte[], string)</c>) are
    /// on the <c>Archive.Legacy.cs</c> partial. See
    /// <see href="https://mupdfnet.readthedocs.io/en/latest/classes/Archive.html"/>.
    /// </para>
    /// </remarks>
    public partial class Archive : IDisposable
    {
        private readonly List<SubArchive> _subarchives = new List<SubArchive>();

        private mupdf.FzArchive? _this;
        private bool _disposed;

        /// <summary>Holds buffers/streams that must outlive mounts (in-memory zip/tar).</summary>
        private readonly List<IDisposable> _heldDisposables = new List<IDisposable>();

        internal mupdf.FzArchive NativeArchive
        {
            get
            {
                if (_disposed) throw new ObjectDisposedException(nameof(Archive));
                return _this!;
            }
        }

        /// <summary>
        /// Create an archive and optionally populate it from constructor arguments.
        /// </summary>
        /// <param name="args">
        /// <list type="bullet">
        /// <item><description>No arguments: empty archive.</description></item>
        /// <item><description>One argument: passed to <see cref="Add(object, string)"/>.</description></item>
        /// <item><description>Two arguments: <c>(content, path)</c> mount path.</description></item>
        /// </list>
        /// Supported <paramref name="args"/> types match <see cref="Add"/> (directory, file path,
        /// bytes, zip/tar, nested <see cref="Archive"/>, <c>(data, name)</c> tuple).
        /// </param>
        public Archive(params object[] args)
        {
            _this = mupdf.mupdf.fz_new_multi_archive();
            if (args != null && args.Length > 0)
                AddFromConstructorArgs(args);
        }

        internal Archive(mupdf.FzArchive archive)
        {
            _this = archive;
        }

        /// <summary>
        /// Access the underlying MuPDF <see cref="mupdf.FzArchive"/> multi-archive handle.
        /// </summary>
        /// <returns>Native archive used by <see cref="Story"/> and MuPDF APIs.</returns>
        public mupdf.FzArchive ToFzArchive() => NativeArchive;

        /// <summary>Returns a short debug description of this archive.</summary>
        public override string ToString() => $"Archive, sub-archives: {_subarchives.Count}";

        private void _add_arch(mupdf.FzArchive subarch, string? path = null)
        {
            mupdf.mupdf.fz_mount_multi_archive(_this!, subarch, path);
        }

        private void _add_arch(Archive subarch, string? path = null)
        {
            _add_arch(subarch.NativeArchive, path);
        }

        private void _add_dir(string folder, string? path = null)
        {
            var sub = mupdf.mupdf.fz_open_directory(folder);
            mupdf.mupdf.fz_mount_multi_archive(_this!, sub, path);
        }

        private void _add_treeitem(byte[] memory, string name, string? path = null)
        {
            var buff = Helpers.BufferFromBytes(memory);
            var tree = new mupdf.FzTree();
            var sub = mupdf.mupdf.fz_new_tree_archive(tree);
            ReleaseFzTreeOwnedByArchive(tree);
            mupdf.mupdf.fz_tree_archive_add_buffer(sub, name, buff);
            buff.Dispose();
            mupdf.mupdf.fz_mount_multi_archive(_this!, sub, path);
        }

        private void _add_ziptarfile(string filepath, int type_, string? path = null)
        {
            mupdf.FzArchive sub;
            if (type_ == 1)
                sub = mupdf.mupdf.fz_open_zip_archive(filepath);
            else
                sub = mupdf.mupdf.fz_open_tar_archive(filepath);
            mupdf.mupdf.fz_mount_multi_archive(_this!, sub, path);
        }

        private void _add_ziptarmemory(byte[] memory, int type_, string? path = null)
        {
            var buff = Helpers.BufferFromBytes(memory);
            var stream = buff.fz_open_buffer();
            _heldDisposables.Add(buff);
            _heldDisposables.Add(stream);
            mupdf.FzArchive sub;
            if (type_ == 1)
                sub = stream.fz_open_zip_archive_with_stream();
            else
                sub = stream.fz_open_tar_archive_with_stream();
            mupdf.mupdf.fz_mount_multi_archive(_this!, sub, path);
        }

        /// <summary>MuPDF takes ownership of the native <c>fz_tree</c>; detach managed wrapper.</summary>
        private static void ReleaseFzTreeOwnedByArchive(mupdf.FzTree tree)
        {
            if (tree == null) return;
            const BindingFlags bf = BindingFlags.Instance | BindingFlags.NonPublic;
            var type = typeof(mupdf.FzTree);
            type.GetField("swigCMemOwn", bf)?.SetValue(tree, false);
            type.GetField("swigCPtr", bf)?.SetValue(tree, new HandleRef(null, IntPtr.Zero));
        }

        /// <summary>
        /// Append a sub-archive to this archive.
        /// </summary>
        /// <param name="content">
        /// Content to mount: directory or file path (<see cref="string"/>); raw bytes
        /// (<see cref="byte"/>[], <see cref="MemoryStream"/>); <see cref="ZipArchive"/>; Python-style
        /// tar objects; nested <see cref="Archive"/>; <c>(data, name)</c> tuple; or a sequence of
        /// these (not a length-2 tuple).
        /// </param>
        /// <param name="path">
        /// Virtual mount path when the same entry name may appear in multiple sub-archives.
        /// Required when <paramref name="content"/> is binary data or a file mounted as a single
        /// tree item (then <paramref name="path"/> is also the entry name).
        /// </param>
        /// <exception cref="ValueErrorException">Invalid path, missing name for binary data, or unknown content.</exception>
        /// <exception cref="ArgumentException">Unrecognised <paramref name="content"/> type.</exception>
        /// <remarks>
        /// Updates <see cref="EntryList"/> with a new <see cref="SubArchive"/> descriptor for each mount.
        /// </remarks>
        public void Add(object content, string? path = null)
        {
            bool IsBinaryData(object x) =>
                x is byte[] or MemoryStream;

            void MakeSubarch(List<string> entries, string? mount, string fmt)
            {
                var subarch = new SubArchive
                {
                    Fmt = fmt,
                    Entries = entries,
                    Path = mount,
                };
                if (fmt != "tree" || _subarchives.Count == 0)
                {
                    _subarchives.Add(subarch);
                }
                else
                {
                    var ltree = _subarchives[_subarchives.Count - 1];
                    if (ltree.Fmt != "tree" || !ArchivePathsEqual(ltree.Path, subarch.Path))
                    {
                        _subarchives.Add(subarch);
                    }
                    else
                    {
                        ltree.Entries.AddRange(subarch.Entries);
                    }
                }
            }

            if (content is global::System.IO.FileInfo fi)
                content = fi.FullName;
            else if (content is DirectoryInfo di)
                content = di.FullName;

            if (content is string strContent)
            {
                if (Directory.Exists(strContent))
                {
                    _add_dir(strContent, path);
                    MakeSubarch(Directory.GetFileSystemEntries(strContent).Select(Path.GetFileName).Where(n => n != null).Cast<string>().ToList(), path, "dir");
                    return;
                }
                else if (File.Exists(strContent))
                {
                    if (string.IsNullOrEmpty(path))
                        throw new ValueErrorException($"Need name for binary content, but path={path}.");
                    byte[] ff = File.ReadAllBytes(strContent);
                    _add_treeitem(ff, path);
                    MakeSubarch(new List<string> { path }, null, "tree");
                    return;
                }
                else
                    throw new ValueErrorException($"Not a file or directory: '{strContent}'");
            }

            else if (IsBinaryData(content))
            {
                byte[] bin = content switch
                {
                    byte[] b => b,
                    MemoryStream ms => ms.ToArray(),
                    _ => throw new ArgumentException($"Unrecognised type {content.GetType()}.")
                };
                if (string.IsNullOrEmpty(path))
                    throw new ValueErrorException($"Need name for binary content, but path={path}.");
                _add_treeitem(bin, path);
                MakeSubarch(new List<string> { path }, null, "tree");
                return;
            }

            else if (TryAsZipFile(content, out List<string> zipNames, out byte[]? zipMem, out string? zipFilename))
            {
                if (zipFilename == null)
                    _add_ziptarmemory(zipMem!, 1, path);
                else
                    _add_ziptarfile(zipFilename, 1, path);
                MakeSubarch(zipNames, path, "zip");
                return;
            }

            else if (TryAsTarFile(content, out List<string> tarNames, out byte[]? tarMem, out string? tarFilename))
            {
                if (tarFilename == null)
                    _add_ziptarmemory(tarMem!, 0, path);
                else
                    _add_ziptarfile(tarFilename, 0, path);
                MakeSubarch(tarNames, path, "tar");
                return;
            }

            else if (TryAsSharpZipLibZipFile(content, out List<string> sharpZipNames))
            {
                if (string.IsNullOrEmpty(path) || !File.Exists(path))
                    throw new ValueErrorException($"Need existing zip file path, but path={path}.");
                _add_ziptarfile(path, 1, path);
                MakeSubarch(sharpZipNames, path, "zip");
                return;
            }

            else if (content is Archive archContent)
            {
                _add_arch(archContent, path);
                MakeSubarch(new List<string>(), path, "multi");
                return;
            }

            if (TryGetTuplePair(content, out object? data, out string? name))
            {
                if (name == null)
                    throw new ValueErrorException($"Unexpected type(name)={data?.GetType()}.");
                if (IsBinaryData(data!))
                {
                    byte[] binData = data! switch
                    {
                        byte[] b => b,
                        MemoryStream ms => ms.ToArray(),
                        _ => throw new InvalidOperationException($"Unexpected {data!.GetType()}.")
                    };
                    _add_treeitem(binData, name, path);
                }
                else if (data is string dataStr)
                {
                    if (File.Exists(dataStr))
                    {
                        byte[] ff = File.ReadAllBytes(dataStr);
                        _add_treeitem(ff, name, path);
                    }
                }
                else
                {
                    throw new InvalidOperationException($"Unexpected type(data)={data!.GetType()}.");
                }
                MakeSubarch(new List<string> { name }, path, "tree");
                return;
            }

            else if (content is IEnumerable seq && content is not string)
            {
                foreach (object item in seq)
                    Add(item, path);
                return;
            }

            else
                throw new ArgumentException($"Unrecognised type {content.GetType()}.");
        }

        private void AddFromConstructorArgs(object[] args)
        {
            if (args.Length == 1)
                Add(args[0], null);
            else if (args.Length == 2)
                Add(args[0], args[1] as string);
            else
                throw new ArgumentException("Archive() takes at most 2 arguments.");
        }

        /// <summary>
        /// List of sub-archive descriptors accumulated by <see cref="Add"/>.
        /// </summary>
        /// <remarks>
        /// Legacy MuPDF.NET exposed this as <see cref="System.Collections.Generic.List{SubArchive}"/>.
        /// Each <see cref="SubArchive"/> records <see cref="SubArchive.Fmt"/>,
        /// <see cref="SubArchive.Entries"/>, and optional <see cref="SubArchive.Path"/>.
        /// </remarks>
        public List<SubArchive> EntryList => _subarchives;

        /// <summary>Number of sub-archive descriptors in <see cref="EntryList"/>.</summary>
        public int EntryCount => _subarchives.Count;

        /// <summary>
        /// Check whether a named entry exists in any mounted sub-archive.
        /// </summary>
        /// <param name="name">Fully qualified archive entry path.</param>
        /// <returns><see langword="true"/> if MuPDF can resolve the entry.</returns>
        public bool HasEntry(string name) => mupdf.mupdf.fz_has_archive_entry(_this!, name) != 0;

        /// <summary>
        /// Read the uncompressed bytes of an archive entry.
        /// </summary>
        /// <param name="name">Fully qualified entry path.</param>
        /// <returns>Entry content as a byte array.</returns>
        /// <exception cref="Exception">MuPDF may throw if the entry does not exist.</exception>
        public byte[] ReadEntry(string name)
        {
            var buff = mupdf.mupdf.fz_read_archive_entry(_this!, name);
            try
            {
                return Helpers.BinFromBuffer(buff);
            }
            finally
            {
                buff?.Dispose();
            }
        }

        /// <summary>Release native archive resources and held buffers/streams.</summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                try { _this?.Dispose(); }
                finally
                {
                    for (int i = _heldDisposables.Count - 1; i >= 0; i--)
                    {
                        try { _heldDisposables[i]?.Dispose(); } catch { }
                    }
                    _heldDisposables.Clear();
                    _this = null;
                    _disposed = true;
                }
            }
            GC.SuppressFinalize(this);
        }

        ~Archive() { Dispose(); }

        // ─── MuPDF API names (internal, same assembly) ─────────────────

        internal void add(object content, string? path = null) => Add(content, path);
        internal List<Dictionary<string, object?>> entry_list
        {
            get
            {
                var list = new List<Dictionary<string, object?>>(_subarchives.Count);
                foreach (var sa in _subarchives)
                {
                    list.Add(new Dictionary<string, object?>
                    {
                        ["fmt"] = sa.Fmt,
                        ["entries"] = sa.Entries,
                        ["path"] = sa.Path,
                    });
                }
                return list;
            }
        }
        internal bool has_entry(string name) => HasEntry(name);
        internal byte[] read_entry(string name) => ReadEntry(name);

        private static bool TryGetTuplePair(object content, out object? data, out string? name)
        {
            data = null;
            name = null;
            if (content is Tuple<object, object> t)
            {
                data = t.Item1;
                name = t.Item2?.ToString();
                return true;
            }
            if (content is ValueTuple<object, object> vt)
            {
                data = vt.Item1;
                name = vt.Item2?.ToString();
                return true;
            }
            if (content is Tuple<byte[], string> tbs)
            {
                data = tbs.Item1;
                name = tbs.Item2;
                return true;
            }
            if (content is Tuple<string, string> tss)
            {
                data = tss.Item1;
                name = tss.Item2;
                return true;
            }
            return false;
        }

        private static bool ArchivePathsEqual(string? a, string? b) =>
            (a == null && b == null) || (a != null && b != null && string.Equals(a, b, StringComparison.Ordinal));

        private static bool TryAsSharpZipLibZipFile(object content, out List<string> names)
        {
            names = new List<string>();
            if (content == null)
                return false;
            Type t = content.GetType();
            if (t.FullName != "ICSharpCode.SharpZipLib.Zip.ZipFile")
                return false;
            if (content is not IEnumerable enumerable)
                return false;
            foreach (object? item in enumerable)
            {
                if (item == null)
                    continue;
                string? name = item.GetType().GetProperty("Name")?.GetValue(item) as string;
                if (!string.IsNullOrEmpty(name))
                    names.Add(name.Replace('\\', '/'));
            }
            return true;
        }

        private static bool TryAsZipFile(object content, out List<string> names, out byte[]? memory, out string? filename)
        {
            names = new List<string>();
            memory = null;
            filename = null;
            if (content is ZipArchive za)
            {
                foreach (var e in za.Entries)
                    names.Add(e.FullName.Replace('\\', '/'));
                if (TryGetZipArchivePath(za, out var zipPath))
                    filename = zipPath;
                else if (TryGetZipArchiveBytes(za, out var bytes))
                    memory = bytes;
                return true;
            }
            return false;
        }

        private static bool TryGetZipArchiveBytes(ZipArchive za, out byte[] bytes)
        {
            bytes = Array.Empty<byte>();
            try
            {
                var field = typeof(ZipArchive).GetField("_archiveStream", BindingFlags.Instance | BindingFlags.NonPublic)
                    ?? typeof(ZipArchive).GetField("archiveStream", BindingFlags.Instance | BindingFlags.NonPublic);
                if (field?.GetValue(za) is Stream s && s is MemoryStream ms)
                {
                    bytes = ms.ToArray();
                    return bytes.Length > 0;
                }
            }
            catch { }
            return false;
        }

        private static bool TryGetZipArchivePath(ZipArchive za, out string? path)
        {
            path = null;
            try
            {
                var field = typeof(ZipArchive).GetField("_archiveStream", BindingFlags.Instance | BindingFlags.NonPublic)
                    ?? typeof(ZipArchive).GetField("archiveStream", BindingFlags.Instance | BindingFlags.NonPublic);
                if (field?.GetValue(za) is FileStream fs && !string.IsNullOrEmpty(fs.Name))
                {
                    path = fs.Name;
                    return true;
                }
            }
            catch { }
            return false;
        }

        private static bool TryAsTarFile(object content, out List<string> names, out byte[]? memory, out string? filename)
        {
            names = new List<string>();
            memory = null;
            filename = null;
            Type t = content.GetType();
            if (t.FullName != "tarfile.TarFile" && !t.Name.Contains("TarFile"))
                return false;
            try
            {
                MethodInfo? getNames = t.GetMethod("getnames", Type.EmptyTypes);
                if (getNames != null)
                    names = ((IEnumerable)getNames.Invoke(content, null)!).Cast<string>().ToList();
                object? fileobj = t.GetProperty("fileobj")?.GetValue(content) ?? t.GetField("fileobj")?.GetValue(content);
                if (fileobj != null)
                {
                    string? fn = fileobj.GetType().GetProperty("name")?.GetValue(fileobj) as string
                        ?? fileobj.GetType().GetField("name")?.GetValue(fileobj) as string;
                    if (!string.IsNullOrEmpty(fn) && File.Exists(fn))
                        filename = fn;
                    else if (fileobj is MemoryStream ms)
                        memory = ms.ToArray();
                    else
                    {
                        var getvalue = fileobj.GetType().GetMethod("getvalue");
                        if (getvalue != null)
                            memory = (byte[])getvalue.Invoke(fileobj, null)!;
                        else
                        {
                            var inner = fileobj.GetType().GetProperty("fileobj")?.GetValue(fileobj)
                                ?? fileobj.GetType().GetField("fileobj")?.GetValue(fileobj);
                            if (inner is MemoryStream ims)
                                memory = ims.ToArray();
                        }
                    }
                }
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}