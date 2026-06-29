using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;

namespace MuPDF.NET
{
    /// <summary>
    /// Link or outline destination details.
    /// <para>Ports PyMuPDF <c>class linkDest</c> (<c>src/__init__.py</c>).</para>
    /// </summary>
    public class LinkDest
    {
        private static readonly Regex RxPageZoom =
            new Regex(@"^#page=([0-9]+)&zoom=([0-9.]+),(-?[0-9.]+),(-?[0-9.]+)$", RegexOptions.CultureInvariant);
        private static readonly Regex RxPageOnly =
            new Regex(@"^#page=([0-9]+)$", RegexOptions.CultureInvariant);
        private static readonly Regex RxNamedDest =
            new Regex(@"^#nameddest=(.*)", RegexOptions.CultureInvariant);

        /// <summary>Reserved destination string (PyMuPDF <c>linkDest.dest</c>; stays empty).</summary>
        public string DestStr { get; } = "";

        /// <summary>External file spec (PyMuPDF <c>linkDest.file_spec</c>).</summary>
        public string FileSpec { get; private set; } = "";

        /// <summary>Link destination flags (PyMuPDF <c>linkDest.flags</c>).</summary>
        public int Flags { get; private set; }

        /// <summary>Whether destination is a map (PyMuPDF <c>linkDest.is_map</c>).</summary>
        public bool IsMap { get; private set; }

        /// <summary>Whether destination is a URI (PyMuPDF <c>linkDest.is_uri</c>).</summary>
        public bool IsUri { get; private set; }

        /// <summary>Link kind (<c>LINK_*</c> / <see cref="LinkType"/>).</summary>
        public int Kind { get; private set; } = Constants.LinkNone;

        /// <summary>Top-left point for zoom destinations (PyMuPDF <c>linkDest.lt</c>).</summary>
        public Point Lt { get; private set; } = new Point(0, 0);

        /// <summary>Bottom-right point (PyMuPDF <c>linkDest.rb</c>).</summary>
        public Point Rb { get; private set; } = new Point(0, 0);

        /// <summary>Named destination parameters (PyMuPDF <c>linkDest.named</c>).</summary>
        public Dictionary<string, object> Named { get; private set; } = new Dictionary<string, object>();

        /// <summary>New-window flag (PyMuPDF <c>linkDest.new_window</c>).</summary>
        public string NewWindow { get; private set; } = "";

        /// <summary>Target page number (PyMuPDF <c>linkDest.page</c>).</summary>
        public int Page { get; private set; }

        /// <summary>URI fragment or external URI (PyMuPDF <c>linkDest.uri</c>).</summary>
        public string Uri { get; private set; }

        /// <summary>
        /// Build from a <see cref="Link"/> (PyMuPDF <c>linkDest(obj, rlink, document)</c>).
        /// </summary>
        /// <param name="link">Source link.</param>
        /// <param name="resolved">Result of <see cref="Document.ResolveLink(string)"/> when applicable.</param>
        /// <param name="document">Owning document for named destinations.</param>
        public LinkDest(Link link, (int page, float x, float y)? resolved, Document document)
        {
            if (link == null) throw new ArgumentNullException(nameof(link));
            InitFromUriPage(link.Uri ?? "", link.IsExternal, link.Page, resolved, document);
        }

        /// <summary>
        /// Build from an <see cref="Outline"/> (PyMuPDF <c>linkDest(outline, None, document)</c>).
        /// </summary>
        public LinkDest(Outline outline, Document document)
        {
            if (outline == null) throw new ArgumentNullException(nameof(outline));
            InitFromUriPage(outline.Uri ?? "", outline.IsExternal, outline.Page, null, document);
        }

