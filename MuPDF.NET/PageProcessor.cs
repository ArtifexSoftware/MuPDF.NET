using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MuPDF.NET
{
    /// <summary>
    /// Provides concurrent page processing for MuPDF documents.
    /// Ports the functionality of PyMuPDF's _apply_pages.py using .NET parallelism.
    /// </summary>
    public static class PageProcessor
    {
        /// <summary>
        /// Process pages of an open document in parallel, applying <paramref name="pageFunction"/>
        /// to each page and returning a list of results in page order.
        /// <para>
        /// Note: The caller must ensure that <paramref name="pageFunction"/> is thread-safe.
        /// The supplied <paramref name="doc"/> is accessed from multiple threads, so the function
        /// should not mutate document state.
        /// </para>
        /// </summary>
        /// <typeparam name="T">The type of result produced for each page.</typeparam>
        /// <param name="doc">The document whose pages to process.</param>
        /// <param name="pageFunction">A function applied to each page, returning a result of type <typeparamref name="T"/>.</param>
        /// <param name="start">First page number (0-based, inclusive). Defaults to 0.</param>
        /// <param name="stop">Last page number (exclusive). Defaults to <see cref="Document.PageCount"/>.</param>
        /// <param name="step">Step between page numbers. Defaults to 1.</param>
        /// <returns>A list of results ordered by the page sequence.</returns>
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

            Parallel.For(0, pageNumbers.Count, i =>
            {
                var page = doc.LoadPage(pageNumbers[i]);
                results[i] = pageFunction(page);
            });

            return results.ToList();
        }

        /// <summary>
        /// Process pages of a document in parallel by opening the file from
        /// <paramref name="filename"/>. Each parallel worker opens its own
        /// <see cref="Document"/> instance to avoid thread-safety issues, matching
        /// the pattern used by PyMuPDF's multiprocessing pool.
        /// </summary>
        /// <typeparam name="T">The type of result produced for each page.</typeparam>
        /// <param name="filename">Path to the document file.</param>
        /// <param name="pageFunction">A function applied to each page, returning a result of type <typeparamref name="T"/>.</param>
        /// <param name="start">First page number (0-based, inclusive). Defaults to 0.</param>
        /// <param name="stop">Last page number (exclusive). Defaults to the document's page count.</param>
        /// <param name="step">Step between page numbers. Defaults to 1.</param>
        /// <returns>A list of results ordered by the page sequence.</returns>
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
                Parallel.For(0, pageNumbers.Count, i =>
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
        /// Process pages of an open document in parallel, applying <paramref name="pageAction"/>
        /// to each page without producing a return value.
        /// <para>
        /// Note: The caller must ensure that <paramref name="pageAction"/> is thread-safe.
        /// The supplied <paramref name="doc"/> is accessed from multiple threads, so the action
        /// should not mutate document state.
        /// </para>
        /// </summary>
        /// <param name="doc">The document whose pages to process.</param>
        /// <param name="pageAction">An action applied to each page.</param>
        /// <param name="start">First page number (0-based, inclusive). Defaults to 0.</param>
        /// <param name="stop">Last page number (exclusive). Defaults to <see cref="Document.PageCount"/>.</param>
        /// <param name="step">Step between page numbers. Defaults to 1.</param>
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

            Parallel.For(0, pageNumbers.Count, i =>
            {
                var page = doc.LoadPage(pageNumbers[i]);
                pageAction(page);
            });
        }

        /// <summary>
        /// Process pages of a document in parallel by opening the file from
        /// <paramref name="filename"/>. Each parallel worker opens its own
        /// <see cref="Document"/> instance to avoid thread-safety issues.
        /// No results are returned.
        /// </summary>
        /// <param name="filename">Path to the document file.</param>
        /// <param name="pageAction">An action applied to each page.</param>
        /// <param name="start">First page number (0-based, inclusive). Defaults to 0.</param>
        /// <param name="stop">Last page number (exclusive). Defaults to the document's page count.</param>
        /// <param name="step">Step between page numbers. Defaults to 1.</param>
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
                Parallel.For(0, pageNumbers.Count, i =>
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
        /// Build the list of page numbers from Python-style start/stop/step slice parameters.
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
    }
}
