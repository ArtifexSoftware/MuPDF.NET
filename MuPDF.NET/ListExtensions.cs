using System;
using System.Collections.Generic;

namespace MuPDF.NET
{
    public static class ListExtensions
    {
        /// <summary>
        /// Return a sub-list starting at <paramref name="start"/> with up to <paramref name="count"/> items.
        /// </summary>
        public static List<T> Slice<T>(this List<T> list, int start, int count)
        {
            if (list == null)
                throw new ArgumentNullException(nameof(list));
            if (count <= 0 || list.Count == 0 || start >= list.Count)
                return new List<T>();
            if (start < 0)
                start = Math.Max(0, list.Count + start);
            int available = list.Count - start;
            if (available <= 0)
                return new List<T>();
            return list.GetRange(start, Math.Min(count, available));
        }
    }
}
