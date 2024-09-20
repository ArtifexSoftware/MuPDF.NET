using System.Runtime.InteropServices;
using System.Text;
using mupdf;

namespace MuPDF.NET
{
    public class Xml
    {
        static Xml()
        {
            Utils.InitApp();
        }

        private FzXml _nativeXml;

        public FzXml ToFzXml()
        {
            return _nativeXml;
        }

        public Xml(FzXml rhs)
        {
            _nativeXml = rhs;
        }

        /// <summary>
        /// Either the node’s text or Null if a tag node.
        /// </summary>
        public string Text
        {
            get { return _nativeXml.fz_xml_text(); }
        }

        /// <summary>
        /// Check if a text node.
        /// </summary>
        public bool IsText
        {
            get { return Text != null; }
        }

        /// <summary>
        /// Either the HTML tag name like p or Null if a text node.
        /// </summary>
        public string TagName
        {
            get { return _nativeXml.fz_xml_tag(); }
        }

        /// <summary>
        /// The top node of the DOM, which hence has the tagname
        /// </summary>
        public Xml Root
        {
            get { return new Xml(_nativeXml.fz_xml_root()); }
        }

        /// <summary>
        /// The previous node at the same level.
        /// </summary>
        public Xml Previous
        {
            get
            {
                FzXml ret = _nativeXml.fz_dom_previous();
                if (ret != null)
                    return new Xml(ret);
                else
                    return null;
            }
        }

        /// <summary>
        /// The next node at the same level (or None).
        /// </summary>
        public Xml Next
        {
            get
            {
                FzXml ret = _nativeXml.fz_dom_next();
                if (ret != null)
                    return new Xml(ret);
                else
                    return null;
            }
        }

        public Xml Parent
        {
            get
            {
                FzXml ret = _nativeXml.fz_dom_parent();
                if (ret != null)
                    return new Xml(ret);
                else
                    return null;
            }
        }

        public Xml FirstChild
        {
            get
            {
                if (_nativeXml.fz_xml_text() == null)
                    return null;
                FzXml ret = _nativeXml.fz_dom_first_child();
                if (ret.m_internal != null)
                    return new Xml(ret);
                else
                    return null;
            }
        }

        public Xml LastChild
        {
            get
            {
                Xml child = FirstChild;
                if (child == null)
                    return null;
                while (true)
                {
                    Xml next = child.Next;
                    if (next == null)
                        return child;
                    child = next;
                }
            }
        }

        public Xml(string rhs)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(rhs);
            FzBuffer buf = Utils.fz_new_buffer_from_data(bytes);
            _nativeXml = mupdf.mupdf.fz_parse_xml_from_html5(buf);
        }

        private List<(int, string)> ShowNode(Xml node, List<(int, string)> items, int shift)
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
                Xml child = node.FirstChild;
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

        /// <summary>
        /// Create a new node with a given tag. This a low-level method used by other methods like
        /// </summary>
        /// <param name="tag">the element tag.</param>
        /// <returns>the created element. To actually bind it to the DOM</returns>
        public Xml CreateElement(string tag)
        {
            return new Xml(_nativeXml.fz_dom_create_element(tag));
        }

        /// <summary>
        /// Create direct text for the current node.
        /// </summary>
        /// <param name="text">the text to append.</param>
        /// <returns>the created element.</returns>
        public Xml CreateTextNode(string text)
        {
            return new Xml(_nativeXml.fz_dom_create_text_node(text));
        }

        /// <summary>
        /// Append a child node.
        /// </summary>
        /// <param name="child">the Xml node to append.</param>
        public void AppendChild(Xml child)
        {
            _nativeXml.fz_dom_append_child(child.ToFzXml());
        }

        /// <summary>
        /// Add an ul tag - bulleted list, context manager.
        /// </summary>
        /// <returns></returns>
        public Xml AddBulletList()
        {
            Xml child = CreateElement("ul");
            AppendChild(child);
            return child;
        }

        /// <summary>
        /// Set (add) some “class” attribute.
        /// </summary>
        /// <param name="text">the name of the class. Must have been defined in either the HTML or the CSS source of the DOM.</param>
        /// <returns></returns>
        public Xml AddClass(string text)
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

