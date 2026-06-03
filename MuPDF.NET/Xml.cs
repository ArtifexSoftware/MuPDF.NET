using System;
using System.Collections.Generic;
using mupdf;

namespace MuPDF.NET
{
    /// <summary>
    /// HTML/XML DOM node for <see cref="Story"/> content (PyMuPDF <c>class Xml</c>).
    /// </summary>
    /// <remarks>
    /// <para>Obtain the document body via <see cref="Story.Body"/> rather than constructing a root node yourself.</para>
    /// <para>Block helpers such as <see cref="AddParagraph"/> return the new child so you can chain
    /// <see cref="SetBold(bool)"/>, <see cref="AddText"/>, and similar calls on that node.</para>
    /// <para>Tree nodes returned by navigation properties and <see cref="Find"/> are views into the
    /// story DOM and are not owned by the wrapper — see <see cref="FromDomNode"/>.</para>
    /// </remarks>
    public class Xml
    {
        internal FzXml This { get; }

        internal static bool HasInternal(FzXml? xml) =>
            xml?.m_internal != null;

        /// <summary>
        /// Wrap a DOM node returned by MuPDF without taking ownership (PyMuPDF <c>Xml(ret)</c> on tree nodes).
        /// </summary>
        internal static Xml? FromDomNode(FzXml? ret)
        {
            if (ret?.m_internal == null)
                return null;
            var h = FzXml.getCPtr(ret).Handle;
            if (h == IntPtr.Zero)
                return null;
            return new Xml(new FzXml(h, false));
        }

        /// <summary>Wrap an existing <see cref="FzXml"/> handle (PyMuPDF <c>Xml(rhs)</c> when rhs is FzXml).</summary>
        public Xml(FzXml rhs) =>
            This = rhs ?? throw new ArgumentNullException(nameof(rhs));

        /// <summary>Parse HTML5 from a string (PyMuPDF <c>Xml(rhs)</c> when rhs is str).</summary>
        public Xml(string rhs)
        {
            var buff = Helpers.BufferFromBytes(System.Text.Encoding.UTF8.GetBytes(rhs ?? ""));
            This = buff.fz_parse_xml_from_html5();
        }

        /// <summary>Text content of a text node; <c>null</c> for element nodes (PyMuPDF <c>Xml.text</c>).</summary>
        public string? Text => This.fz_xml_text();

        /// <summary><c>true</c> if this node carries text rather than a tag (PyMuPDF <c>Xml.is_text</c>).</summary>
        public bool IsText => Text != null;

        /// <summary>HTML tag name (e.g. <c>p</c>), or <c>null</c> on text nodes (PyMuPDF <c>Xml.tagname</c>).</summary>
        public string? TagName => This.fz_xml_tag();

        /// <summary>Document root element (typically <c>html</c>) (PyMuPDF <c>Xml.root</c>).</summary>
        public Xml? Root => FromDomNode(This.fz_xml_root());

        /// <summary>Previous sibling at the same level, or <c>null</c> (PyMuPDF <c>Xml.previous</c>).</summary>
        public Xml? Previous => FromDomNode(This.fz_dom_previous());

        /// <summary>Next sibling at the same level, or <c>null</c> (PyMuPDF <c>Xml.next</c>).</summary>
        public Xml? Next => FromDomNode(This.fz_dom_next());

        /// <summary>Parent element, or <c>null</c> (PyMuPDF <c>Xml.parent</c>).</summary>
        public Xml? Parent => FromDomNode(This.fz_dom_parent());

        /// <summary>First child node, or <c>null</c>; text nodes have no children (PyMuPDF <c>Xml.first_child</c>).</summary>
        public Xml? FirstChild
        {
            get
            {
                if (Text != null)
                    return null;
                return FromDomNode(This.fz_dom_first_child());
            }
        }

        /// <summary>Last child among direct children, or <c>null</c> (PyMuPDF <c>Xml.last_child</c>).</summary>
        public Xml? LastChild
        {
            get
            {
                var child = FirstChild;
                if (child == null)
                    return null;
                while (true)
                {
                    var next = child.Next;
                    if (next == null)
                        return child;
                    child = next;
                }
            }
        }

