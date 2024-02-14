using mupdf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using Microsoft.Maui.Layouts;

namespace MuPDF.NET
{
    public class MuPDFXml : IDisposable
    {
        private FzXml _nativeXml;

        public FzXml ToFzXml()
        {
            return _nativeXml;
        }

        public MuPDFXml(FzXml rhs)
        {
            _nativeXml = rhs;
        }

        public string Text
        {
            get
            {
                return _nativeXml.fz_xml_text();
            }
        }

        public bool IsText
        {
            get
            {
                return Text != null;
            }
        }

        public string TagName
        {
            get
            {
                return _nativeXml.fz_xml_tag();
            }
        }

        public MuPDFXml Root
        {
            get
            {
                return new MuPDFXml(_nativeXml.fz_xml_root());
            }
        }

        public MuPDFXml Previous
        {
            get
            {
                FzXml ret = _nativeXml.fz_dom_previous();
                if (ret != null)
                    return new MuPDFXml(ret);
                else
                    return null;
            }
        }

        public MuPDFXml Next
        {
            get
            {
                FzXml ret = _nativeXml.fz_dom_next();
                if (ret != null)
                    return new MuPDFXml(ret);
                else
                    return null;
            }
        }

        public MuPDFXml Parent
        {
            get
            {
                FzXml ret = _nativeXml.fz_dom_parent();
                if (ret != null)
                    return new MuPDFXml(ret);
                else
                    return null;
            }
        }

        public MuPDFXml FirstChild
        {
            get
            {
                if (_nativeXml.fz_xml_text() == null)
                    return null;
                FzXml ret = _nativeXml.fz_dom_first_child();
                if (ret != null)
                    return new MuPDFXml(ret);
                else
                    return null;
            }
        }

        public MuPDFXml LastChild
        {
            get
            {
                MuPDFXml child = FirstChild;
                if (child == null)
                    return null;
                while (true)
                {
                    MuPDFXml next = child.Next;
                    if (next == null)
                        return child;
                    child = next;
                }
            }
        }

        public MuPDFXml(string rhs)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(rhs);

            IntPtr unmanagedPointer = Marshal.AllocHGlobal(bytes.Length);
            Marshal.Copy(bytes, 0, unmanagedPointer, bytes.Length);
            SWIGTYPE_p_unsigned_char s = new SWIGTYPE_p_unsigned_char(unmanagedPointer, false);
            Marshal.FreeHGlobal(unmanagedPointer);

            FzBuffer buf = mupdf.mupdf.fz_new_buffer_from_copied_data(s, (uint)bytes.Length);
            _nativeXml = mupdf.mupdf.fz_parse_xml_from_html5(buf);
        }

        private List<(int, string)> ShowNode(MuPDFXml node, List<(int, string)> items, int shift)
        {
            while (node != null)
            {
                if (node.IsText)
                {
                    items.Add((shift, $"\"{node.Text}\""));
                    node = node.Next;
                    continue;
                }
                items.Add((shift, $"\"{node.TagName}\""));
                foreach ((string k, string v) in node.GetAttributes())
                    items.Add((shift, $"={k} \"{v}\""));
                MuPDFXml child = node.FirstChild;
                if (child != null)
                {
                    items = ShowNode(child, items, shift + 1);
                }

                items.Add((shift, $"){node.TagName}"));
                node = node.Next;
            }
            return items;
        }

        private List<(int, string)> GetNodeTree()
        {
            int shift = 0;
            List<(int, string)> items = new List<(int, string)>();
            items = ShowNode(this, items, shift);
            return items;
        }

        public MuPDFXml CreateElement(string tag)
        {
            return new MuPDFXml(_nativeXml.fz_dom_create_text_node(tag));
        }

        public MuPDFXml CreateTextNode(string text)
        {
            return new MuPDFXml(_nativeXml.fz_dom_create_text_node(text));
        }

        public void AppendChild(MuPDFXml child)
        {
            _nativeXml.fz_dom_append_child(child.ToFzXml());
        }

        public MuPDFXml AddBulletList()
        {
            MuPDFXml child = CreateElement("ul");
            AppendChild(child);
            return child;
        }

        public MuPDFXml AddClass(string text)
        {
            string cls = GetAttributeValue("class");
            if (cls != null && cls.Contains(text))
                return this;
            RemoveAttribute("class");
            if (cls == null)
                cls = text;
            else
                cls += " " + text;
            SetAttribute("class", cls);
            return this;
        }

        public MuPDFXml AddCode(string text = null)
        {
            MuPDFXml child = CreateElement("code");
            AppendChild(CreateTextNode(text));
            MuPDFXml prev = SpanBottom();
            if (prev != null)
                prev = this;
            prev.AppendChild(child);
            return this;
        }

