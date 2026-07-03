using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MuPDF.NET;
using Xunit;

namespace MuPDF.NET.Test
{
    [Collection("MuPDF.NET native")]
    public class TableTest
    {
        private const string TestClassName = nameof(TableTest);
        private static string Doc(string fileName) => _Path.ForTestClass(fileName, TestClassName);
        private static string Out(string fileName) => _Path.ForOutput(fileName, TestClassName);

        /// <summary>
        /// Table test based on Demo Program.TestTable():
        /// Loads err_table.pdf, gets tables with lines_strict/lines/text strategies,
        /// asserts Extract() and ToMarkdown() work for any tables found.
        /// </summary>
        [Fact]
        public void TestTable()
        {
            string testFilePath = Doc("err_table.pdf");
            Assert.True(File.Exists(testFilePath), $"Test file not found: {testFilePath}");

            Document doc = new Document(testFilePath);
            try
            {
                Assert.True(doc.PageCount >= 1);

                Page page = doc[0];

                // Test 1: Get tables with 'lines_strict' strategy (as in Demo)
                List<Table> tables = Utils.GetTables(
                    page,
                    clip: page.Rect,
                    vertical_strategy: "lines_strict",
                    horizontal_strategy: "lines_strict");

                Assert.NotNull(tables);

                if (tables.Count == 0)
                {
                    // Test 2: Fallback with 'lines' strategy (as in Demo)
                    tables = Utils.GetTables(
                        page,
                        clip: page.Rect,
                        vertical_strategy: "lines",
                        horizontal_strategy: "lines");
                }

                // Test 3: Get tables with 'text' strategy (as in Demo)
                List<Table> textTables = Utils.GetTables(
                    page,
                    clip: page.Rect,
                    vertical_strategy: "text",
                    horizontal_strategy: "text");

                Assert.NotNull(textTables);

                for (int i = 0; i < tables.Count; i++)
                {
                    Table table = tables[i];
                    Assert.True(table.row_count >= 0);
                    Assert.True(table.col_count >= 0);

                    List<List<string>> tableData = table.Extract();
                    Assert.NotNull(tableData);

                    string markdown = table.ToMarkdown(clean: false, fillEmpty: true);
                    Assert.NotNull(markdown);
                }

                // Test 4: Get tables from all pages (as in Demo)
                int totalTables = 0;
                for (int pageNum = 0; pageNum < doc.PageCount; pageNum++)
                {
                    Page currentPage = doc[pageNum];
                    List<Table> pageTables = Utils.GetTables(
                        currentPage,
                        clip: currentPage.Rect,
                        vertical_strategy: "lines_strict",
                        horizontal_strategy: "lines_strict");
                    if (pageTables.Count > 0)
                        totalTables += pageTables.Count;
                    currentPage.Dispose();
                }

                Assert.Equal(6, totalTables);
                page.Dispose();
            }
            finally
            {
                doc.Close();
            }
        }

        [Fact]
        public void BorderedTable()
        {
            Document doc = new Document(Doc("bordered-table.pdf"));
            Rect clip = new Rect(20, 100, 580, 300);
            Page page = doc[0];
            int cellCount = 0;

            List<Table> tables = page.GetTables(clip:clip);
            foreach (var table in tables)
            {
                List<List<string>> text = table.Extract();
                foreach (var row in text)
                {
                    foreach (var cell in row)
                    {
                        cellCount++;
                    }
                }
            }

            doc.Close();

            Assert.Equal(32, cellCount);
        }

        [Fact]
        public void NonBorderedTable()
        {
            Document doc = new Document(Doc("non-bordered-table.pdf"));
            Page page = doc[0];
            int cellCount = 0;

            List<Table> tables = page.GetTables(vertical_strategy: "text", horizontal_strategy: "text");
            foreach (var table in tables)
            {
                List<List<string>> text = table.Extract();
                foreach (var row in text)
                {
                    foreach (var cell in row)
                    {
                        cellCount++;
                    }
                }
            }

            doc.Close();

            Assert.Equal(102, cellCount);
        }

        /// <summary>
        /// Parallel stress test for <see cref="Utils.GetTables"/>; each worker opens its own document.
        /// </summary>
        [Fact]
        public void TestGetTablesParallel()
        {
            const int iterations = 50;
            const int degreeOfParallelism = 4;

            string testFilePath = Doc("bordered-table.pdf");
            var errors = new ConcurrentBag<Exception>();
            var tableCounts = new ConcurrentBag<int>();

            Parallel.ForEach(
                Enumerable.Range(0, iterations),
                new ParallelOptions { MaxDegreeOfParallelism = degreeOfParallelism },
                iteration =>
                {
                    try
                    {
                        using var doc = new Document(testFilePath);
                        using var page = doc[0];

                        List<Table> tables = Utils.GetTables(
                            page,
                            clip: page.Rect,
                            vertical_strategy: iteration % 2 == 0 ? "lines" : "text",
                            horizontal_strategy: iteration % 2 == 0 ? "lines" : "text");

                        tableCounts.Add(tables.Count);
                        foreach (Table table in tables)
                        {
                            List<List<string>> data = table.Extract();
                            if (data == null)
                                throw new InvalidOperationException("table.Extract() returned null");
                        }
                    }
                    catch (Exception ex)
                    {
                        errors.Add(ex);
                    }
                });

            Assert.True(errors.IsEmpty, string.Join(Environment.NewLine, errors.Select(e => e.ToString())));
            Assert.Equal(iterations, tableCounts.Count);
            Assert.Contains(tableCounts, count => count > 0);
        }
    }
}