        /// <summary>
        /// Add “code” text (code tag) - inline element, treated like text.
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public Xml AddCode(string text = null)
        {
            Xml child = CreateElement("code");
            AppendChild(CreateTextNode(text));
            Xml prev = SpanBottom();
            if (prev != null)
                prev = this;
            prev.AppendChild(child);
            return this;
        }

        public Xml AddVar(string text = null)
        {
            return AddCode(text);
        }

        public Xml AddSamp(string text = null)
        {
            return AddCode(text);
        }

        public Xml AddKbd(string text = null)
        {
            return AddCode(text);
        }

        /// <summary>
        /// Add a pre tag, context manager.
        /// </summary>
        /// <returns></returns>
        public Xml AddCodeBlock()
        {
            Xml child = CreateElement("pre");
            AppendChild(child);
            return child;
        }

        /// <summary>
        /// Add a dl tag, context manager.
        /// </summary>
        /// <returns></returns>
        public Xml AddDescriptionList()
        {
            Xml child = CreateElement("dl");
            AppendChild(child);
            return child;
        }

        /// <summary>
        /// Add a div tag, context manager.
        /// </summary>
        /// <returns></returns>
        public Xml AddDivision()
        {
            Xml child = CreateElement("div");
            AppendChild(child);
            return child;
        }

        /// <summary>
        /// Add a header tag (one of h1 to h6), context manager.
        /// </summary>
        /// <param name="level"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public Xml AddHeader(int level = 1)
        {
            if (!Utils.INRANGE(level, 1, 6))
                throw new Exception("Header level must be in [1, 6]");
            string tagName = TagName;
            string newTag = $"h{level}";
            Xml child = CreateElement(newTag);
            if (!(new[] { "h1", "h2", "h3", "h4", "h5", "h6" }).Contains(tagName))
            {
                AppendChild(child);
                return child;
            }
            Parent.AppendChild(child);
            return child;
        }

        /// <summary>
        /// Add a hr tag.
        /// </summary>
        /// <returns></returns>
        public Xml AddHorizontalLine()
        {
            Xml child = CreateElement("hr");
            AppendChild(child);
            return child;
        }

