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

        }

        public MuPDFArchive(FileInfo file, string path = null)
        {

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
                SubArchiveStruct ltree = _subArchives[-1];
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

        public void Add(dynamic content, string path)
        {
            string fmt = null;
            List<string> entries = new List<string>();
            string mount = null;

            if (content == typeof(ZipArchive))
            {
                fmt = "zip";
                ZipArchive tmp = (content as ZipArchive);
                
                foreach (ZipArchiveEntry e in tmp.Entries)
                {
                    entries.Add(e.FullName);
                }
                mount = path;
                string filename = Directory.GetParent(Directory.GetCurrentDirectory()).FullName + path;
                
                if (filename != null)
                    _AddZiptarFile(filename, 0, path); //issue
                MakeSubArch(fmt, entries, mount);
                return;
            }
            
            if (content == typeof(TarReader))
            {
                fmt = "tar";
                TarReader tmp = content as TarReader;
                TarEntry? entry;
                while ((entry = tmp.GetNextEntry(true)) != null)
                {
                    entries.Add(entry.Name);
                }
                mount = path;
                string filename = Directory.GetParent(Directory.GetCurrentDirectory()).FullName + path;

                if (filename != null)
                    _AddZiptarFile(filename, 0, path);
                MakeSubArch(fmt, entries, mount);
                return;
            }

            if (content is FzArchive)
            {
                fmt = "multi";
                mount = path;
                _AddArch(content as FzArchive, path);
                MakeSubArch(fmt, entries, mount);
                return;
            }

            if (content is byte[])
            {
                if (path == null)
                    throw new Exception("need name for binary content");
                fmt = "tree";
                mount = null;
                entries.Add(path);
                _AddTreeItem(content as byte[], path);
                MakeSubArch(fmt, entries, mount);
                return;
            }

            if (content is string && Directory.Exists(content))
            {
                fmt = "dir";
                mount = path;
                entries = new List<string>(Directory.GetFiles(path));
                _AddDir(content, path);
                MakeSubArch(fmt, entries, mount);
            }

            if (content is string && File.Exists(content))
            {
                if (path == null)
                    throw new Exception("need name for binary content");
                byte[] ff = File.ReadAllBytes(content);
                fmt = "tree";
                mount = null;
                entries.Add(path);
                _AddTreeItem(ff, path);
                MakeSubArch(fmt, entries, mount);
                return;
            }

            //issue
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