        private void InitFromUriPage(string uri0, bool isExt, int sourcePage, (int page, float x, float y)? resolved, Document document)
        {
            // isExt = obj.is_external; isInt = not isExt
            bool isInt = !isExt;
            Uri = uri0 ?? "";
            Page = sourcePage;
            Kind = Constants.LinkNone;
            Named = new Dictionary<string, object>();

            // if rlink and not self.uri.startswith("#"):
            if (resolved.HasValue && (Uri.Length == 0 || Uri[0] != '#'))
            {
                var r = resolved.Value;
                Uri = string.Format(CultureInfo.InvariantCulture, "#page={0}&zoom=0,{1},{2}",
                    r.page + 1, FormatG(r.x), FormatG(r.y));
            }

            if (isExt)
            {
                Page = -1;
                Kind = Constants.LinkUri;
            }

            if (string.IsNullOrEmpty(Uri))
            {
                Page = -1;
                Kind = Constants.LinkNone;
            }

            if (isInt && !string.IsNullOrEmpty(Uri))
            {
                Uri = Uri.Replace("&zoom=nan", "&zoom=0");
                if (Uri.StartsWith("#", StringComparison.Ordinal))
                {
                    Kind = Constants.LinkGoto;
                    var mz = RxPageZoom.Match(Uri);
                    if (mz.Success)
                    {
                        Page = int.Parse(mz.Groups[1].Value, CultureInfo.InvariantCulture) - 1;
                        Lt = new Point(
                            float.Parse(mz.Groups[3].Value, CultureInfo.InvariantCulture),
                            float.Parse(mz.Groups[4].Value, CultureInfo.InvariantCulture));
                        Flags |= Constants.LinkFlagLValid | Constants.LinkFlagTValid;
                    }
                    else
                    {
                        var mp = RxPageOnly.Match(Uri);
                        if (mp.Success)
                            Page = int.Parse(mp.Groups[1].Value, CultureInfo.InvariantCulture) - 1;
                        else
                        {
                            Kind = Constants.LinkNamed;
                            var mn = RxNamedDest.Match(Uri);
                            if (document != null && mn.Success)
                            {
                                // named = unescape(m.group(1)); self.named = document.resolve_names().get(named)
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
                    Kind = Constants.LinkNamed;
                    Named = UriToDict(Uri);
                }
            }

            if (isExt)
            {
                if (string.IsNullOrEmpty(Uri))
                {
                }
                else if (Uri.StartsWith("file:", StringComparison.OrdinalIgnoreCase))
                {
                    FileSpec = Uri.Length > 5 ? Uri.Substring(5) : "";
                    if (FileSpec.StartsWith("//", StringComparison.Ordinal))
                        FileSpec = FileSpec.Substring(2);
                    IsUri = false;
                    Uri = "";
                    Kind = Constants.LinkLaunch;
                    var hash = FileSpec.IndexOf('#');
                    if (hash >= 0)
                    {
                        string tail = FileSpec.Substring(hash + 1);
                        FileSpec = FileSpec.Substring(0, hash);
                        if (tail.StartsWith("page=", StringComparison.Ordinal))
                        {
                            Kind = Constants.LinkGotor;
                            var part = tail.Split('&')[0];
                            if (part.Length > 5 && int.TryParse(part.Substring(5), NumberStyles.Integer, CultureInfo.InvariantCulture, out int p1))
                                Page = p1 - 1;
                        }
                    }
                }
                else if (Uri.IndexOf(':') >= 0)
                {
                    IsUri = true;
                    Kind = Constants.LinkUri;
                }
                else
                {
                    IsUri = true;
                    Kind = Constants.LinkLaunch;
                }
            }
        }

        private static string FormatG(float v) => v.ToString("G9", CultureInfo.InvariantCulture);

        /// <summary>Parses a URI action string into a link-destination dictionary.</summary>
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

        /// <summary>Unescapes <c>%AB</c> URI substrings to characters.</summary>
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

        // ─── PyMuPDF attribute names (internal, same assembly) ───────────

        internal string dest => DestStr;

        internal string file_spec => FileSpec;

        internal int flags => Flags;

        internal bool is_map => IsMap;

        internal bool is_uri => IsUri;

        internal int kind => Kind;

        internal Point lt => Lt;

        internal Point rb => Rb;

        internal Dictionary<string, object> named => Named;

        internal string new_window => NewWindow;

        internal int page => Page;

        internal string uri => Uri;
    }
}