        /// <summary>Return the document <c>body</c> element (PyMuPDF <c>Xml.bodytag</c>).</summary>
        public Xml? BodyTag() => FromDomNode(This.fz_dom_body());

        /// <summary>
        /// Find the first descendant matching tag/attribute (PyMuPDF <c>Xml.find</c>).
        /// </summary>
        /// <param name="tag">Element tag, or <c>null</c> for any tag.</param>
        /// <param name="att">Attribute name, or <c>null</c>.</param>
        /// <param name="match">Attribute value to match, or <c>null</c>.</param>
        public Xml? Find(string? tag, string? att, string? match) =>
            FromDomNode(This.fz_dom_find(tag, att, match));

        /// <summary>Continue <see cref="Find"/> with the same criteria (PyMuPDF <c>Xml.find_next</c>).</summary>
        public Xml? FindNext(string? tag, string? att, string? match) =>
            FromDomNode(This.fz_dom_find_next(tag, att, match));

        /// <summary>All attributes of an element node; <c>null</c> on text nodes (PyMuPDF <c>Xml.get_attributes</c>).</summary>
        public Dictionary<string, string>? GetAttributes()
        {
            if (IsText)
                return null;
            var result = new Dictionary<string, string>();
            for (int i = 0; ; i++)
            {
                var (val, key) = This.fz_dom_get_attribute(i);
                if (string.IsNullOrEmpty(val) || string.IsNullOrEmpty(key))
                    break;
                result[key] = val;
            }
            return result;
        }

        /// <summary>Read a DOM attribute (PyMuPDF <c>Xml.get_attribute_value</c>).</summary>
        public string? GetAttributeValue(string key)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentException("key must be non-empty", nameof(key));
            return This.fz_dom_attribute(key);
        }

