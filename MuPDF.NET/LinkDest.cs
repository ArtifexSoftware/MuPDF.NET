using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;

namespace MuPDF.NET
{
    /// <summary>
    /// Link or outline destination details (Python <c>linkDest</c>).
    /// </summary>
    public class LinkDest
    {
        private static readonly Regex RxPageZoom =
            new Regex(@"^#page=([0-9]+)&zoom=([0-9.]+),(-?[0-9.]+),(-?[0-9.]+)$", RegexOptions.CultureInvariant);
        private static readonly Regex RxPageOnly =
            new Regex(@"^#page=([0-9]+)$", RegexOptions.CultureInvariant);
        private static readonly Regex RxNamedDest =
            new Regex(@"^#nameddest=(.*)", RegexOptions.CultureInvariant);

        /// <summary>Python <c>linkDest.dest</c> (reserved; remains empty in the constructor port).</summary>
        public string DestStr { get; } = "";

        public string FileSpec { get; private set; } = "";
        public int Flags { get; private set; }
        public bool IsMap { get; private set; }
        public bool IsUri { get; private set; }
        public int Kind { get; private set; } = Constants.LINK_NONE;
        public Point Lt { get; private set; } = new Point(0, 0);
        public Point Rb { get; private set; } = new Point(0, 0);
        public Dictionary<string, object> Named { get; private set; } = new Dictionary<string, object>();
        public string NewWindow { get; private set; } = "";
        public int Page { get; private set; }
        public string Uri { get; private set; }

        /// <summary>Python <c>linkDest(obj, rlink, document)</c>.</summary>
        /// <param name="link">Source link.</param>
        /// <param name="resolved">Result of <see cref="Document.ResolveLink(string)"/> when applicable; otherwise <c>null</c>.</param>
        /// <param name="document">Owning document (for named destinations); may be <c>null</c>.</param>
        public LinkDest(Link link, (int page, float x, float y)? resolved, Document document)
        {
            if (link == null) throw new ArgumentNullException(nameof(link));
            InitFromUriPage(link.Uri ?? "", link.IsExternal, link.Page, resolved, document);
        }

        /// <summary>Python <c>linkDest(outline, None, document)</c> / <c>Outline.destination</c>.</summary>
        public LinkDest(Outline outline, Document document)
        {
            if (outline == null) throw new ArgumentNullException(nameof(outline));
            InitFromUriPage(outline.Uri ?? "", outline.IsExternal, outline.Page, null, document);
        }