        public MuPDFXml AddCodeBlock()
        {
            MuPDFXml child = CreateElement("pre");
            AppendChild(child);
            return child;
        }

        public MuPDFXml AddDescriptionList()
        {
            MuPDFXml child = CreateElement("dl");
            AppendChild(child);
            return child;
        }

        public MuPDFXml AddDivision()
        {
            MuPDFXml child = CreateElement("div");
            AppendChild(child);
            return child;
        }

        public MuPDFXml AddHeader(int level = 1)
        {
            if (!Utils.INRANGE(level, 1, 6))
                throw new Exception("Header level must be in [1, 6]");
            string tagName = TagName;
            string newTag = $"h{level}";
            MuPDFXml child = CreateElement(newTag);
            if (!(new[] {"h1", "h2", "h3", "h4", "h5", "h6"}).Contains(tagName))
            {
                AppendChild(child);
                return child;
            }
            Parent.AppendChild(child);
            return child;
        }

        public MuPDFXml AddHorizontalLine()
        {
            MuPDFXml child = CreateElement("hr");
            AppendChild(child);
            return child;
        }

        public MuPDFXml AddImage(string name, string width = null, string height = null, string imgFloat = null, string align = null)
        {
            MuPDFXml child = CreateElement("img");
            if (width != null)
                child.SetAttribute("width", $"{width}");
            if (height != null)
                child.SetAttribute("height", $"{height}");
            if (imgFloat != null)
                child.SetAttribute("style", $"float: {imgFloat}");
            if (align != null)
                child.SetAttribute("align", $"{align}");
            child.SetAttribute("src", $"{name}");
            AppendChild(child);
            return child;
        }

        public MuPDFXml AddLink(string href, string text = null)
        {
            MuPDFXml child = CreateElement("a");
            if (text is string)
                text = href;
            child.SetAttribute("href", href);
            child.AppendChild(CreateTextNode(text));
            MuPDFXml prev = SpanBottom();
            if (prev == null)
                prev = this;
            prev.AppendChild(child);
            return this;
        }

        public MuPDFXml AddListItem()
        {
            if (TagName != "ol" || TagName != "ul")
                throw new Exception("cannot add list item");
            MuPDFXml child = CreateElement("li");
            AppendChild(child);
            return child;
        }

        public MuPDFXml AddNumberList(int start = 1, string numType = null)
        {
            MuPDFXml child = CreateElement("ol");
            if (start > 1)
                child.SetAttribute("start", Convert.ToString(start));
            if (numType != null)
                child.SetAttribute("type", numType);
            AppendChild(child);
            return child;
        }

        public MuPDFXml AddParagraph()
        {
            MuPDFXml child = CreateElement("p");
            if (TagName != "p")
                AppendChild(child);
            else
                Parent.AppendChild(child);
            return child;
        }

        public MuPDFXml AddSpan()
        {
            MuPDFXml child = CreateElement("span");
            AppendChild(child);
            return child;
        }

        public MuPDFXml AddStyle(string text)
        {
            string style = GetAttributeValue("style");
            if (style != null && style.Contains(text))
                return this;
            RemoveAttribute("style");
            if (style == null)
                style = Text;
            else
                style += ";" + Text;
            SetAttribute("style", style);
            return this;
        }

        public MuPDFXml AddSubscript(string text)
        {
            MuPDFXml child = CreateElement("sub");
            child.AppendChild(CreateTextNode(text));
            MuPDFXml prev = SpanBottom();
            if (prev == null)
                prev = this;
            prev.AppendChild(child);
            return this;
        }

        public MuPDFXml AddSuperscript(string text = null)
        {
            MuPDFXml child = CreateElement("sup");
            child.AppendChild(CreateTextNode(text));
            MuPDFXml prev = SpanBottom();
            if (prev == null)
                prev = this;
            prev.AppendChild(child);
            return this;
        }

        public MuPDFXml AddText(string text)
        {
            string[] lines = text.Split("\n");
            int lineCount = lines.Length;
            MuPDFXml prev = SpanBottom();
            if (prev == null)
                prev = this;

            for (int i = 0; i < lineCount; i++)
            {
                prev.AppendChild(CreateTextNode(lines[i]));
                if (i < lineCount - 1)
                    prev.AppendChild(CreateElement("br"));
            }
            return this;
        }

        public MuPDFXml AppendStyledSpan(string style)
        {
            MuPDFXml span = CreateElement("span");
            span.AddStyle(style);
            MuPDFXml prev = SpanBottom();
            if (prev == null)
                prev = this;
            prev.AppendChild(span);
            return prev;
        }

        public MuPDFXml BodyTag()
        {
            return new MuPDFXml(_nativeXml.fz_dom_body());
        }

        public MuPDFXml Clone()
        {
            return new MuPDFXml(_nativeXml.fz_dom_clone());
        }

