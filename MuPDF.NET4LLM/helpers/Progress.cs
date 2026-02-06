using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace MuPDF.NET4LLM.Helpers
{
    /// <summary>
    /// Text-based progress bar to allow watching the advancement
    /// of Markdown conversion of document pages.
    /// Ported and adapted from LLM helpers.
    /// 
    /// Copyright and License
    /// Copyright 2024 Artifex Software, Inc.
    /// License GNU Affero GPL 3.0
    /// </summary>
    public class _ProgressBar : IEnumerator<object>
    {
        private readonly List<object> _items;
        private readonly int _progressWidth;
        private readonly int _lenDigits;
        private float _progressBarValue;
        private int _currentIndex;
        private IEnumerator<object> _enumerator;

        public _ProgressBar(List<object> items, int progressWidth = 40)
        {
            _items = items;
            _progressWidth = progressWidth;
            _lenDigits = items.Count.ToString().Length;
            _progressBarValue = 0;
            _currentIndex = -1; // Start at -1 for initial MoveNext to work
            _enumerator = items.GetEnumerator();

            // Calculate the increment for each item based on the list length and the progress width
            // Init progress bar
            Console.Write($"[{new string(' ', _progressWidth)}] (0/{_items.Count})");
            Console.Out.Flush();
            Console.Write($"\b{_progressWidth + _lenDigits + 6}");
        }

        public object Current => _enumerator.Current;

        public bool MoveNext()
        {
            if (!_enumerator.MoveNext())
            {
                // End progress on StopIteration
                Console.WriteLine("]\n");
                return false;
            }

            // Update the current index
            _currentIndex++;

            // Add the increment to the progress bar and calculate how many "=" to add
            _progressBarValue += (float)_progressWidth / _items.Count;

            int filledLength = (int)(_currentIndex * (float)_progressWidth / _items.Count);
            // Update the numerical progress
            string paddedIndex = (_currentIndex + 1).ToString().PadLeft(_lenDigits);
            string progressInfo = $" ({paddedIndex}/{_items.Count})";

            Console.Write($"\r[{new string('=', filledLength)}{new string(' ', _progressWidth - filledLength)}]");
            Console.Write(progressInfo);
            Console.Out.Flush();

            return true;
        }

        public void Reset()
        {
            _currentIndex = -1;
            _progressBarValue = 0;
            _enumerator.Reset();
        }

        public void Dispose()
        {
            _enumerator?.Dispose();
        }
    }

    public static class ProgressBar
    {
        public static IEnumerator<object> Create(List<object> list, int progressWidth = 40)
        {
            return new _ProgressBar(list, progressWidth);
        }
    }
}
