using Xunit;

namespace MuPDF.NET.Test
{
    /// <summary>
    /// — graphics state <c>q</c>/<c>Q</c> balance,
    /// <see cref="Page.IsWrapped"/>, and <see cref="Page.WrapContents"/>.
    /// </summary>
    public class TestBalanceCount
    {
        private static readonly string outDocPath = _Path.ForOutput("test_q_count.pdf", nameof(TestBalanceCount));

        /// <summary>
        /// <summary>Regression test: q count.</summary>
        /// Testing graphics state balances and wrap_contents().
        /// Take page's contents and generate various imbalanced graphics state
        /// situations. Each time compare q-count with expected results.
        /// Finally confirm we are out of balance using "is_wrapped", wrap the
        /// contents object(s) via "wrap_contents()" and confirm success.
        /// PDF commands "q" / "Q" stand for "push", respectively "pop".
        /// </summary>
        [Fact]
        public void test_q_count()
        {
            using var doc = new Document();
            var page = doc.NewPage();
            // the page has no /Contents objects at all yet. Create one causing
            // an initial imbalance (so prepended "q" is needed)
            Tools.InsertContents(page, "Q", overlay: true); // append
            Assert.Equal((1, 0), page._count_q_balance());
            Assert.False(page.is_wrapped);

            // Prepend more data that yield a different type of imbalanced contents:
            // Although counts of q and Q are equal now, the unshielded 'cm' before
            // the first 'q' makes the contents unusable for insertions.
            Tools.InsertContents(page, "1 0 0 -1 0 0 cm q ", overlay: false); // prepend
            Assert.False(page.is_wrapped);
            if (page._count_q_balance() == (0, 0))
            {
                Console.WriteLine("imbalance undetected by q balance count");
            }

            const string text = "Hello, World!";
            page.InsertText(new Point(100, 100), text); // establishes balance!

            // this should have produced a balanced graphics state
            Assert.Equal((0, 0), page._count_q_balance());
            Assert.True(page.is_wrapped);

            // an appended "pop" must be balanced by a prepended "push"
            Tools.InsertContents(page, "Q", overlay: true); // append
            Assert.Equal((1, 0), page._count_q_balance());

            // a prepended "pop" yet needs another push
            Tools.InsertContents(page, "Q", overlay: false); // prepend
            Assert.Equal((2, 0), page._count_q_balance());

            // an appended "push" needs an additional "pop"
            Tools.InsertContents(page, "q", overlay: true); // append
            Assert.Equal((2, 1), page._count_q_balance());

            // wrapping the contents should yield a balanced state again
            Assert.False(page.is_wrapped);
            page.WrapContents();
            Assert.True(page.is_wrapped);
            Assert.Equal((0, 0), page._count_q_balance());

            doc.Save(outDocPath);
        }
    }
}