        public static string ColorText(dynamic color)
        {
            if (color is string)
                return color;
            if (color is int)
            {
                (int, int, int) rgb = Utils.sRGB2rgb(color);
                return $"rgb({rgb.Item1},{rgb.Item2},{rgb.Item3})";
            }
            if ((color is Tuple || color is List<int> || color is List<float>))//issue: list length
            {
                return $"rbg({color[0]},{color[1]},{color[2]}";
            }
            return color;
        }

        public void Debug()
        {
            List < (int, string) > items = GetNodeTree();
            foreach ((int k, string v) in items)
            {
                Console.WriteLine($"{k}: " + v.Replace("\n", "\\n"));
            }
        }

        public MuPDFXml Find(string tag, string att, string match)
        {
            FzXml ret = _nativeXml.fz_dom_find(tag, att, match);
            if (ret != null)
                return new MuPDFXml(ret);
            return null;
        }

        public MuPDFXml FindNext(string tag, string att, string match)
        {
            FzXml ret = _nativeXml.fz_dom_find_next(tag, att, match);
            if (ret != null)
                return new MuPDFXml(ret);
            return null;
        }

        public void InsertAfter(MuPDFXml node)
        {
            _nativeXml.fz_dom_insert_after(node.ToFzXml());
        }

        public void InsertBefore(MuPDFXml node)
        {
            _nativeXml.fz_dom_insert_before(node.ToFzXml());
        }

        public MuPDFXml InsertText(string text)
        {
            string[] lines = text.Split("\n");
            int lineCount = lines.Length;

            for (int i = 0; i < lineCount; i++)
            {
                AppendChild(CreateTextNode(lines[i]));
                if (i < lineCount - 1)
                    AppendChild(CreateElement("br"));
            }
            return this;
        }

        public void Remove()
        {
            _nativeXml.fz_dom_remove();
        }

        public MuPDFXml SetAlign(dynamic align)
        {
            string text = "text-align: ";
            if (align is string)
                text += align;
            else if (align == Utils.TEXT_ALIGN_LEFT)
                text += "left";
            else if (align == Utils.TEXT_ALIGN_CENTER)
                text += "center";
            else if (align == Utils.TEXT_ALIGN_RIGHT)
                text += "right";
            else if (align == Utils.TEXT_ALIGN_JUSTIFY)
                text += "justify";
            else
                throw new Exception($"Unrecognised {align}");

            AddStyle(text);
            return this;
        }

        public MuPDFXml SetBgColor(dynamic color)
        {
            string text = $"backgroud-color: {MuPDFXml.ColorText(color)}";
            AddStyle(text);
            return this;
        }

        public MuPDFXml SetBold(bool isBold)
        {
            string text = "font-weight: ";
            if (isBold)
                text += "bold";
            else
                text += "normal";
            AppendStyledSpan(text);
            return this;
        }

        public MuPDFXml SetColor(dynamic color)
        {
            string text = $"color: {MuPDFXml.ColorText(color)}";
            AppendStyledSpan(text);
            return this;
        }

        public MuPDFXml SetColumns(int cols)
        {
            string text = $"columns: {cols}";
            AppendStyledSpan(text);
            return this;
        }

        public MuPDFXml SetFont(string font)
        {
            string text = $"font-family: {font}";
            AppendStyledSpan(text);
            return this;
        }

        public MuPDFXml SetFontSize(int fontSize)
        {
            string text = $"font-size: {fontSize}px";
            AppendStyledSpan(text);
            return this;
        }

        public MuPDFXml SetId(string unique)
        {
            MuPDFXml root = Root;
            if (root.Find(null, "id", unique) == null)
                throw new Exception($"id \'{unique}\' already exists");
            SetAttribute("id", unique);
            return this;
        }

        public MuPDFXml SetItalic(bool isItalic)
        {
            string text = "font-style: ";
            if (isItalic)
                text += "italic";
            else
                text += "normal";
            AppendStyledSpan(text);
            return this;
        }

        public MuPDFXml SetLeading(string leading)
        {
            string text = $"-mupdf-leading: {leading}";
            AddStyle(text);
            return this;
        }

        public MuPDFXml SetLetterSpacing(string spacing)
        {
            string text = $"leter-spacing: {spacing}";
            AppendStyledSpan(text);
            return this;
        }

        public MuPDFXml SetLineHeight(string lineHeight)
        {
            string text = $"line-height: {lineHeight}";
            AddStyle(text);
            return this;
        }

        public MuPDFXml SetMargins(string val)
        {
            string text = $"margin: {val}";
            AppendStyledSpan(text);
            return this;
        }

        public MuPDFXml SetOpacity(string opacity)
        {
            string text = $"opacity: {opacity}";
            AppendStyledSpan(text);
            return this;
        }