        /// <summary>Add or replace a DOM attribute (PyMuPDF <c>Xml.set_attribute</c>).</summary>
        public void SetAttribute(string key, string? value = null)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentException("key must be non-empty", nameof(key));
            This.fz_dom_add_attribute(key, value ?? "");
        }

        /// <summary>Remove attribute <paramref name="key"/> (PyMuPDF <c>Xml.remove_attribute</c>).</summary>
        public void RemoveAttribute(string key)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentException("key must be non-empty", nameof(key));
            This.fz_dom_remove_attribute(key);
        }

        /// <summary>Create an element node; bind with <see cref="AppendChild"/> (PyMuPDF <c>Xml.create_element</c>).</summary>
        public Xml CreateElement(string tag) => new Xml(This.fz_dom_create_element(tag));

        /// <summary>Create a text node (PyMuPDF <c>Xml.create_text_node</c>).</summary>
        public Xml CreateTextNode(string text) => new Xml(This.fz_dom_create_text_node(text));

        /// <summary>Append <paramref name="child"/> under this node (PyMuPDF <c>Xml.append_child</c>).</summary>
        public void AppendChild(Xml child) =>
            This.fz_dom_append_child(child.This);

        /// <summary>Insert <paramref name="node"/> before this node (PyMuPDF <c>Xml.insert_before</c>).</summary>
        public void InsertBefore(Xml node) => This.fz_dom_insert_before(node.This);

        /// <summary>Insert <paramref name="node"/> after this node (PyMuPDF <c>Xml.insert_after</c>).</summary>
        public void InsertAfter(Xml node) => This.fz_dom_insert_after(node.This);

        /// <summary>Detach this node from the DOM (PyMuPDF <c>Xml.remove</c>).</summary>
        public void Remove() => This.fz_dom_remove();

        /// <summary>Deep copy of this node (PyMuPDF <c>Xml.clone</c>).</summary>
        public Xml Clone() => new Xml(This.fz_dom_clone());

        /// <summary>Convert a color argument to a CSS color string (PyMuPDF <c>Xml.color_text</c>).</summary>
        public static string ColorText(object color)
        {
            if (color is string s)
                return s;
            if (color is int i)
            {
                var (r, g, b) = SRgbToRgb(i);
                return $"rgb({r},{g},{b})";
            }
            if (color is float[] rgb && rgb.Length == 3)
                return $"rgb({rgb[0]},{rgb[1]},{rgb[2]})";
            if (color is double[] drgb && drgb.Length == 3)
                return $"rgb({drgb[0]},{drgb[1]},{drgb[2]})";
            return color?.ToString() ?? "";
        }

        /// <summary>Print a simplified node tree (PyMuPDF <c>Xml.debug</c>).</summary>
        public void Debug()
        {
            foreach (var item in GetNodeTree())
                Helpers.message(new string(' ', item.shift * 2) + EscapeDebugLine(item.line));
        }

        // ─── Block structure ─────────────────────────────────────────────

        /// <summary>Add a bulleted list (<c>ul</c>) (PyMuPDF <c>Xml.add_bullet_list</c>).</summary>
        public Xml AddBulletList()
        {
            var child = CreateElement("ul");
            AppendChild(child);
            return child;
        }

        /// <summary>Add a numbered list (<c>ol</c>) (PyMuPDF <c>Xml.add_number_list</c>).</summary>
        public Xml AddNumberList(int start = 1, string? numType = null)
        {
            var child = CreateElement("ol");
            if (start > 1)
                child.SetAttribute("start", start.ToString());
            if (numType != null)
                child.SetAttribute("type", numType);
            AppendChild(child);
            return child;
        }

        /// <summary>Add a list item (<c>li</c>) under <c>ol</c> or <c>ul</c> (PyMuPDF <c>Xml.add_list_item</c>).</summary>
        public Xml AddListItem()
        {
            var tag = TagName;
            if (tag != "ol" && tag != "ul")
                throw new ValueErrorException($"cannot add list item to {tag}");
            var child = CreateElement("li");
            AppendChild(child);
            return child;
        }

        /// <summary>Add a paragraph (<c>p</c>) (PyMuPDF <c>Xml.add_paragraph</c>).</summary>
        public Xml AddParagraph()
        {
            var child = CreateElement("p");
            if (TagName != "p")
                AppendChild(child);
            else
                Parent!.AppendChild(child);
            return child;
        }

        /// <summary>Add a division (<c>div</c>) (PyMuPDF <c>Xml.add_division</c>).</summary>
        public Xml AddDivision()
        {
            var child = CreateElement("div");
            AppendChild(child);
            return child;
        }

        /// <summary>Add a description list (<c>dl</c>) (PyMuPDF <c>Xml.add_description_list</c>).</summary>
        public Xml AddDescriptionList()
        {
            var child = CreateElement("dl");
            AppendChild(child);
            return child;
        }

        /// <summary>Add a preformatted block (<c>pre</c>) (PyMuPDF <c>Xml.add_codeblock</c>).</summary>
        public Xml AddCodeBlock()
        {
            var child = CreateElement("pre");
            AppendChild(child);
            return child;
        }

        /// <summary>Add a horizontal rule (<c>hr</c>) (PyMuPDF <c>Xml.add_horizontal_line</c>).</summary>
        public Xml AddHorizontalLine()
        {
            var child = CreateElement("hr");
            AppendChild(child);
            return child;
        }

        /// <summary>Add a span wrapper (PyMuPDF <c>Xml.add_span</c>).</summary>
        public Xml AddSpan()
        {
            var child = CreateElement("span");
            AppendChild(child);
            return child;
        }

        /// <summary>Add a heading <c>h1</c>–<c>h6</c> (PyMuPDF <c>Xml.add_header</c>).</summary>
        /// <param name="level">Heading level from 1 to 6.</param>
        public Xml AddHeader(int level = 1)
        {
            if (!Helpers.InRange(level, 1, 6))
                throw new ValueErrorException("Header level must be in [1, 6]");
            var thisTag = TagName;
            var newTag = $"h{level}";
            var child = CreateElement(newTag);
            if (thisTag is not ("h1" or "h2" or "h3" or "h4" or "h5" or "h6" or "p"))
                AppendChild(child);
            else
                Parent!.AppendChild(child);
            return child;
        }

        /// <summary>
        /// Add an image (<c>img</c>). <paramref name="name"/> must match an archive entry used by <see cref="Story"/>.
        /// </summary>
        public Xml AddImage(
            string name,
            string? width = null,
            string? height = null,
            string? imgFloat = null,
            string? align = null)
        {
            var child = CreateElement("img");
            if (width != null)
                child.SetAttribute("width", width);
            if (height != null)
                child.SetAttribute("height", height);
            if (imgFloat != null)
                child.SetAttribute("style", $"float: {imgFloat}");
            if (align != null)
                child.SetAttribute("align", align);
            child.SetAttribute("src", name);
            AppendChild(child);
            return child;
        }

        // ─── Inline content ──────────────────────────────────────────────

        /// <summary>Add text with line breaks as <c>br</c> elements (PyMuPDF <c>Xml.add_text</c>).</summary>
        public Xml AddText(string text)
        {
            var lines = SplitLines(text);
            var prev = SpanBottom() ?? this;
            for (int i = 0; i < lines.Length; i++)
            {
                prev.AppendChild(CreateTextNode(lines[i]));
                if (i < lines.Length - 1)
                    prev.AppendChild(CreateElement("br"));
            }
            return this;
        }

        /// <summary>Append text and <c>br</c> nodes as direct children (PyMuPDF <c>Xml.insert_text</c>).</summary>
        public Xml InsertText(string text)
        {
            var lines = SplitLines(text);
            for (int i = 0; i < lines.Length; i++)
            {
                AppendChild(CreateTextNode(lines[i]));
                if (i < lines.Length - 1)
                    AppendChild(CreateElement("br"));
            }
            return this;
        }

        /// <summary>Add a hyperlink (<c>a</c>) (PyMuPDF <c>Xml.add_link</c>).</summary>
        public Xml AddLink(string href, string? text = null)
        {
            if (text == null)
                text = href;
            var child = CreateElement("a");
            child.SetAttribute("href", href);
            child.AppendChild(CreateTextNode(text));
            var prev = SpanBottom() ?? this;
            prev.AppendChild(child);
            return this;
        }

        /// <summary>Add inline <c>code</c> (PyMuPDF <c>Xml.add_code</c>).</summary>
        public Xml AddCode(string? text = null) => AddInlineTag("code", text);

        /// <summary>Add inline <c>var</c> (PyMuPDF aliases <c>add_var</c> to <c>add_code</c>).</summary>
        public Xml AddVar(string? text = null) => AddCode(text);

        /// <summary>Add inline sample text (PyMuPDF aliases <c>add_samp</c> to <c>add_code</c>).</summary>
        public Xml AddSamp(string? text = null) => AddCode(text);

        /// <summary>Add inline keyboard text (PyMuPDF aliases <c>add_kbd</c> to <c>add_code</c>).</summary>
        public Xml AddKbd(string? text = null) => AddCode(text);

        /// <summary>Add subscript (<c>sub</c>) (PyMuPDF <c>Xml.add_subscript</c>).</summary>
        public Xml AddSubscript(string? text = null) => AddInlineTag("sub", text);

        /// <summary>Add superscript (<c>sup</c>) (PyMuPDF <c>Xml.add_superscript</c>).</summary>
        public Xml AddSuperscript(string? text = null) => AddInlineTag("sup", text);

        // ─── CSS / presentation ──────────────────────────────────────────

        /// <summary>Append a CSS fragment to the <c>style</c> attribute (PyMuPDF <c>Xml.add_style</c>).</summary>
        public Xml AddStyle(string text)
        {
            var style = GetAttributeValue("style");
            if (style != null && style.IndexOf(text, StringComparison.Ordinal) >= 0)
                return this;
            RemoveAttribute("style");
            if (style == null)
                style = text;
            else
                style += ";" + text;
            SetAttribute("style", style);
            return this;
        }

        /// <summary>Append a class name to the <c>class</c> attribute (PyMuPDF <c>Xml.add_class</c>).</summary>
        public Xml AddClass(string text)
        {
            var cls = GetAttributeValue("class");
            if (cls != null && cls.IndexOf(text, StringComparison.Ordinal) >= 0)
                return this;
            RemoveAttribute("class");
            if (cls == null)
                cls = text;
            else
                cls += " " + text;
            SetAttribute("class", cls);
            return this;
        }

        /// <summary>Set text alignment on block nodes (PyMuPDF <c>Xml.set_align</c>).</summary>
        public Xml SetAlign(object align)
        {
            string t;
            if (align is string s)
                t = s;
            else if (align is int n)
            {
                t = n switch
                {
                    Constants.TextAlignLeft => "left",
                    Constants.TextAlignCenter => "center",
                    Constants.TextAlignRight => "right",
                    Constants.TextAlignJustify => "justify",
                    _ => throw new ValueErrorException($"Unrecognised {align}"),
                };
            }
            else
                throw new ValueErrorException($"Unrecognised {align}");
            return AddStyle($"text-align: {t}");
        }

        /// <summary>Set background color on block nodes (PyMuPDF <c>Xml.set_bgcolor</c>).</summary>
        public Xml SetBgColor(int color) => AddStyle($"background-color: {ColorText(color)}");

        /// <summary>Set background color on block nodes.</summary>
        public Xml SetBgColor(float[] color) => AddStyle($"background-color: {ColorText(color)}");

        /// <summary>Set background color on block nodes.</summary>
        public Xml SetBgColor(string color) => AddStyle($"background-color: {ColorText(color)}");

        /// <summary>Apply bold via a nested styled span (PyMuPDF <c>Xml.set_bold</c>).</summary>
        public Xml SetBold(bool val = true) =>
            AppendStyledSpan($"font-weight: {(val ? "bold" : "normal")}");

        /// <summary>Apply bold with a custom <c>font-weight</c> value.</summary>
        public Xml SetBold(string val) => AppendStyledSpan($"font-weight: {val}");

        /// <summary>Apply italic via a nested styled span (PyMuPDF <c>Xml.set_italic</c>).</summary>
        public Xml SetItalic(bool val = true) =>
            AppendStyledSpan($"font-style: {(val ? "italic" : "normal")}");

        /// <summary>Apply italic with a custom <c>font-style</c> value.</summary>
        public Xml SetItalic(string val) => AppendStyledSpan($"font-style: {val}");

        /// <summary>Set text color for following content (PyMuPDF <c>Xml.set_color</c>).</summary>
        public Xml SetColor(object color) => AppendStyledSpan($"color: {ColorText(color)}");

        /// <summary>Set column count (PyMuPDF <c>Xml.set_columns</c>).</summary>
        public Xml SetColumns(int cols) => AppendStyledSpan($"columns: {cols}");

        /// <summary>Set font family (PyMuPDF <c>Xml.set_font</c>).</summary>
        public Xml SetFont(string font) => AppendStyledSpan($"font-family: {font}");

        /// <summary>Set font size in pixels (PyMuPDF <c>Xml.set_fontsize</c>).</summary>
        public Xml SetFontSize(float fontSize) => AppendStyledSpan($"font-size: {fontSize}px");

        /// <summary>Set font size with a CSS length (PyMuPDF <c>Xml.set_fontsize</c>).</summary>
        public Xml SetFontSize(string fontSize) => AppendStyledSpan($"font-size: {fontSize}");

        /// <summary>Set a unique element <c>id</c> (PyMuPDF <c>Xml.set_id</c>).</summary>
        public Xml SetId(string unique)
        {
            if (Root!.Find(null, "id", unique) != null)
                throw new ValueErrorException($"id '{unique}' already exists");
            SetAttribute("id", unique);
            return this;
        }

        /// <summary>Set inter-block leading (PyMuPDF <c>Xml.set_leading</c>).</summary>
        public Xml SetLeading(string leading) => AddStyle($"-mupdf-leading: {leading}");

        /// <summary>Set letter spacing (PyMuPDF <c>Xml.set_letter_spacing</c>).</summary>
        public Xml SetLetterSpacing(string spacing) => AppendStyledSpan($"letter-spacing: {spacing}");

        /// <summary>Set line height on block nodes (PyMuPDF <c>Xml.set_lineheight</c>).</summary>
        public Xml SetLineHeight(string lineHeight) => AddStyle($"line-height: {lineHeight}");

        /// <summary>Set margins via a styled span (PyMuPDF <c>Xml.set_margins</c>).</summary>
        public Xml SetMargins(string val) => AppendStyledSpan($"margins: {val}");

        /// <summary>Set opacity (PyMuPDF <c>Xml.set_opacity</c>).</summary>
        public Xml SetOpacity(string opacity) => AppendStyledSpan($"opacity: {opacity}");

        /// <summary>Force a page break after this node (PyMuPDF <c>Xml.set_pagebreak_after</c>).</summary>
        public Xml SetPageBreakAfter() => AddStyle("page-break-after: always");

        /// <summary>Force a page break before this node (PyMuPDF <c>Xml.set_pagebreak_before</c>).</summary>
        public Xml SetPageBreakBefore() => AddStyle("page-break-before: always");

        /// <summary>Set first-line indent on block nodes (PyMuPDF <c>Xml.set_text_indent</c>).</summary>
        public Xml SetTextIndent(string indent) => AddStyle($"text-indent: {indent}");

        /// <summary>Set text decoration (PyMuPDF <c>Xml.set_underline</c>).</summary>
        public Xml SetUnderline(string val = "underline") =>
            AppendStyledSpan($"text-decoration: {val}");

        /// <summary>Set word spacing (PyMuPDF <c>Xml.set_word_spacing</c>).</summary>
        public Xml SetWordSpacing(string spacing) => AppendStyledSpan($"word-spacing: {spacing}");

        /// <summary>
        /// Apply multiple presentation properties on this node in one call (PyMuPDF <c>Xml.set_properties</c>).
        /// </summary>
        /// <remarks>Unlike individual <c>Set*</c> methods, styles are merged onto this node's <c>style</c> attribute.</remarks>
        public Xml SetProperties(
            object? align = null,
            object? bgcolor = null,
            bool? bold = null,
            object? color = null,
            int? columns = null,
            string? font = null,
            object? fontSize = null,
            string? indent = null,
            bool? italic = null,
            string? leading = null,
            string? letterSpacing = null,
            string? lineHeight = null,
            string? margins = null,
            object? pageBreakAfter = null,
            object? pageBreakBefore = null,
            string? wordSpacing = null,
            string? unqid = null,
            string? cls = null)
        {
            var root = Root!;
            var temp = root.AddDivision();
            if (align != null)
                temp.SetAlign(align);
            if (bgcolor != null)
            {
                if (bgcolor is int bi)
                    temp.SetBgColor(bi);
                else if (bgcolor is string bs)
                    temp.SetBgColor(bs);
                else if (bgcolor is float[] bf)
                    temp.SetBgColor(bf);
                else
                    throw new ValueErrorException($"Unsupported bgcolor type: {bgcolor.GetType().Name}");
            }
            if (bold != null)
                temp.SetBold(bold.Value);
            if (color != null)
                temp.SetColor(color);
            if (columns != null)
                temp.SetColumns(columns.Value);
            if (font != null)
                temp.SetFont(font);
            if (fontSize != null)
            {
                if (fontSize is float f)
                    temp.SetFontSize(f);
                else if (fontSize is int n)
                    temp.SetFontSize(n);
                else
                    temp.SetFontSize(fontSize.ToString()!);
            }
            if (indent != null)
                temp.SetTextIndent(indent);
            if (italic != null)
                temp.SetItalic(italic.Value);
            if (leading != null)
                temp.SetLeading(leading);
            if (letterSpacing != null)
                temp.SetLetterSpacing(letterSpacing);
            if (lineHeight != null)
                temp.SetLineHeight(lineHeight);
            if (margins != null)
                temp.SetMargins(margins);
            if (pageBreakAfter != null)
                temp.SetPageBreakAfter();
            if (pageBreakBefore != null)
                temp.SetPageBreakBefore();
            if (wordSpacing != null)
                temp.SetWordSpacing(wordSpacing);
            if (unqid != null)
                SetId(unqid);
            if (cls != null)
                AddClass(cls);

            var styles = new List<string>();
            var topStyle = temp.GetAttributeValue("style");
            if (topStyle != null)
                styles.Add(topStyle);
            var child = temp.FirstChild;
            while (child != null)
            {
                var st = child.GetAttributeValue("style");
                if (st != null)
                    styles.Add(st);
                child = child.FirstChild;
            }
            SetAttribute("style", string.Join(";", styles));
            temp.Remove();
            return this;
        }

        // ─── Helpers ─────────────────────────────────────────────────────

        private Xml AddInlineTag(string tag, string? text)
        {
            var child = CreateElement(tag);
            if (text != null)
                child.AppendChild(CreateTextNode(text));
            var prev = SpanBottom() ?? this;
            prev.AppendChild(child);
            return this;
        }

        /// <summary>Append a <c>span</c> with inline CSS (PyMuPDF <c>Xml.append_styled_span</c>).</summary>
        public Xml AppendStyledSpan(string style)
        {
            var span = CreateElement("span");
            span.AddStyle(style);
            var prev = SpanBottom() ?? this;
            prev.AppendChild(span);
            return prev;
        }

        /// <summary>Deepest open <c>span</c> for inline styling (PyMuPDF <c>Xml.span_bottom</c>).</summary>
        public Xml? SpanBottom()
        {
            var parent = this;
            var child = LastChild;
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
                if (child.TagName is "a" or "sub" or "sup" or "body" || child.IsText)
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

        private static string EscapeDebugLine(string? text)
        {
            if (string.IsNullOrEmpty(text) || text.IndexOf('\n') < 0)
                return text ?? "";
            var sb = new System.Text.StringBuilder(text.Length + 4);
            foreach (char ch in text)
            {
                if (ch == '\n')
                    sb.Append("\\n");
                else
                    sb.Append(ch);
            }
            return sb.ToString();
        }

        private static string[] SplitLines(string text) =>
            text.Replace("\r\n", "\n")
                .Replace('\r', '\n')
                .Split('\n');

        private static (int r, int g, int b) SRgbToRgb(int srgb)
        {
            srgb &= 0xffffff;
            return ((srgb >> 16) & 0xff, (srgb >> 8) & 0xff, srgb & 0xff);
        }

        private List<(int shift, string line)> GetNodeTree() => ShowNode(this, new List<(int, string)>(), 0);

        private static List<(int shift, string line)> ShowNode(Xml? node, List<(int, string)> items, int shift)
        {
            while (node != null)
            {
                if (node.IsText)
                {
                    items.Add((shift, $"\"{node.Text}\""));
                    node = node.Next;
                    continue;
                }
                items.Add((shift, $"({node.TagName}"));
                var attrs = node.GetAttributes();
                if (attrs != null)
                {
                    foreach (var kv in attrs)
                        items.Add((shift, $"={kv.Key} '{kv.Value}'"));
                }
                var child = node.FirstChild;
                if (child != null)
                    ShowNode(child, items, shift + 1);
                items.Add((shift, $"){node.TagName}"));
                node = node.Next;
            }
            return items;
        }

        // ─── PyMuPDF API names (internal, same assembly) ─────────────────

        internal Xml? bodytag() => BodyTag();
        internal Xml? find(string? tag, string? att, string? match) => Find(tag, att, match);
        internal Xml? find_next(string? tag, string? att, string? match) => FindNext(tag, att, match);
        internal string? tagname => TagName;
        internal string? text => Text;
        internal bool is_text => IsText;
        internal Xml? root => Root;
        internal Xml? previous => Previous;
        internal Xml? next => Next;
        internal Xml? parent => Parent;
        internal Xml? first_child => FirstChild;
        internal Xml? last_child => LastChild;
        internal string? get_attribute_value(string key) => GetAttributeValue(key);
        internal void set_attribute(string key, string? value = null) => SetAttribute(key, value);
        internal void remove_attribute(string key) => RemoveAttribute(key);
        internal Dictionary<string, string>? get_attributes() => GetAttributes();
        internal void append_child(Xml child) => AppendChild(child);
        internal Xml create_element(string tag) => CreateElement(tag);
        internal Xml create_text_node(string text) => CreateTextNode(text);
        internal void insert_before(Xml node) => InsertBefore(node);
        internal void insert_after(Xml node) => InsertAfter(node);
        internal void remove() => Remove();
        internal Xml clone() => Clone();
        internal void debug() => Debug();
        internal Xml add_bullet_list() => AddBulletList();
        internal Xml add_number_list(int start = 1, string? numtype = null) => AddNumberList(start, numtype);
        internal Xml add_list_item() => AddListItem();
        internal Xml add_paragraph() => AddParagraph();
        internal Xml add_division() => AddDivision();
        internal Xml add_description_list() => AddDescriptionList();
        internal Xml add_codeblock() => AddCodeBlock();
        internal Xml add_horizontal_line() => AddHorizontalLine();
        internal Xml add_span() => AddSpan();
        internal Xml add_header(int level = 1) => AddHeader(level);
        internal Xml add_image(string name, string? width = null, string? height = null, string? imgfloat = null, string? align = null) =>
            AddImage(name, width, height, imgfloat, align);
        internal Xml add_text(string text) => AddText(text);
        internal Xml insert_text(string text) => InsertText(text);
        internal Xml add_link(string href, string? text = null) => AddLink(href, text);
        internal Xml add_code(string? text = null) => AddCode(text);
        internal Xml add_var(string? text = null) => AddVar(text);
        internal Xml add_samp(string? text = null) => AddSamp(text);
        internal Xml add_kbd(string? text = null) => AddKbd(text);
        internal Xml add_subscript(string? text = null) => AddSubscript(text);
        internal Xml add_superscript(string? text = null) => AddSuperscript(text);
        internal Xml add_style(string text) => AddStyle(text);
        internal Xml add_class(string text) => AddClass(text);
        internal Xml set_align(object align) => SetAlign(align);
        internal Xml set_bgcolor(int color) => SetBgColor(color);
        internal Xml set_bold(bool val = true) => SetBold(val);
        internal Xml set_italic(bool val = true) => SetItalic(val);
        internal Xml set_color(object color) => SetColor(color);
        internal Xml set_columns(int cols) => SetColumns(cols);
        internal Xml set_font(string font) => SetFont(font);
        internal Xml set_fontsize(float fontsize) => SetFontSize(fontsize);
        internal Xml set_fontsize(string fontsize) => SetFontSize(fontsize);
        internal Xml set_id(string unique) => SetId(unique);
        internal Xml set_leading(string leading) => SetLeading(leading);
        internal Xml set_letter_spacing(string spacing) => SetLetterSpacing(spacing);
        internal Xml set_lineheight(string lineheight) => SetLineHeight(lineheight);
        internal Xml set_margins(string val) => SetMargins(val);
        internal Xml set_opacity(string opacity) => SetOpacity(opacity);
        internal Xml set_pagebreak_after() => SetPageBreakAfter();
        internal Xml set_pagebreak_before() => SetPageBreakBefore();
        internal Xml set_text_indent(string indent) => SetTextIndent(indent);
        internal Xml set_underline(string val = "underline") => SetUnderline(val);
        internal Xml set_word_spacing(string spacing) => SetWordSpacing(spacing);
        internal Xml append_styled_span(string style) => AppendStyledSpan(style);
        internal Xml? span_bottom() => SpanBottom();
        internal static string color_text(object color) => ColorText(color);
    }
}
