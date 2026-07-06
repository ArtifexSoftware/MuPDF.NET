using System;
using System.Collections.Generic;

namespace MuPDF.NET
{
    public class LinkInfo
    {
        /// <summary>
        /// describing the "hot spot" location on the page's visible representation
        /// </summary>
        public Rect From { get; set; }

        /// <summary>
        /// an integer indicating the kind of link
        /// </summary>
        public LinkType Kind { get; set; }

        public Point To { get; set; } = null;

        public string ToStr { get; set; } //used page number is less than 0

        /// <summary>
        /// page number
        /// </summary>
        public int Page { get; set; }

        public string Name { get; set; }

        /// <summary>Named destination ( key for <c>LINK_NAMED</c>).</summary>
        public string NamedDest { get; set; }

        /// <summary>
        /// a string specifying the destination internet resource
        /// </summary>
        public string Uri { get; set; }

        /// <summary>
        /// zoom value
        /// </summary>
        public float Zoom { get; set; } = 0;

        /// <summary>
        /// a string specifying the destination file
        /// </summary>
        public string File { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// xref of the item
        /// </summary>
        public int Xref { get; set; }

        /// <summary>
        /// true if italic item text, or omitted. PDF only
        /// </summary>
        public bool Italic { get; set; } = false;

        /// <summary>
        /// true if bold item text or omitted. PDF only
        /// </summary>
        public bool Bold { get; set; } = false;

        /// <summary>
        /// true if sub-items are folded, or omitted in toc. PDF only
        /// </summary>
        public bool Collapse { get; set; }

        /// <summary>
        /// item color in PDF RGB format
        /// </summary>
        public float[] Color { get; set; }

        public override string ToString()
        {
            return $"Kind = {(int)Kind}, Xref = {Xref}, Page = {Page}, To = {To.ToString()}, Zoom = {Zoom}, Collapse = {Collapse}";
        }

        /// <summary>MuPDF.NET dictionary-style access (<c>link["kind"]</c>).</summary>
        public object this[string key]
        {
            get
            {
                switch (key)
                {
                    case "from": return From;
                    case "kind": return (int)Kind;
                    case "to": return To ?? (object)ToStr;
                    case "page": return Page;
                    case "name": return Name;
                    case "nameddest": return NamedDest;
                    case "uri": return Uri;
                    case "zoom": return Zoom;
                    case "file": return File;
                    case "id": return Id;
                    case "xref": return Xref;
                    default: return null;
                }
            }
            set
            {
                switch (key)
                {
                    case "from": From = value as Rect; break;
                    case "kind": Kind = (LinkType)Convert.ToInt32(value); break;
                    case "to":
                        if (value is Point p)
                            To = p;
                        else
                            ToStr = value?.ToString();
                        break;
                    case "page": Page = Convert.ToInt32(value); break;
                    case "name": Name = value?.ToString(); break;
                    case "nameddest": NamedDest = value?.ToString(); break;
                    case "uri": Uri = value?.ToString(); break;
                    case "zoom": Zoom = Convert.ToSingle(value); break;
                    case "file": File = value?.ToString(); break;
                    case "id": Id = value?.ToString(); break;
                    case "xref": Xref = Convert.ToInt32(value); break;
                }
            }
        }

        public bool TryGetValue(string key, out object value)
        {
            value = this[key];
            return value != null;
        }

        public static implicit operator Dictionary<string, object>(LinkInfo link)
        {
            if (link == null)
                return null;
            var d = new Dictionary<string, object>
            {
                ["from"] = link.From,
                ["kind"] = (int)link.Kind,
                ["page"] = link.Page,
                ["zoom"] = link.Zoom,
                ["xref"] = link.Xref,
            };
            if (link.To != null)
                d["to"] = link.To;
            else if (!string.IsNullOrEmpty(link.ToStr))
                d["to"] = link.ToStr;
            if (!string.IsNullOrEmpty(link.Name))
                d["name"] = link.Name;
            if (!string.IsNullOrEmpty(link.NamedDest))
                d["nameddest"] = link.NamedDest;
            if (!string.IsNullOrEmpty(link.Uri))
                d["uri"] = link.Uri;
            if (!string.IsNullOrEmpty(link.File))
                d["file"] = link.File;
            if (!string.IsNullOrEmpty(link.Id))
                d["id"] = link.Id;
            return d;
        }

        // Compatibility bridge: allows assignment from modern Page.GetLinks dictionaries.
        public static implicit operator LinkInfo(Dictionary<string, object> data)
        {
            if (data == null)
                return null;
            int I(string key) => data.TryGetValue(key, out var v) ? Convert.ToInt32(v) : 0;
            float F(string key) => data.TryGetValue(key, out var v) ? Convert.ToSingle(v) : 0f;
            string Opt(string key) => data.TryGetValue(key, out var v) ? v?.ToString() : null;
            return new LinkInfo
            {
                From = data.TryGetValue("from", out var from) ? from as Rect : null,
                Kind = data.TryGetValue("kind", out var kind) ? (LinkType)Convert.ToInt32(kind) : LinkType.None,
                To = data.TryGetValue("to", out var to) ? to as Point : null,
                ToStr = data.TryGetValue("to", out var toStr) && toStr is not Point ? toStr?.ToString() : null,
                Page = I("page"),
                Name = Opt("name"),
                NamedDest = Opt("nameddest"),
                Uri = Opt("uri"),
                Zoom = F("zoom"),
                File = Opt("file"),
                Id = Opt("id"),
                Xref = I("xref"),
            };
        }
    }
}