        void InitFromUriPage(string uri0, bool isExt, int sourcePage, (int page, float x, float y)? resolved, Document document)
        {
            bool isInt = !isExt;
            Uri = uri0 ?? "";
            Page = sourcePage;
            Kind = Constants.LINK_NONE;
            Named = new Dictionary<string, object>();

            if (resolved.HasValue && (Uri.Length == 0 || Uri[0] != '#'))
            {
                var r = resolved.Value;
                Uri = string.Format(CultureInfo.InvariantCulture, "#page={0}&zoom=0,{1},{2}",
                    r.page + 1, FormatG(r.x), FormatG(r.y));
            }

            if (isExt)
            {
                Page = -1;
                Kind = Constants.LINK_URI;
            }

            if (string.IsNullOrEmpty(Uri))
            {
                Page = -1;
                Kind = Constants.LINK_NONE;
            }

            if (isInt && !string.IsNullOrEmpty(Uri))
            {
                Uri = Uri.Replace("&zoom=nan", "&zoom=0");
                if (Uri.StartsWith("#", StringComparison.Ordinal))
                {
                    Kind = Constants.LINK_GOTO;
                    var mz = RxPageZoom.Match(Uri);
                    if (mz.Success)
                    {
                        Page = int.Parse(mz.Groups[1].Value, CultureInfo.InvariantCulture) - 1;
                        Lt = new Point(
                            double.Parse(mz.Groups[3].Value, CultureInfo.InvariantCulture),
                            double.Parse(mz.Groups[4].Value, CultureInfo.InvariantCulture));
                        Flags |= Constants.LINK_FLAG_L_VALID | Constants.LINK_FLAG_T_VALID;
                    }
                    else
                    {
                        var mp = RxPageOnly.Match(Uri);
                        if (mp.Success)
                            Page = int.Parse(mp.Groups[1].Value, CultureInfo.InvariantCulture) - 1;
                        else
                        {
                            Kind = Constants.LINK_NAMED;
                            var mn = RxNamedDest.Match(Uri);
                            if (document != null && mn.Success)
                            {
                                string named = UnescapePercent(mn.Groups[1].Value);
                                Named.Clear();
                                if (document.ResolveNames().TryGetValue(named, out var entry))
                                {
                                    foreach (var kv in entry)
                                        Named[kv.Key] = kv.Value;
                                }
                                Named["nameddest"] = named;
                            }
                            else
                            {
                                Named = UriToDict(Uri.Length > 1 ? Uri.Substring(1) : "");
                            }
                        }
                    }
                }
                else
                {
                    Kind = Constants.LINK_NAMED;
                    Named = UriToDict(Uri);
                }
            }

            if (isExt)
            {
                if (string.IsNullOrEmpty(Uri))
                {
                    // keep defaults
                }
                else if (Uri.StartsWith("file:", StringComparison.OrdinalIgnoreCase))
                {
                    FileSpec = Uri.Length > 5 ? Uri.Substring(5) : "";
                    if (FileSpec.StartsWith("//", StringComparison.Ordinal))
                        FileSpec = FileSpec.Substring(2);
                    IsUri = false;
                    Uri = "";
                    Kind = Constants.LINK_LAUNCH;
                    var hash = FileSpec.IndexOf('#');
                    if (hash >= 0)
                    {
                        string tail = FileSpec.Substring(hash + 1);
                        FileSpec = FileSpec.Substring(0, hash);
                        if (tail.StartsWith("page=", StringComparison.Ordinal))
                        {
                            Kind = Constants.LINK_GOTOR;
                            var part = tail.Split('&')[0];
                            if (part.Length > 5 && int.TryParse(part.Substring(5), NumberStyles.Integer, CultureInfo.InvariantCulture, out int p1))
                                Page = p1 - 1;
                        }
                    }
                }
                else if (Uri.IndexOf(':') >= 0)
                {
                    IsUri = true;
                    Kind = Constants.LINK_URI;
                }
                else
                {
                    IsUri = true;
                    Kind = Constants.LINK_LAUNCH;
                }
            }
        }

        private static string FormatG(double v) => v.ToString("G9", CultureInfo.InvariantCulture);

        private static Dictionary<string, object> UriToDict(string uriWithoutHash)
        {
            var ret = new Dictionary<string, object>();
            foreach (var item in uriWithoutHash.Split('&'))
            {
                if (item.Length == 0) continue;
                int eq = item.IndexOf('=');
                if (eq >= 0)
                    ret[item.Substring(0, eq)] = item.Substring(eq + 1);
                else
                    ret[item] = null;
            }
            return ret;
        }

        private static string UnescapePercent(string encodedName)
        {
            string split = encodedName.Replace("%%", "%25");
            var parts = split.Split('%');
            if (parts.Length == 0) return "";
            var sb = new System.Text.StringBuilder(parts[0]);
            for (int i = 1; i < parts.Length; i++)
            {
                string item = parts[i];
                if (item.Length >= 2)
                {
                    string piece = item.Substring(0, 2);
                    int code = int.Parse(piece, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                    sb.Append((char)code);
                    sb.Append(item.Substring(2));
                }
                else
                    sb.Append('%').Append(item);
            }
            return sb.ToString();
        }

        // Python-style attribute names (read-only snapshots after construction).
        public string dest => DestStr;
        public string file_spec => FileSpec;
        public int flags => Flags;
        public bool is_map => IsMap;
        public bool is_uri => IsUri;
        public int kind => Kind;
        public Point lt => Lt;
        public Point rb => Rb;
        public Dictionary<string, object> named => Named;
        public string new_window => NewWindow;
        public int page => Page;
        public string uri => Uri;
    }
}
