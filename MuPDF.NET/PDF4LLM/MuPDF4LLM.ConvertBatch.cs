using System;
using System.Collections.Generic;
using System.IO;

namespace MuPDF.NET.PDF4LLM
{
    public static partial class MuPDF4LLM
    {

        /// <summary>
        /// Convert a list of document paths (layout <c>convert_batch</c>).
        /// Sequential; worker-pool / streaming / persistent strategies are not ported.
        /// </summary>

        /// <param name="inputs">Document paths.</param>
        /// <param name="backend"><c>md</c>, <c>json</c>, or <c>txt</c>.</param>
        public static List<string> ConvertBatch(IReadOnlyList<string> inputs, string backend = "md")
        {
            if (inputs == null)
                throw new ArgumentNullException(nameof(inputs));

            string kind = (backend ?? "md").Trim().ToLowerInvariant();
            if (kind != "md" && kind != "json" && kind != "txt")
                throw new ArgumentException("backend must be 'md', 'json', or 'txt'.", nameof(backend));

            var results = new List<string>(inputs.Count);
            foreach (string path in inputs)
            {
                if (string.IsNullOrWhiteSpace(path))
                    throw new ArgumentException("Each input path must be non-empty.", nameof(inputs));
                if (!File.Exists(path))
                    throw new System.IO.FileNotFoundException("Input document not found.", path);

                switch (kind)
                {
                    case "json":
                        results.Add(ToJson(path));
                        break;
                    case "txt":
                        results.Add(ToText(path));
                        break;
                    default:
                        results.Add(ToMarkdown(path));
                        break;
                }
            }

            return results;
        }
    }
}