        public MuPDFXml SetPageBreakAfter()
        {
            string text = "page-break-after: always";
            AddStyle(text);
            return this;
        }

        public MuPDFXml SetPageBreakBefore()
        {
            string text = "page-break-before: always";
            AddStyle(text);
            return this;
        }

        public MuPDFXml SetProperties(
            string align = null,
            string bgcolor = null,
            bool bold = false,
            string color = null,
            int columns = 0,
            string font = null,
            int fontSize = 10,
            bool italic = false,
            string indent = null,
            string leading = null,
            string letterSpacing = null,
            string lineHeight = null,
            string margins = null,
            string pageBreakAfter = null,
            string pageBreakBefore = null,
            string wordSpacing = null,
            string unqid = null,
            string cls = null
            )
        {
            MuPDFXml root = Root;
            MuPDFXml temp = root.AddDivision();
            if (align != null)
            {
                temp.SetAlign(align);
            }
            if (bgcolor != null)
            {
                temp.SetBgColor(bgcolor);
            }
            if (bold)
            {
                temp.SetBold(bold);
            }
            if (color != null)
            {
                temp.SetColor(color);
            }
            if (columns != 0)
            {
                temp.SetColumns(columns);
            }
            if (font != null)
            {
                temp.SetFont(font);
            }
            if (fontSize != 10)
            {
                temp.SetFontSize(fontSize);
            }
            if (indent != null)
            {
                temp.SetTextIndent(indent);
            }
            if (italic)
            {
                temp.SetItalic(italic);
            }
            if (leading != null)
            {
                temp.SetLeading(leading);
            }
            if (letterSpacing != null)
            {
                temp.SetLetterSpacing(letterSpacing);
            }
            if (lineHeight != null)
            {
                temp.SetLineHeight(lineHeight);
            }
            if (margins != null)
            {
                temp.SetMargins(margins);
            }
            if (pageBreakAfter != null)
            {
                temp.SetPageBreakAfter();
            }
            if (pageBreakBefore != null)
            {
                temp.SetPageBreakBefore();
            }
            if (wordSpacing != null)
            {
                temp.SetWordSpacing(wordSpacing);
            }
            if (unqid != null)
            {
                this.SetId(unqid);
            }
            if (cls != null)
            {
                this.AddClass(cls);
            }

            List<string> styles = new List<string>();
            string topStyle = GetAttributeValue("style");
            if (topStyle != null)
                styles.Add(topStyle);
            MuPDFXml child = temp.FirstChild;
            while (child != null)
            {
                styles.Add(child.GetAttributeValue("style"));
                child = child.FirstChild;
            }
            SetAttribute("style", string.Join(";", styles.ToArray()));
            temp.Remove();
            return this;
        }

        public MuPDFXml SetTextIndent(string indent)
        {
            string text = $"text-indent: {indent}";
            AddStyle(text);
            return this;
        }

        public MuPDFXml SetUnderline(string val = "underline")
        {
            string text = $"text-decoration: {val}";
            AppendStyledSpan(text);
            return this;
        }

        public MuPDFXml SetWordSpacing(string spacing)
        {
            string text = $"text-spacing: {spacing}";
            AppendStyledSpan(text);
            return this;
        }

        public Dictionary<string, string> GetAttributes()
        {
            if (!IsText)
                return null;
            Dictionary<string, string> ret = new Dictionary<string, string>();
            int i = 0;
            while (true)
            {
                (string val, string attr) = _nativeXml.fz_dom_get_attribute(i);
                if (val == null || attr == null)
                    break;
                ret.Add(attr, val);
                i += 1;
            }
            return ret;
        }

        public MuPDFXml GetBodyTag()
        {
            return new MuPDFXml(_nativeXml.fz_dom_body());
        }

        public string GetAttributeValue(string attr)
        {
            return _nativeXml.fz_dom_attribute(attr);
        }

        public void RemoveAttribute(string attr)
        {
            _nativeXml.fz_dom_remove_attribute(attr);
        }

        public void SetAttribute(string attr, string value)
        {
            _nativeXml.fz_dom_add_attribute(attr, value);
        }

        public MuPDFXml SpanBottom()
        {
            MuPDFXml parent = this;
            MuPDFXml child = LastChild;
            if (child == null)
                return null;
            while (child.IsText)
            {
                child = child.Previous;
                if (child == null)
                    break;
            }

            if (child == null || child.TagName != "span")
                return null;

            while (true)
            {
                if (child == null)
                    return parent;
                if ((new List<string>() { "a", "sub", "sup", "body" }).Contains(child.TagName))
                {
                    child = child.Next;
                    continue;
                }

                if (child.TagName == "span")
                {
                    parent = child;
                    child = child.FirstChild;
                }
                else
                    return parent;
            }
        }

        public void Dispose()
        {
            _nativeXml.Dispose();
        }
    }
}
