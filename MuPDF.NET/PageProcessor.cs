using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MuPDF.NET
{
    /// <summary>
    /// Concurrent page processing for MuPDF documents.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>src/_apply_pages.py</c>. Public API uses .NET <see cref="ApplyPages"/> overloads with
    /// <c>ParallelRunner.For</c> instead of Python <c>multiprocessing</c> / <c>os.fork()</c>.
    /// </para>
    /// <para>
    /// Prefer the <see cref="ApplyPages(string, Func{Page, T}, int?, int?, int?)"/> overload when processing many
    /// pages in parallel: each worker thread opens its own <see cref="Document"/> (same pattern as
    /// <c>_apply_pages._worker_fn</c>). PyMuPDF documents that a shared <see cref="Document"/> must not be used
    /// across forked processes because file-descriptor offsets are shared.
    /// </para>
    /// </remarks>
    public static class PageProcessor
    {
        /// <summary>
        /// Returns a list of results from <paramref name="pageFunction"/>, one per selected page, in page order.
        /// Applies pages sequentially (<c>method='single'</c>; PyMuPDF <c>apply_pages</c>). Here uses
        /// <see cref="ParallelRunner.For"/> on the supplied open document).
        /// </summary>
        /// <typeparam name="T">Result type produced for each page.</typeparam>
        /// <param name="doc">Open document whose pages are processed.</param>
        /// <param name="pageFunction">
        /// Called for each page as <c>pageFunction(page)</c>. Must be thread-safe; must not mutate
        /// <paramref name="doc"/> if multiple threads run concurrently.
        /// </param>
        /// <param name="start">First page number (0-based, inclusive). Python slice <c>start</c>; default 0.</param>
        /// <param name="stop">Last page number (exclusive). Python slice <c>stop</c>; default page count.</param>
        /// <param name="step">Step between page numbers. Python slice <c>step</c>; default 1. Must not be zero.</param>
        /// <returns>Results in the same order as the generated page-number sequence.</returns>
        public static List<T> ApplyPages<T>(
            Document doc,
            Func<Page, T> pageFunction,
            int? start = null,
            int? stop = null,
            int? step = null)
        {
            if (doc == null) throw new ArgumentNullException(nameof(doc));
            if (pageFunction == null) throw new ArgumentNullException(nameof(pageFunction));

            var pageNumbers = BuildPageList(doc.PageCount, start, stop, step);
            var results = new T[pageNumbers.Count];

            ParallelRunner.For(0, pageNumbers.Count, i =>
            {
                var page = doc.LoadPage(pageNumbers[i]);
                results[i] = pageFunction(page);
            });

            return results.ToList();
        }

        /// <summary>
        /// Returns a list of results from <paramref name="pageFunction"/> using parallel workers that each open
        /// the file at <paramref name="filename"/> (PyMuPDF <c>apply_pages</c> with <c>method='mp'</c> /
        /// <c>'fork'</c> — one <see cref="Document"/> per worker in <c>_apply_pages._worker_fn</c>).
        /// </summary>
        /// <typeparam name="T">Result type produced for each page.</typeparam>
        /// <param name="filename">Path of the document file (PyMuPDF <c>path</c> argument).</param>
        /// <param name="pageFunction">Called for each page; must be thread-safe.</param>
        /// <param name="start">First page number (0-based, inclusive).</param>
        /// <param name="stop">Last page number (exclusive).</param>
        /// <param name="step">Step between page numbers; must not be zero.</param>
        /// <returns>Results in page-sequence order.</returns>
        public static List<T> ApplyPages<T>(
            string filename,
            Func<Page, T> pageFunction,
            int? start = null,
            int? stop = null,
            int? step = null)
        {
            if (filename == null) throw new ArgumentNullException(nameof(filename));
            if (pageFunction == null) throw new ArgumentNullException(nameof(pageFunction));

            int pageCount;
            using (var probe = new Document(filename))
                pageCount = probe.PageCount;

            var pageNumbers = BuildPageList(pageCount, start, stop, step);
            var results = new T[pageNumbers.Count];

            var threadLocalDoc = new ThreadLocal<Document>(
                () => new Document(filename), trackAllValues: true);

            try
            {
                ParallelRunner.For(0, pageNumbers.Count, i =>
                {
                    var doc = threadLocalDoc.Value;
                    var page = doc.LoadPage(pageNumbers[i]);
                    results[i] = pageFunction(page);
                });
            }
            finally
            {
                foreach (var doc in threadLocalDoc.Values)
                    doc.Dispose();
                threadLocalDoc.Dispose();
            }

            return results.ToList();
        }

        /// <summary>
        /// Applies <paramref name="pageAction"/> to each selected page of an open document (no return list).
        /// Skips pages when the callback returns <c>null</c> (PyMuPDF <c>apply_pages</c>).
        /// </summary>
        /// <param name="doc">Open document whose pages are processed.</param>
        /// <param name="pageAction">Called for each page. Must be thread-safe; avoid mutating <paramref name="doc"/>.</param>
        /// <param name="start">First page number (0-based, inclusive).</param>
        /// <param name="stop">Last page number (exclusive).</param>
        /// <param name="step">Step between page numbers; must not be zero.</param>
        public static void ApplyPages(
            Document doc,
            Action<Page> pageAction,
            int? start = null,
            int? stop = null,
            int? step = null)
        {
            if (doc == null) throw new ArgumentNullException(nameof(doc));
            if (pageAction == null) throw new ArgumentNullException(nameof(pageAction));

            var pageNumbers = BuildPageList(doc.PageCount, start, stop, step);

            ParallelRunner.For(0, pageNumbers.Count, i =>
            {
                var page = doc.LoadPage(pageNumbers[i]);
                pageAction(page);
            });
        }

        /// <summary>
        /// Applies <paramref name="pageAction"/> to each selected page, opening <paramref name="filename"/> per
        /// parallel worker thread (see file-path overload remarks on <see cref="PageProcessor"/>).
        /// </summary>
        /// <param name="filename">Path of the document file.</param>
        /// <param name="pageAction">Called for each page; must be thread-safe.</param>
        /// <param name="start">First page number (0-based, inclusive).</param>
        /// <param name="stop">Last page number (exclusive).</param>
        /// <param name="step">Step between page numbers; must not be zero.</param>
        public static void ApplyPages(
            string filename,
            Action<Page> pageAction,
            int? start = null,
            int? stop = null,
            int? step = null)
        {
            if (filename == null) throw new ArgumentNullException(nameof(filename));
            if (pageAction == null) throw new ArgumentNullException(nameof(pageAction));

            int pageCount;
            using (var probe = new Document(filename))
                pageCount = probe.PageCount;

            var pageNumbers = BuildPageList(pageCount, start, stop, step);

            var threadLocalDoc = new ThreadLocal<Document>(
                () => new Document(filename), trackAllValues: true);

            try
            {
                ParallelRunner.For(0, pageNumbers.Count, i =>
                {
                    var doc = threadLocalDoc.Value;
                    var page = doc.LoadPage(pageNumbers[i]);
                    pageAction(page);
                });
            }
            finally
            {
                foreach (var doc in threadLocalDoc.Values)
                    doc.Dispose();
                threadLocalDoc.Dispose();
            }
        }

        /// <summary>
        /// Build page numbers from Python-style <c>start</c>/<c>stop</c>/<c>step</c> slice parameters
        /// (used when <c>pages</c> is built from a range in <c>apply_pages</c>).
        /// </summary>
        private static List<int> BuildPageList(int pageCount, int? start, int? stop, int? step)
        {
            int s = start ?? 0;
            int e = stop ?? pageCount;
            int st = step ?? 1;

            if (st == 0) throw new ArgumentException("Step cannot be zero.", nameof(step));

            if (s < 0) s = Math.Max(pageCount + s, 0);
            if (e < 0) e = Math.Max(pageCount + e, 0);

            s = Math.Min(s, pageCount);
            e = Math.Min(e, pageCount);

            var pages = new List<int>();
            if (st > 0)
            {
                for (int i = s; i < e; i += st)
                    pages.Add(i);
            }
            else
            {
                for (int i = s; i > e; i += st)
                    pages.Add(i);
            }

            return pages;
        }

        // ─── PyMuPDF API names (internal, same assembly) ─────────────────

        internal static List<T> apply_pages<T>(
            string path,
            Func<Page, T> pagefn,
            int? start = null,
            int? stop = null,
            int? step = null) =>
            ApplyPages(path, pagefn, start, stop, step);

        internal static List<T> apply_pages<T>(
            Document doc,
            Func<Page, T> pagefn,
            int? start = null,
            int? stop = null,
            int? step = null) =>
            ApplyPages(doc, pagefn, start, stop, step);

        internal static void apply_pages(
            string path,
            Action<Page> pagefn,
            int? start = null,
            int? stop = null,
            int? step = null) =>
            ApplyPages(path, pagefn, start, stop, step);

        internal static void apply_pages(
            Document doc,
            Action<Page> pagefn,
            int? start = null,
            int? stop = null,
            int? step = null) =>
            ApplyPages(doc, pagefn, start, stop, step);
    }
}