        /// <summary>
        /// Add an img tag. This causes the inclusion of the named image in the DOM.
        /// </summary>
        /// <param name="name">the filename of the image. This must be the member name of some entry of the MuPDFArchive parameter of the MuPDFStory constructor.</param>
        /// <param name="width">if provided, either an absolute (int) value, or a percentage string like “30%”.</param>
        /// <param name="height"> if provided, either an absolute (int) value, or a percentage string like “30%”.</param>
        /// <param name="imgFloat"></param>
        /// <param name="align"></param>
        /// <returns></returns>
        public Xml AddImage(
            string name,
            string width = null,
            string height = null,
            string imgFloat = null,
            string align = null
        )
        {
            Xml child = CreateElement("img");
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

        /// <summary>
        /// Add an a tag - inline element, treated like text.
        /// </summary>
        /// <param name="href">the URL target.</param>
        /// <param name="text">the text to display. If omitted, the href text is shown instead.</param>
        /// <returns></returns>
        public Xml AddLink(string href, string text = null)
        {
            Xml child = CreateElement("a");
            if (text is string)
                text = href;
            child.SetAttribute("href", href);
            child.AppendChild(CreateTextNode(text));
            Xml prev = SpanBottom();
            if (prev == null)
                prev = this;
            prev.AppendChild(child);
            return this;
        }

        /// <summary>
        /// Add an ol tag, context manager.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public Xml AddListItem()
        {
            if (TagName != "ol" || TagName != "ul")
                throw new Exception("cannot add list item");
            Xml child = CreateElement("li");
            AppendChild(child);
            return child;
        }

        /// <summary>
        /// Add an ol tag, context manager.
        /// </summary>
        /// <param name="start"></param>
        /// <param name="numType"></param>
        /// <returns></returns>
        public Xml AddNumberList(int start = 1, string numType = null)
        {
            Xml child = CreateElement("ol");
            if (start > 1)
                child.SetAttribute("start", Convert.ToString(start));
            if (numType != null)
                child.SetAttribute("type", numType);
            AppendChild(child);
            return child;
        }

        /// <summary>
        /// Add a <b>p</b> tag, context manager.
        /// </summary>
        /// <returns></returns>
        public Xml AddParagraph()
        {
            Xml child = CreateElement("p");
            if (TagName != "p")
                AppendChild(child);
            else
                Parent.AppendChild(child);
            return child;
        }

        /// <summary>
        /// Add a span tag, context manager.
        /// </summary>
        /// <returns></returns>
        public Xml AddSpan()
        {
            Xml child = CreateElement("span");
            AppendChild(child);
            return child;
        }

        /// <summary>
        /// Set (add) some style attribute not supported by its own set_ method.
        /// </summary>
        /// <param name="text">any valid CSS style value.</param>
        /// <returns></returns>
        public Xml AddStyle(string text)
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

        /// <summary>
        /// Add “subscript” text(sub tag) - inline element, treated like text.
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public Xml AddSubscript(string text)
        {
            Xml child = CreateElement("sub");
            child.AppendChild(CreateTextNode(text));
            Xml prev = SpanBottom();
            if (prev == null)
                prev = this;
            prev.AppendChild(child);
            return this;
        }

        /// <summary>
        /// Add “superscript” text (sup tag) - inline element, treated like text.
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public Xml AddSuperscript(string text = null)
        {
            Xml child = CreateElement("sup");
            child.AppendChild(CreateTextNode(text));
            Xml prev = SpanBottom();
            if (prev == null)
                prev = this;
            prev.AppendChild(child);
            return this;
        }

        /// <summary>
        /// Add a text string. Line breaks n are honored as br tags.
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public Xml AddText(string text)
        {
            string[] lines = text.Split("\n");
            int lineCount = lines.Length;
            Xml prev = SpanBottom();
            if (prev == null)
                prev = this;

            for (int i = 0; i < lineCount; i++)
            {
                prev.AppendChild(CreateTextNode(lines[i]));
                if (i < lineCount - 1)
                {
                    Xml br = CreateElement("br");
                    prev.AppendChild(br);
                }
            }
            return this;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="style"></param>
        /// <returns></returns>
        public Xml AppendStyledSpan(string style)
        {
            Xml span = CreateElement("span");
            span.AddStyle(style);
            Xml prev = SpanBottom();
            if (prev == null)
                prev = this;
            prev.AppendChild(span);
            return prev;
        }

        public Xml Clone()
        {
            return new Xml(_nativeXml.fz_dom_clone());
        }

        public static string ColorText(dynamic color)
        {
            if (color is string)
                return color;
            if (color is int)
            {
                (int, int, int) rgb = Utils.sRGB2Rgb(color);
                return $"rgb({rgb.Item1},{rgb.Item2},{rgb.Item3})";
            }
            if ((color is float[] && color.Length == 3))
            {
                return $"rbg({color[0]},{color[1]},{color[2]}";
            }
            return color;
        }

        /// <summary>
        /// For debugging purposes, print this node’s structure in a simplified form.
        /// </summary>
        public void Debug()
        {
            List<(int, string)> items = GetNodeTree();
            foreach ((int k, string v) in items)
            {
                Console.WriteLine($"{k}: " + v.Replace("\n", "\\n"));
            }
        }

        /// <summary>
        /// Under the current node, find the first node with the given tag, attribute att and value match.
        /// </summary>
        /// <param name="tag">restrict search to this tag. May be null for unrestricted searches.</param>
        /// <param name="att">check this attribute. May be None.</param>
        /// <param name="match">the desired attribute value to match. May be null.</param>
        /// <returns></returns>
        public Xml Find(string tag, string att, string match)
        {
            FzXml ret = _nativeXml.fz_dom_find(tag, att, match);
            if (ret != null)
                return new Xml(ret);
            return null;
        }

        /// <summary>
        /// Continue a previous Xml.find() (or find_next()) with the same values.
        /// </summary>
        /// <param name="tag"></param>
        /// <param name="att"></param>
        /// <param name="match"></param>
        /// <returns>Null if null more found, otherwise the next matching node.</returns>
        public Xml FindNext(string tag, string att, string match)
        {
            FzXml ret = _nativeXml.fz_dom_find_next(tag, att, match);
            if (ret != null)
                return new Xml(ret);
            return null;
        }

        /// <summary>
        /// Insert the given element elem after this node.
        /// </summary>
        /// <param name="node"></param>
        public void InsertAfter(Xml node)
        {
            _nativeXml.fz_dom_insert_after(node.ToFzXml());
        }

        /// <summary>
        /// Insert the given element elem before this node.
        /// </summary>
        /// <param name="node"></param>
        public void InsertBefore(Xml node)
        {
            _nativeXml.fz_dom_insert_before(node.ToFzXml());
        }

        public Xml InsertText(string text)
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

        /// <summary>
        /// Set the text alignment. Only works for block-level tags.
        /// </summary>
        /// <param name="align">either one of the Text Alignment or the text-align values.</param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public Xml SetAlign(dynamic align)
        {
            string text = "text-align: ";
            if (align is string)
                text += align;
            else if (align == TextAlign.TEXT_ALIGN_LEFT)
                text += "left";
            else if (align == TextAlign.TEXT_ALIGN_CENTER)
                text += "center";
            else if (align == TextAlign.TEXT_ALIGN_RIGHT)
                text += "right";
            else if (align == TextAlign.TEXT_ALIGN_JUSTIFY)
                text += "justify";
            else
                throw new Exception($"Unrecognised {align}");

            AddStyle(text);
            return this;
        }

        /// <summary>
        /// Set the background color. Only works for block-level tags.
        /// </summary>
        /// <param name="color">either an RGB value like (255, 0, 0) (for “red”) or a valid background-color value.</param>
        /// <returns></returns>
        public Xml SetBgColor(int color)
        {
            string text = $"backgroud-color: {Xml.ColorText(color)}";
            AddStyle(text);
            return this;
        }

        /// <summary>
        /// Set the background color. Only works for block-level tags.
        /// </summary>
        /// <param name="color">either an RGB value like (255, 0, 0) (for “red”) or a valid background-color value.</param>
        /// <returns></returns>
        public Xml SetBgColor(float[] color)
        {
            string text = $"backgroud-color: {Xml.ColorText(color)}";
            AddStyle(text);
            return this;
        }

        /// <summary>
        /// Set the background color. Only works for block-level tags.
        /// </summary>
        /// <param name="color">either an RGB value like (255, 0, 0) (for “red”) or a valid background-color value.</param>
        /// <returns></returns>
        public Xml SetBgColor(string color)
        {
            string text = $"backgroud-color: {Xml.ColorText(color)}";
            AddStyle(text);
            return this;
        }

        /// <summary>
        /// Set bold on or off or to some string value.
        /// </summary>
        /// <param name="isBold">True, False or a valid font-weight value.</param>
        /// <returns></returns>
        public Xml SetBold(bool isBold)
        {
            string text = "font-weight: ";
            if (isBold)
                text += "bold";
            else
                text += "normal";
            AppendStyledSpan(text);
            return this;
        }

        /// <summary>
        /// Set the color of the text following.
        /// </summary>
        /// <param name="color">either an RGB value like (255, 0, 0) (for “red”) or a valid color value.</param>
        /// <returns></returns>
        public Xml SetColor(dynamic color)
        {
            string text = $"color: {Xml.ColorText(color)}";
            AppendStyledSpan(text);
            return this;
        }

        /// <summary>
        /// Set the number of columns.
        /// </summary>
        /// <param name="cols">a valid columns value.</param>
        /// <returns></returns>
        public Xml SetColumns(int cols)
        {
            string text = $"columns: {cols}";
            AppendStyledSpan(text);
            return this;
        }

        /// <summary>
        /// Set the font-family.
        /// </summary>
        /// <param name="font"></param>
        /// <returns></returns>
        public Xml SetFont(string font)
        {
            string text = $"font-family: {font}";
            AppendStyledSpan(text);
            return this;
        }

        /// <summary>
        /// Set the font size for text following.
        /// </summary>
        /// <param name="fontSize">a float or a valid font-size value.</param>
        /// <returns></returns>
        public Xml SetFontSize(int fontSize)
        {
            string text = $"font-size: {fontSize}px";
            AppendStyledSpan(text);
            return this;
        }

        /// <summary>
        /// Set a id. This serves as a unique identification of the node within the DOM.
        /// </summary>
        /// <param name="unique">id string of the node.</param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public Xml SetId(string unique)
        {
            Xml root = Root;
            if (root.Find(null, "id", unique) == null)
                throw new Exception($"id \'{unique}\' already exists");
            SetAttribute("id", unique);
            return this;
        }

        /// <summary>
        /// Set italic on or off or to some string value for the text following it.
        /// </summary>
        /// <param name="isItalic">True, False or some valid font-style value.</param>
        /// <returns></returns>
        public Xml SetItalic(bool isItalic)
        {
            string text = "font-style: ";
            if (isItalic)
                text += "italic";
            else
                text += "normal";
            AppendStyledSpan(text);
            return this;
        }

        /// <summary>
        /// Set inter-block text distance (-mupdf-leading), only works on block-level nodes.
        /// </summary>
        /// <param name="leading">the distance in points to the previous block.</param>
        /// <returns></returns>
        public Xml SetLeading(string leading)
        {
            string text = $"-mupdf-leading: {leading}";
            AddStyle(text);
            return this;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="spacing"></param>
        /// <returns></returns>
        public Xml SetLetterSpacing(string spacing)
        {
            string text = $"leter-spacing: {spacing}";
            AppendStyledSpan(text);
            return this;
        }

        /// <summary>
        /// Set height of a line.
        /// </summary>
        /// <param name="lineHeight">a float like 1.5 (which sets to 1.5 * fontsize), or some valid line-height value.</param>
        /// <returns></returns>
        public Xml SetLineHeight(string lineHeight)
        {
            string text = $"line-height: {lineHeight}";
            AddStyle(text);
            return this;
        }

        /// <summary>
        /// Set the margin(s).
        /// </summary>
        /// <param name="val">float or string with up to 4 values.</param>
        /// <returns></returns>
        public Xml SetMargins(string val)
        {
            string text = $"margin: {val}";
            AppendStyledSpan(text);
            return this;
        }

        /// <summary>
        /// Set opacity
        /// </summary>
        /// <param name="opacity"></param>
        /// <returns></returns>
        public Xml SetOpacity(string opacity)
        {
            string text = $"opacity: {opacity}";
            AppendStyledSpan(text);
            return this;
        }

        /// <summary>
        /// Insert a page break after this node.
        /// </summary>
        /// <returns></returns>
        public Xml SetPageBreakAfter()
        {
            string text = "page-break-after: always";
            AddStyle(text);
            return this;
        }

        /// <summary>
        /// Insert a page break before this node.
        /// </summary>
        /// <returns></returns>
        public Xml SetPageBreakBefore()
        {
            string text = "page-break-before: always";
            AddStyle(text);
            return this;
        }

        /// <summary>
        /// Set any or all desired properties in one call. The meaning of argument values equal the values of the corresponding set_ methods.
        /// </summary>
        /// <param name="align"></param>
        /// <param name="bgcolor"></param>
        /// <param name="bold"></param>
        /// <param name="color"></param>
        /// <param name="columns"></param>
        /// <param name="font"></param>
        /// <param name="fontSize"></param>
        /// <param name="italic"></param>
        /// <param name="indent"></param>
        /// <param name="leading"></param>
        /// <param name="letterSpacing"></param>
        /// <param name="lineHeight"></param>
        /// <param name="margins"></param>
        /// <param name="pageBreakAfter"></param>
        /// <param name="pageBreakBefore"></param>
        /// <param name="wordSpacing"></param>
        /// <param name="unqid"></param>
        /// <param name="cls"></param>
        /// <returns></returns>
        public Xml SetProperties(
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
            Xml root = Root;
            Xml temp = root.AddDivision();
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
            Xml child = temp.FirstChild;
            while (child != null)
            {
                styles.Add(child.GetAttributeValue("style"));
                child = child.FirstChild;
            }
            SetAttribute("style", string.Join(";", styles.ToArray()));
            temp.Remove();
            return this;
        }

        /// <summary>
        /// Set indentation for the first textblock line. Only works for block-level nodes.
        /// </summary>
        /// <param name="indent"></param>
        /// <returns></returns>
        public Xml SetTextIndent(string indent)
        {
            string text = $"text-indent: {indent}";
            AddStyle(text);
            return this;
        }

        public Xml SetUnderline(string val = "underline")
        {
            string text = $"text-decoration: {val}";
            AppendStyledSpan(text);
            return this;
        }

        public Xml SetWordSpacing(string spacing)
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

        public Xml GetBodyTag()
        {
            return new Xml(_nativeXml.fz_dom_body());
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

        public Xml SpanBottom()
        {
            Xml parent = this;
            Xml child = LastChild;
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
    }
}
