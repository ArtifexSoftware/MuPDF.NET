﻿using mupdf;
using System;
using System.Collections.Generic;
using System.Formats.Tar;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MuPDF.NET
{
    public class MuPDFArchive
    {

        private FzArchive _nativeArchive;

        private List<SubArchiveStruct> _subArchives;

        public List<SubArchiveStruct> EntryList
        {
            get
            {
                return _subArchives;
            }
        }

        public MuPDFArchive(string dirName)
        {
            _subArchives = new List<SubArchiveStruct>();
            _nativeArchive = mupdf.mupdf.fz_new_multi_archive();
            Add(content: dirName, path: dirName);
        }

        public MuPDFArchive()
        {
            _nativeArchive = mupdf.mupdf.fz_new_multi_archive();
            _subArchives = new List<SubArchiveStruct>();
        }

        public MuPDFArchive(string filename, string path = null)
        {
            _nativeArchive = mupdf.mupdf.fz_new_multi_archive();
            Add(filename, path);
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
                sub = mupdf.mupdf.fz_open_zip_archive(filePath);
            else
                sub = mupdf.mupdf.fz_open_tar_archive(filePath);
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
            SubArchiveStruct subarch = new SubArchiveStruct()
            {
                Fmt = fmt, Entries = entries, Path = mount
            };

            if (fmt != "tree" || _subArchives.Count == 0)
            {
                _subArchives.Add(subarch);
            }
            else
            {
                SubArchiveStruct ltree = _subArchives[_subArchives.Count - 1];
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

        public void Add(ZipArchive content, string path = null)
        {
            string fmt = "zip";
            string filename = path == null ? "" : Path.GetFileName(path);
            List<string> entries = new List<string>();
            foreach (ZipArchiveEntry e in content.Entries)
            {
                entries.Add(e.FullName);
            }

            if (filename == "")
            {
                
            }
            else
            {
                _AddZiptarFile(filename, 1, path);
            }
            MakeSubArch(fmt, entries, path);
        }

        public void Add(TarReader content, string path = null)
        {
            string fmt = "zip";
            string filename = path == null ? "" : Path.GetFileName(path);
            List<string> entries = new List<string>();
            TarEntry? entry;
            while ((entry = content.GetNextEntry(true)) != null)
            {
                entries.Add(entry.Name);
            }

            if (filename == "")
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

            if (path == null)
                throw new Exception("Need name for binary content");
            entries.Add(path);
            _AddTreeItem(content as byte[], path);
            MakeSubArch("tree", entries, null);
        }

        public void Add(string content, string path)
        {
            string fmt = null;
            List<string> entries = new List<string>();
            string mount = null;

            if (Directory.Exists(content))
            {
                fmt = "dir";
                mount = path;
                entries = new List<string>(Directory.GetFiles(path));
                _AddDir(content, path);
                MakeSubArch(fmt, entries, mount);
            }

            if (File.Exists(Path.Combine(path, content)))
            {
                if (path == null)
                    throw new Exception("need name for binary content");
                byte[] ff = File.ReadAllBytes(content);
                fmt = "tree";
                mount = null;
                entries.Add(path);
                _AddTreeItem(ff, path);
                MakeSubArch(fmt, entries, mount);
            }
        }

        public int HasEntry(string name)
        {
            return _nativeArchive.fz_has_archive_entry(name);
        }

        public byte[] ReadEntry(string name)
        {
            FzBuffer buf = _nativeArchive.fz_read_archive_entry(name);
            return Utils.BinFromBuffer(buf);
        }
    }
}