using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace MuPDF.NET
{
    public class Toc
    {
        /// <summary>
        /// hierarchy level
        /// </summary>
        public int Level { get; set; }

        /// <summary>
        /// title
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// 1-based source page number
        /// </summary>
        public int Page { get; set; }

        /// <summary>
        /// included only if simple is False. Contains details of the TOC item
        /// </summary>
        public object Link { get; set; } = null;

        /// <summary>Legacy MuPDF.NET / PyMuPDF tuple field names.</summary>
        public int level
        {
            get => Level;
            set => Level = value;
        }

        public string title
        {
            get => Title;
            set => Title = value;
        }

        public int page
        {
            get => Page;
            set => Page = value;
        }

        public Dictionary<string, object> link
        {
            get => Link as Dictionary<string, object>;
            set => Link = value;
        }

        public void Deconstruct(
            out int level,
            out string title,
            out int page,
            out Dictionary<string, object> link)
        {
            level = Level;
            title = Title;
            page = Page;
            link = Link as Dictionary<string, object>;
        }

        public override string ToString()
        {
            return "Level=" + Level + ", Title=" + Title + ", Page=" + Page + ", Link=" + (Link != null);
        }

        // Compatibility bridge: allows assignment from modern Document.GetToc tuple rows.
        public static implicit operator Toc((int level, string title, int page, Dictionary<string, object> link) value)
        {
            return new Toc
            {
                Level = value.level,
                Title = value.title,
                Page = value.page,
                Link = value.link,
            };
        }
    }

    /// <summary>
    /// Table of contents result compatible with legacy <see cref="List{Toc}"/>
    /// and modern tuple-row consumers.
    /// </summary>
    public readonly struct TocResult : IReadOnlyList<(int level, string title, int page, Dictionary<string, object> link)>
    {
        private readonly List<Toc> _items;

        public TocResult(List<Toc> items) => _items = items ?? new List<Toc>();

        public int Count => _items.Count;

        public (int level, string title, int page, Dictionary<string, object> link) this[int index]
        {
            get
            {
                var item = _items[index];
                return (item.level, item.title, item.page, item.link);
            }
        }

        public IEnumerator<(int level, string title, int page, Dictionary<string, object> link)> GetEnumerator()
        {
            foreach (var item in _items)
                yield return (item.level, item.title, item.page, item.link);
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public static implicit operator List<Toc>(TocResult result) =>
            result._items ?? new List<Toc>();

        public static implicit operator List<(int level, string title, int page, Dictionary<string, object> link)>(TocResult result)
        {
            if (result._items == null || result._items.Count == 0)
                return new List<(int level, string title, int page, Dictionary<string, object> link)>();
            return result._items
                .Select(t => (t.level, t.title, t.page, t.link))
                .ToList();
        }
    }
}
