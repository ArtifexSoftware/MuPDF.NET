﻿using mupdf;
using ICSharpCode.SharpZipLib.Zip;
using ICSharpCode.SharpZipLib.Tar;
using System.Collections.Generic;
using System.IO;
using System;

namespace MuPDF.NET
{
    public class Archive
    {
        static Archive()
        {
            Utils.InitApp();
        }

        private FzArchive _nativeArchive;

        private List<SubArchive> _subArchives;

        public List<SubArchive> EntryList
        {
            get
            {
                return _subArchives;
            }
        }

        public Archive(string dirName, string path = "")
        {
            _subArchives = new List<SubArchive>();
            _nativeArchive = mupdf.mupdf.fz_new_multi_archive();
            Add(content: dirName, path: path);
        }

        public Archive()
        {
            _nativeArchive = mupdf.mupdf.fz_new_multi_archive();
            _subArchives = new List<SubArchive>();
        }

        public Archive(byte[] content, string path = "")
        {
            _subArchives = new List<SubArchive>();
            _nativeArchive = mupdf.mupdf.fz_new_multi_archive();
            Add(content: content, path: path);
        }

        public FzArchive ToFzArchive()
        {
            return _nativeArchive;
        }

        public override string ToString()
        {
            return $"Archive, sub-archive: {_subArchives.Count}";
        }

        private void _AddArch(FzArchive subArch, string path = null)
        {
            _nativeArchive.fz_mount_multi_archive(subArch, path);
        }

        private void _AddDir(string folder, string path = null)
        {
            FzArchive sub = mupdf.mupdf.fz_open_directory(folder);
            _nativeArchive.fz_mount_multi_archive(sub, path);
        }

        private void _AddTreeItem(byte[] memory, string name, string path = null)
        {
            FzBuffer buf = Utils.BufferFromBytes(memory);
            FzArchive sub = new FzTree().fz_new_tree_archive();
            sub.fz_tree_archive_add_buffer(name, buf);
            _nativeArchive.fz_mount_multi_archive(sub, path);
        }

        private void _AddZiptarFile(string filePath, int type, string path = null)
        {
            FzArchive sub = null;
            if (type == 1)
                sub = mupdf.mupdf.fz_open_zip_archive(path);
            else
                sub = mupdf.mupdf.fz_open_tar_archive(path);
            _nativeArchive.fz_mount_multi_archive(sub, path);
        }

        private void _AddZiptarMemory(byte[] memory, int type, string path = null)
        {
            FzBuffer buf = Utils.BufferFromBytes(memory);
            FzStream stream = buf.fz_open_buffer();
            FzArchive sub = null;
            if (type == 1)
                sub = stream.fz_open_zip_archive_with_stream();
            else
                sub = stream.fz_open_tar_archive_with_stream();
            _nativeArchive.fz_mount_multi_archive(sub, path);
        }

        private void MakeSubArch(string fmt, List<string> entries, string mount)
        {
            SubArchive subarch = new SubArchive()
            {
                Fmt = fmt,
                Entries = entries,
                Path = mount
            };

            if (fmt != "tree" || _subArchives.Count == 0)
            {
                _subArchives.Add(subarch);
            }
            else
            {
                SubArchive ltree = _subArchives[_subArchives.Count - 1];
                if (ltree.Fmt != "tree" || ltree.Path != subarch.Path)
                {
                    _subArchives.Add(subarch);
                }
                else
                {
                    ltree.Entries.AddRange(subarch.Entries);
                    _subArchives[_subArchives.Count - 1] = ltree;
                }
            }
        }

        /// <summary>
        /// Append a sub-archive. The meaning of the parameters are exactly the same as explained above. Of course, parameter content is not optional here.
        /// </summary>
        /// <param name="content">The fully qualified name of the entry.</param>
        /// <param name="path"></param>
        public void Add(ZipFile content, string path = null)
        {
            string fmt = "zip";
            if (!File.Exists(path))
                throw new Exception("Need name for zip file");
            string filename = Path.GetFileName(path);
            List<string> entries = new List<string>();
            foreach (ZipEntry e in content)
            {
                entries.Add(e.Name);
            }

            if (string.IsNullOrEmpty(filename))
            {

            }
            else
            {
                _AddZiptarFile(filename, 1, path);
            }
            MakeSubArch(fmt, entries, path);
        }

        public void Add(TarInputStream content, string path = null)
        {
            string fmt = "tar";
            if (!File.Exists(path))
                throw new Exception("Need name for zip file");

            string filename = Path.GetFileName(path);
            List<string> entries = new List<string>();
            TarEntry entry;
            while ((entry = content.GetNextEntry()) != null)
            {
                entries.Add(entry.Name);
            }

            if (string.IsNullOrEmpty(filename))
            {
                
            }
            else
            {
                _AddZiptarFile(filename, 0, path);
            }
            MakeSubArch(fmt, entries, path);
        }

        public void Add(FzArchive content, string path)
        {
            _AddArch(content, path);
            MakeSubArch("multi", new List<string>(), path);
        }

        public void Add(byte[] content, string path)
        {
            List<string> entries = new List<string>();

            if (string.IsNullOrEmpty(path))
                throw new Exception("Need name for binary content");
            entries.Add(path);
            _AddTreeItem(content as byte[], path);
            MakeSubArch("tree", entries, null);
        }

        public void Add(string content, string path = "")
        {
            string fmt = null;
            string mount = null;
            List<string> entries = new List<string>();

            if (Directory.Exists(content))
            {
                fmt = "dir";
                mount = path;
                entries = new List<string>(Directory.GetFiles(content));
                _AddDir(content, path);
                MakeSubArch(fmt, entries, mount);
            }
            else if (File.Exists(content))
            {
                if (string.IsNullOrEmpty(path))
                    throw new Exception("need name for binary content");
                byte[] ff = File.ReadAllBytes(content);
                fmt = "tree";
                mount = null;
                entries.Add(path);
                _AddTreeItem(ff, path);
                MakeSubArch(fmt, entries, mount);
            }
            else
                throw new Exception("Not a file or directory");
        }

        /// <summary>
        /// Checks whether an entry exists in any of the sub-archives.
        /// </summary>
        /// <param name="name">The fully qualified name of the entry.</param>
        /// <returns></returns>
        public bool HasEntry(string name)
        {
            return _nativeArchive.fz_has_archive_entry(name) != 0;
        }

        /// <summary>
        /// Retrieve the data of an entry.
        /// </summary>
        /// <param name="name">The fully qualified name of the entry.</param>
        /// <returns></returns>
        public byte[] ReadEntry(string name)
        {
            FzBuffer buf = _nativeArchive.fz_read_archive_entry(name);
            
            return Utils.BinFromBuffer(buf);
        }
    }
}
