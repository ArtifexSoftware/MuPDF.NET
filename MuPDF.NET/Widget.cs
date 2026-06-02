using System;
using System.Collections.Generic;
using System.Linq;

namespace MuPDF.NET
{
    /// <summary>
    /// PDF form field (widget) for interactive forms (PyMuPDF <c>Widget</c>).
    /// </summary>
    /// <remarks>
    /// <para>PDF only. Widgets are a specialized annotation type for user input. Traverse a page with
    /// <see cref="Page.FirstWidget"/> and <see cref="Next"/> (not <see cref="Page.FirstAnnot"/>).</para>
    /// <para>Create with <see cref="Widget()"/>, set properties, then <see cref="Page.AddWidget"/> or change an
    /// existing field and call <see cref="Update"/>. Reload the page after adding fields if you need a fresh view.</para>
    /// <para>Supported types: text, button, checkbox, combobox, listbox, radiobutton (no radio groups), signature (read-only).</para>
    /// </remarks>
    public class Widget : IDisposable
    {
        private mupdf.PdfAnnot _nativeWidget;
        private bool _disposed;
        private bool _insertMode;

        public Page Parent { get; internal set; }
        internal Annot BoundAnnot { get; private set; }
        internal mupdf.PdfAnnot NativeWidget => _nativeWidget;

        // Values used when building a new widget (PyMuPDF Widget() before add_widget).
        internal WidgetType InsertFieldType { get; private set; } = WidgetType.Unknown;
        internal string InsertFieldName { get; private set; }
        internal string InsertFieldValue { get; private set; }
        internal bool? InsertFieldValueBool { get; private set; }
        internal string InsertFieldLabel { get; private set; }
        internal List<string> InsertChoiceValues { get; private set; } // choice fields only (PyMuPDF __init__)
        internal List<object> InsertChoiceValuesMixed { get; private set; }
        internal Rect InsertRect { get; private set; }
        internal string InsertScript { get; private set; }
        internal string InsertScriptStroke { get; private set; }
        internal string InsertScriptFormat { get; private set; }
        internal string InsertScriptChange { get; private set; }
        internal string InsertScriptCalc { get; private set; }
        internal string InsertScriptBlur { get; private set; }
        internal string InsertScriptFocus { get; private set; }
        internal string InsertBorderStyle { get; private set; } = "S";
        internal float InsertBorderWidth { get; private set; }
        internal int? InsertFieldFlags { get; private set; }
        internal List<float> InsertFillColor { get; private set; }
        internal List<float> InsertBorderColor { get; private set; }
        internal List<int> InsertBorderDashes { get; private set; }
        internal string InsertTextDa { get; private set; } = "";
        internal int InsertTextMaxLen { get; private set; }
        internal int InsertFieldDisplay { get; private set; }
        internal string InsertButtonCaption { get; private set; }
        internal List<float> InsertTextColor { get; private set; } = new List<float> { 0, 0, 0 };
        internal string InsertTextFont { get; private set; } = "Helv";
        internal float InsertTextFontsize { get; private set; }
        private int _legacyTextFormat;
        private int _legacyRbParent;

        /// <summary>Creates a widget definition for <see cref="Page.AddWidget"/>.</summary>
        public Widget()
        {
            _insertMode = true;
        }

        /// <summary>Create an empty widget bound to a page (legacy MuPDF.NET API).</summary>
        public Widget(Page page)
        {
            Parent = page;
            _insertMode = true;
        }

        internal Widget(mupdf.PdfAnnot widget, Page page)
        {
            _nativeWidget = widget;
            Parent = page;
            _insertMode = false;
            SyncFromNative();
        }

        internal void BindAnnot(mupdf.PdfAnnot annot, Page page, Annot annotWrapper)
        {
            _nativeWidget = annot;
            Parent = page;
            BoundAnnot = annotWrapper;
            _insertMode = false;
        }

        // ─── PyMuPDF Widget attributes (settable on insert widgets) ─────

        /// <summary>
        /// Field type 1–7 (<see cref="WidgetType"/>). Cannot be changed on an existing widget.
        /// </summary>
        public WidgetFieldType FieldType
        {
            get
            {
                if (_insertMode)
                    return (int)InsertFieldType;
                return (int)mupdf.mupdf.pdf_widget_type(_nativeWidget);
            }
            set
            {
                if (!_insertMode)
                    throw new InvalidOperationException("cannot set field_type on existing widget");
                int fieldType = value;
                if (fieldType < 1 || fieldType > 7)
                    throw new ValueErrorException("bad field type");
                InsertFieldType = (WidgetType)fieldType;
            }
        }

        /// <summary>Human-readable field type derived from <see cref="FieldType"/>.</summary>
        public string FieldTypeString
        {
            get
            {
                return (WidgetType)FieldType switch
                {
                    WidgetType.Button => "Button",
                    WidgetType.Text => "Text",
                    WidgetType.ComboBox => "ComboBox",
                    WidgetType.ListBox => "ListBox",
                    WidgetType.Signature => "Signature",
                    WidgetType.CheckBox => "CheckBox",
                    WidgetType.RadioButton => "RadioButton",
                    _ => "Unknown"
                };
            }
        }

        /// <summary>Mandatory PDF field name (unique within the form).</summary>
        public string FieldName
        {
            get
            {
                if (_insertMode)
                    return InsertFieldName ?? "";
                var name = mupdf.mupdf.pdf_load_field_name(mupdf.mupdf.pdf_annot_obj(_nativeWidget));
                return name ?? "";
            }
            set
            {
                if (_insertMode)
                    InsertFieldName = value;
                else
                    throw new InvalidOperationException("use update_widget to change field_name");
            }
        }

        /// <summary>Alternate field name or tooltip (<c>TU</c>); defaults to <see cref="FieldName"/>.</summary>
        public string FieldLabel
        {
            get
            {
                if (_insertMode)
                    return InsertFieldLabel ?? "";
                return GetInheritableLabel(mupdf.mupdf.pdf_annot_obj(_nativeWidget)) ?? "";
            }
            set
            {
                if (_insertMode)
                    InsertFieldLabel = value;
                else
                    throw new InvalidOperationException("use update_widget to change field_label");
            }
        }

        /// <summary>
        /// Optional attribute set by some PyMuPDF tests as <c>field.value</c>; not read by <see cref="Update"/>.
        /// Use <see cref="FieldValue"/> for the PDF field value.
        /// </summary>
        public string Value { get; set; }

        /// <summary>Current field value; for buttons use <see cref="OnState"/> or <c>true</c>/<c>false</c> via <see cref="SetFieldValue"/>.</summary>
        public string FieldValue
        {
            get
            {
                if (_insertMode)
                    return InsertFieldValue ?? "";
                return mupdf.mupdf.pdf_field_value(mupdf.mupdf.pdf_annot_obj(_nativeWidget)) ?? "";
            }
            set => SetFieldValue(value);
        }

        /// <summary>Set field value (PyMuPDF accepts bool for button fields).</summary>
        public void SetFieldValue(object value)
        {
            InsertFieldValueBool = null;
            if (value is bool b)
            {
                InsertFieldValueBool = b;
                InsertFieldValue = b ? (OnState() ?? "Yes") : "Off";
            }
            else
                InsertFieldValue = value?.ToString() ?? "";
        }

        /// <summary>Valid choices for list and combo boxes (at least two entries when creating).</summary>
        public List<string> ChoiceValues
        {
            get
            {
                if (_insertMode)
                    return InsertChoiceValues ?? new List<string>();
                var result = new List<string>();
                var obj = mupdf.mupdf.pdf_annot_obj(_nativeWidget);
                var opt = mupdf.mupdf.pdf_dict_get(obj, mupdf.mupdf.pdf_new_name("Opt"));
                if (opt.m_internal == null) return result;
                int n = mupdf.mupdf.pdf_array_len(opt);
                for (int i = 0; i < n; i++)
                {
                    var item = mupdf.mupdf.pdf_array_get(opt, i);
                    if (mupdf.mupdf.pdf_is_array(item) != 0)
                        result.Add(mupdf.mupdf.pdf_to_text_string(mupdf.mupdf.pdf_array_get(item, 1)));
                    else
                        result.Add(mupdf.mupdf.pdf_to_text_string(item));
                }
                return result;
            }
            set
            {
                if (_insertMode)
                {
                    InsertChoiceValuesMixed = null;
                    InsertChoiceValues = value;
                }
                else
                    SetChoiceValues(value);
            }
        }

        /// <summary>
        /// Set choice options (PyMuPDF <c>choice_values</c> with strings or 2-item lists/tuples).
        /// </summary>
        public void SetChoiceValues(IEnumerable<object> values)
        {
            if (values == null)
            {
                InsertChoiceValuesMixed = null;
                InsertChoiceValues = null;
                return;
            }
            InsertChoiceValuesMixed = values.ToList();
            InsertChoiceValues = null;
            if (!_insertMode && _nativeWidget?.m_internal != null)
                Helpers.JmSetChoiceOptions(_nativeWidget, InsertChoiceValuesMixed);
        }

        /// <summary>Field rectangle on the page.</summary>
        public Rect Rect
        {
            get
            {
                if (_insertMode)
                    return InsertRect;
                var r = mupdf.mupdf.pdf_bound_annot(_nativeWidget);
                return new Rect(r.x0, r.y0, r.x1, r.y1);
            }
            set
            {
                if (_insertMode)
                    InsertRect = value;
                else
                    SetRect(value);
            }
        }

        /// <summary>JavaScript on change (PyMuPDF <c>script_change</c>, /AA/V).</summary>
        public string ScriptChange
        {
            get => _insertMode ? InsertScriptChange : GetScript("V");
            set
            {
                if (!_insertMode)
                    throw new InvalidOperationException("use update_widget to change script_change");
                InsertScriptChange = value;
            }
        }

        /// <summary>Field flags (PyMuPDF <c>field_flags</c>).</summary>
        public int FieldFlags
        {
            get
            {
                if (_insertMode)
                    return InsertFieldFlags ?? 0;
                return mupdf.mupdf.pdf_field_flags(mupdf.mupdf.pdf_annot_obj(_nativeWidget));
            }
            set => InsertFieldFlags = value;
        }

        /// <summary>Legacy field display flags.</summary>
        public int FieldDisplay
        {
            get => InsertFieldDisplay;
            set => InsertFieldDisplay = value;
        }

        /// <summary>Border color (up to four components, 0–1); null disables border drawing.</summary>
        public IList<float> BorderColor
        {
            get => InsertBorderColor;
            set => InsertBorderColor = ToFloatList(value);
        }

        /// <summary>Background fill color (up to four components).</summary>
        public IList<float> FillColor
        {
            get => InsertFillColor;
            set => InsertFillColor = ToFloatList(value);
        }

        /// <summary>Dash pattern for dashed borders (<see cref="BorderStyle"/> <c>D</c> with <see cref="BorderColor"/> set).</summary>
        public IList<int> BorderDashes
        {
            get => InsertBorderDashes;
            set => InsertBorderDashes = ToIntList(value);
        }

        /// <summary>JavaScript for button actions (<c>/A</c>); only supported on button-type fields.</summary>
        public string Script
        {
            get => _insertMode ? InsertScript : GetTopLevelScript();
            set
            {
                if (_insertMode)
                    InsertScript = value;
                else
                    throw new InvalidOperationException("use Update() to change script");
            }
        }

        /// <summary>Text max length (PyMuPDF <c>text_maxlen</c>).</summary>
        public int TextMaxlen
        {
            get => MaxLen;
            set => MaxLen = value;
        }

        /// <summary>
        /// Border line style (first character only): <c>S</c> solid, <c>D</c> dashed, etc. (see <see cref="Annot"/> border styles).
        /// </summary>
        public string BorderStyle
        {
            get => InsertBorderStyle ?? "S";
            set => InsertBorderStyle = value;
        }

        /// <summary>Border line width in points (default 1).</summary>
        public float BorderWidth
        {
            get => InsertBorderWidth > 0 ? InsertBorderWidth : 1f;
            set => InsertBorderWidth = value;
        }

        /// <summary>Caption text for push-button fields.</summary>
        public string ButtonCaption
        {
            get => InsertButtonCaption;
            set => InsertButtonCaption = value;
        }

        /// <summary>Text color for default appearance (PyMuPDF <c>text_color</c>).</summary>
        public IList<float> TextColor
        {
            get => InsertTextColor;
            set => InsertTextColor = ToFloatList(value) ?? new List<float> { 0, 0, 0 };
        }

        /// <summary>Text font name (PyMuPDF <c>text_font</c>).</summary>
        public string TextFont
        {
            get => InsertTextFont;
            set => InsertTextFont = NormalizeTextFont(value);
        }

        /// <summary>Text font size (PyMuPDF <c>text_fontsize</c>).</summary>
        public float TextFontsize
        {
            get => InsertTextFontsize;
            set => InsertTextFontsize = value;
        }

        // Legacy API alias uses capital "S".
        public float TextFontSize
        {
            get => TextFontsize;
            set => TextFontsize = value;
        }

        /// <summary>Field default value.</summary>
        public string FieldDefault
        {
            get
            {
                if (_insertMode)
                    return "";
                var dv = mupdf.mupdf.pdf_dict_get_text_string(mupdf.mupdf.pdf_annot_obj(_nativeWidget),
                    mupdf.mupdf.pdf_new_name("DV"));
                return dv ?? "";
            }
        }

        /// <summary>PDF object xref of this widget.</summary>
        public int Xref =>
            _insertMode ? 0 : mupdf.mupdf.pdf_to_num(mupdf.mupdf.pdf_annot_obj(_nativeWidget));

        /// <summary>Check if field is read only.</summary>
        public bool IsReadOnly => (FieldFlags & 1) != 0;

        /// <summary>Check if field is required.</summary>
        public bool IsRequired => (FieldFlags & 2) != 0;

        /// <summary>Maximum text length.</summary>
        public int MaxLen
        {
            get
            {
                if (_insertMode)
                    return InsertTextMaxLen;
                return mupdf.mupdf.pdf_dict_get_int(mupdf.mupdf.pdf_annot_obj(_nativeWidget),
                    mupdf.mupdf.pdf_new_name("MaxLen"));
            }
            set
            {
                if (_insertMode)
                    InsertTextMaxLen = value;
            }
        }

        public int TextMaxLen
        {
            get => MaxLen;
            set => MaxLen = value;
        }

        /// <summary>Legacy text-format flag (MuPDF.NET compatibility).</summary>
        public int TextFormat
        {
            get => _legacyTextFormat;
            set => _legacyTextFormat = value;
        }

        public string TextDa
        {
            get => InsertTextDa;
            set => InsertTextDa = value ?? string.Empty;
        }

        public int RbParent
        {
            get => _legacyRbParent;
            set => _legacyRbParent = value;
        }

        /// <summary>Check if text field is multi-line.</summary>
        public bool IsMultiline => (FieldFlags & (1 << 12)) != 0;

        /// <summary>Check if text field is a comb field.</summary>
        public bool IsComb => (FieldFlags & (1 << 24)) != 0;

        /// <summary>True when a signature field has a value; false for other field types.</summary>
        public bool IsSigned
        {
            get
            {
                if (_insertMode || FieldType != (int)WidgetType.Signature)
                    return false;
                var obj = mupdf.mupdf.pdf_annot_obj(_nativeWidget);
                var v = mupdf.mupdf.pdf_dict_get(obj, mupdf.mupdf.pdf_new_name("V"));
                return v.m_internal != null && mupdf.mupdf.pdf_is_null(v) == 0;
            }
        }

        /// <summary>Next widget on the same page, or null.</summary>
        public Widget Next
        {
            get
            {
                if (_insertMode)
                    return null;
                var next = mupdf.mupdf.pdf_next_widget(_nativeWidget);
                return next.m_internal != null ? new Widget(next, Parent) : null;
            }
        }

        /// <summary>JavaScript calculation script (PyMuPDF <c>script_calc</c>, /AA/C).</summary>
        public string ScriptCalc
        {
            get => _insertMode ? InsertScriptCalc : GetScript("C");
            set
            {
                if (_insertMode)
                    InsertScriptCalc = value;
                else
                    throw new InvalidOperationException("use Update() to change script_calc");
            }
        }

        /// <summary>JavaScript format script (PyMuPDF <c>script_format</c>, /AA/F).</summary>
        public string ScriptFormat
        {
            get => _insertMode ? InsertScriptFormat : GetScript("F");
            set
            {
                if (_insertMode)
                    InsertScriptFormat = value;
                else
                    throw new InvalidOperationException("use Update() to change script_format");
            }
        }

        /// <summary>Keystroke validation script (<c>/AA/K</c>).</summary>
        public string ScriptKeystroke => _insertMode ? InsertScriptStroke : GetScript("K");

        /// <summary>Legacy name for <see cref="ScriptKeystroke"/>.</summary>
        public string ScriptStroke
        {
            get => ScriptKeystroke;
            set
            {
                if (_insertMode)
                    InsertScriptStroke = value;
                else
                    throw new InvalidOperationException("use Update() to change script_stroke");
            }
        }

        /// <summary>Alias for <see cref="ScriptChange"/> (PyMuPDF <c>script_validation</c>).</summary>
        public string ScriptValidation => ScriptChange;

        /// <summary>JavaScript blur script (PyMuPDF <c>script_blur</c>, /AA/Bl).</summary>
        public string ScriptBlur
        {
            get => _insertMode ? InsertScriptBlur : GetScript("Bl");
            set
            {
                if (_insertMode)
                    InsertScriptBlur = value;
                else
                    throw new InvalidOperationException("use Update() to change script_blur");
            }
        }

        /// <summary>JavaScript focus script (PyMuPDF <c>script_focus</c>, /AA/Fo).</summary>
        public string ScriptFocus
        {
            get => _insertMode ? InsertScriptFocus : GetScript("Fo");
            set
            {
                if (_insertMode)
                    InsertScriptFocus = value;
                else
                    throw new InvalidOperationException("use Update() to change script_focus");
            }
        }

        // ─── Methods ────────────────────────────────────────────────────

        /// <summary>
        /// Persists property changes to the PDF (required after edits). Optionally syncs <see cref="FieldFlags"/> to the field group.
        /// </summary>
        /// <param name="syncFlags">Propagate flags to parent and sibling widgets in a field group.</param>
        public void Update(bool syncFlags = false)
        {
            if (_nativeWidget?.m_internal == null)
                throw new InvalidOperationException("Annot is not bound to a page");
            if (InsertFieldType == WidgetType.Unknown)
                InsertFieldType = (WidgetType)FieldType;
            if (string.IsNullOrEmpty(InsertFieldName))
                InsertFieldName = FieldName;
            if (InsertRect.IsEmpty || InsertRect.IsInfinite)
            {
                var r = mupdf.mupdf.pdf_bound_annot(_nativeWidget);
                InsertRect = new Rect(r.x0, r.y0, r.x1, r.y1);
            }
            if (InsertFieldType == WidgetType.RadioButton
                && InsertFieldValueBool != false
                && InsertFieldValue != "Off")
                TurnOffSiblingRadioButtons();
            Validate();
            AdjustFont();
            BuildTextDa();
            if (!string.IsNullOrEmpty(InsertScriptCalc))
                Helpers.EnsureWidgetCalc(_nativeWidget);
            Helpers.JmSetWidgetProperties(_nativeWidget, this);
            InsertTextDa = "";
            InsertFieldValueBool = null;
            if (syncFlags)
                SyncFlags();
        }

        /// <summary>Port of PyMuPDF <c>Widget._validate</c>.</summary>
        internal void Validate()
        {
            if (InsertRect.IsEmpty || InsertRect.IsInfinite)
                throw new ValueErrorException("bad rect");
            if (InsertFieldType == WidgetType.Unknown || (int)InsertFieldType < 1 || (int)InsertFieldType > 7)
                throw new ValueErrorException("bad field type");
            if (string.IsNullOrEmpty(InsertFieldName))
                throw new ValueErrorException("field name missing");
            if (InsertFieldLabel == "Unnamed")
                InsertFieldLabel = null;
            if (InsertTextColor == null || InsertTextColor.Count == 0)
                InsertTextColor = new List<float> { 0, 0, 0 };
            if (string.IsNullOrEmpty(InsertBorderStyle))
                InsertBorderStyle = "S";
            else
                InsertBorderStyle = InsertBorderStyle.ToUpperInvariant().Substring(0, 1);

            bool btnType = InsertFieldType == WidgetType.Button
                || InsertFieldType == WidgetType.CheckBox
                || InsertFieldType == WidgetType.RadioButton;
            if (btnType || string.IsNullOrEmpty(InsertScriptCalc)) InsertScriptCalc = null;
            if (btnType || string.IsNullOrEmpty(InsertScriptChange)) InsertScriptChange = null;
            if (btnType || string.IsNullOrEmpty(InsertScriptFormat)) InsertScriptFormat = null;
            if (btnType || string.IsNullOrEmpty(InsertScriptStroke)) InsertScriptStroke = null;
            if (btnType || string.IsNullOrEmpty(InsertScriptBlur)) InsertScriptBlur = null;
            if (btnType || string.IsNullOrEmpty(InsertScriptFocus)) InsertScriptFocus = null;
            // PyMuPDF _validate: /A script is allowed on buttons; only AA/* scripts are cleared.
            if (string.IsNullOrEmpty(InsertScript)) InsertScript = null;
        }

        public void AdjustFont()
        {
            InsertTextFont = NormalizeTextFont(InsertTextFont);
        }

        private static string NormalizeTextFont(string font)
        {
            if (string.IsNullOrEmpty(font))
                return "Helv";
            foreach (var f in new[] { "Cour", "TiRo", "Helv", "ZaDb" })
            {
                if (string.Equals(font, f, StringComparison.OrdinalIgnoreCase))
                    return f;
            }
            return "Helv";
        }

        private void BuildTextDa()
        {
            var tc = InsertTextColor;
            string fmt;
            if (tc.Count == 3)
                fmt = "{0:g} {1:g} {2:g} rg /{3} {4:g} Tf";
            else if (tc.Count == 1)
                fmt = "{0:g} g /{1} {2:g} Tf";
            else if (tc.Count == 4)
                fmt = "{0:g} {1:g} {2:g} {3:g} k /{4} {5:g} Tf";
            else
            {
                InsertTextDa = "";
                return;
            }
            if (tc.Count == 3)
                InsertTextDa = string.Format(System.Globalization.CultureInfo.InvariantCulture,
                    fmt, tc[0], tc[1], tc[2], InsertTextFont, InsertTextFontsize);
            else if (tc.Count == 1)
                InsertTextDa = string.Format(System.Globalization.CultureInfo.InvariantCulture,
                    fmt, tc[0], InsertTextFont, InsertTextFontsize);
            else
                InsertTextDa = string.Format(System.Globalization.CultureInfo.InvariantCulture,
                    fmt, tc[0], tc[1], tc[2], tc[3], InsertTextFont, InsertTextFontsize);
        }

        /// <summary>PyMuPDF <c>Widget._sync_flags</c> — propagate field flags to parent and kids.</summary>
        public bool SyncFlags()
        {
            if (Xref == 0)
                return false;
            var doc = Parent?.Parent;
            if (doc == null)
                return false;
            var pdf = doc.NativePdfDocument;
            var pdfWidget = mupdf.mupdf.pdf_load_object(pdf, Xref);
            var parentObj = mupdf.mupdf.pdf_dict_get(pdfWidget, mupdf.mupdf.pdf_new_name("Parent"));
            if (mupdf.mupdf.pdf_is_dict(parentObj) == 0)
                return false;

            int flags = FieldFlags;
            mupdf.mupdf.pdf_dict_put_int(parentObj, mupdf.mupdf.pdf_new_name("Ff"), flags);

            var kids = mupdf.mupdf.pdf_dict_get(parentObj, mupdf.mupdf.pdf_new_name("Kids"));
            if (mupdf.mupdf.pdf_is_array(kids) == 0)
            {
                Helpers.message("warning: malformed PDF, Parent has no Kids array");
                return false;
            }
            int n = mupdf.mupdf.pdf_array_len(kids);
            for (int i = 0; i < n; i++)
            {
                var kid = mupdf.mupdf.pdf_array_get(kids, i);
                if (mupdf.mupdf.pdf_is_dict(kid) == 0)
                    continue;
                int kidXref = mupdf.mupdf.pdf_to_num(kid);
                if (kidXref == Xref)
                    continue;
                var subtype = mupdf.mupdf.pdf_dict_get(kid, mupdf.mupdf.pdf_new_name("Subtype"));
                if (mupdf.mupdf.pdf_to_name(subtype) != "Widget")
                    continue;
                mupdf.mupdf.pdf_dict_put_int(kid, mupdf.mupdf.pdf_new_name("Ff"), flags);
            }
            return true;
        }

        /// <summary>Set widget rectangle on an existing field (PyMuPDF uses <c>rect</c> on insert widgets).</summary>
        public void SetRect(Rect rect)
        {
            mupdf.mupdf.pdf_set_annot_rect(_nativeWidget, rect.ToFzRect());
            mupdf.mupdf.pdf_update_annot(_nativeWidget);
        }

        public void SetChoiceValues(List<string> values)
        {
            if (values == null || values.Count == 0)
            {
                InsertChoiceValues = null;
                InsertChoiceValuesMixed = null;
                return;
            }
            InsertChoiceValuesMixed = null;
            InsertChoiceValues = values;
            if (!_insertMode && _nativeWidget?.m_internal != null)
                Helpers.JmSetChoiceOptions(_nativeWidget, values);
        }

        /// <summary>Refresh the widget appearance stream (MuPDF <c>pdf_update_annot</c>).</summary>
        public void UpdateAppearance() => mupdf.mupdf.pdf_update_annot(_nativeWidget);

        /// <summary>
        /// Appearance state names for checkbox and radio button widgets (PyMuPDF <c>button_states</c>).
        /// </summary>
        /// <returns>
        /// Dictionary with keys <c>normal</c> and <c>down</c>, each a list of state names from <c>AP/N</c> and <c>AP/D</c>,
        /// or null for non-button field types.
        /// </returns>
        public Dictionary<string, List<string>> ButtonStates()
        {
            if (FieldType != (int)WidgetType.CheckBox && FieldType != (int)WidgetType.RadioButton)
                return null;
            if (_insertMode || Xref == 0)
                return null;

            var doc = Parent?.Parent;
            if (doc == null)
                return null;

            var states = new Dictionary<string, List<string>>
            {
                ["normal"] = null,
                ["down"] = null,
            };

            states["normal"] = ReadAppearanceStates(doc, Xref, "AP/N");
            states["down"] = ReadAppearanceStates(doc, Xref, "AP/D");
            return states;
        }

        /// <summary>
        /// The “on” value for checkboxes and radio buttons (PyMuPDF <c>on_state</c>).
        /// </summary>
        /// <returns>The non-<c>Off</c> appearance state name from <see cref="ButtonStates"/>, or <c>Yes</c> if none is found.</returns>
        public string OnState()
        {
            if (FieldType != (int)WidgetType.CheckBox && FieldType != (int)WidgetType.RadioButton)
                return null;

            if (!_insertMode && _nativeWidget?.m_internal != null)
            {
                var onstate = mupdf.mupdf.pdf_button_field_on_state(mupdf.mupdf.pdf_annot_obj(_nativeWidget));
                if (onstate.m_internal != null)
                {
                    string name = mupdf.mupdf.pdf_to_name(onstate);
                    if (!string.IsNullOrEmpty(name) && name != "Off")
                        return name;
                }
            }

            var bstate = ButtonStates();
            if (bstate != null)
            {
                foreach (var list in bstate.Values)
                {
                    if (list == null) continue;
                    foreach (string v in list)
                    {
                        if (v != "Off")
                            return v;
                    }
                }
            }

            Helpers.message("warning: radio button has no 'On' value.");
            return "Yes";
        }

        private static List<string> ReadAppearanceStates(Document doc, int xref, string key)
        {
            var (kind, raw) = doc.XrefGetKey(xref, key);
            if (kind == "null")
                return null;
            if (kind == "dict")
                return ParseAppearanceStateNames(raw);
            if (kind == "xref" && int.TryParse(raw.Split(' ')[0], out int subXref))
                return ParseAppearanceStateNames(doc.XrefObject(subXref));
            return null;
        }

        private static List<string> ParseAppearanceStateNames(string pdfObject)
        {
            if (string.IsNullOrEmpty(pdfObject))
                return null;
            string body = pdfObject.Trim();
            if (body.StartsWith("<<", StringComparison.Ordinal))
                body = body.Substring(2);
            if (body.EndsWith(">>", StringComparison.Ordinal))
                body = body.Substring(0, body.Length - 2);
            var names = new List<string>();
            foreach (string part in body.Split('/'))
            {
                if (string.IsNullOrWhiteSpace(part))
                    continue;
                string name = part.Split((char[])null, StringSplitOptions.RemoveEmptyEntries)[0];
                if (!string.IsNullOrEmpty(name))
                    names.Add(name);
            }
            return names.Count > 0 ? names : null;
        }

        /// <summary>
        /// Resets <see cref="FieldValue"/> to the default (<c>DV</c>) or clears it; call <see cref="Update"/> afterward if needed.
        /// </summary>
        public void Reset()
        {
            // TOOLS._reset_widget(self._annot)
            var obj = mupdf.mupdf.pdf_annot_obj(_nativeWidget);
            var dv = mupdf.mupdf.pdf_dict_get(obj, mupdf.mupdf.pdf_new_name("DV"));
            if (dv.m_internal != null)
                mupdf.mupdf.pdf_dict_put(obj, mupdf.mupdf.pdf_new_name("V"), dv);
            else
                mupdf.mupdf.pdf_dict_del(obj, mupdf.mupdf.pdf_new_name("V"));
            mupdf.mupdf.pdf_update_annot(_nativeWidget);
        }

        /// <summary>Render widget appearance to a pixmap (PyMuPDF annot pixmap pattern).</summary>
        public Pixmap GetPixmap(Matrix matrix = null, Colorspace cs = null, bool alpha = false)
        {
            var ctm = (matrix ?? Matrix.Identity).ToFzMatrix();
            var colorspace = (cs ?? Colorspace.Rgb).ToFzColorspace();
            var pix = mupdf.mupdf.pdf_new_pixmap_from_annot(_nativeWidget, ctm, colorspace, new mupdf.FzSeparations(), alpha ? 1 : 0);
            return new Pixmap(pix);
        }

        private string GetTopLevelScript()
        {
            var obj = mupdf.mupdf.pdf_annot_obj(_nativeWidget);
            var action = mupdf.mupdf.pdf_dict_get(obj, mupdf.mupdf.pdf_new_name("A"));
            return Helpers.JmGetScript(action);
        }

        private string GetScript(string trigger)
        {
            var obj = mupdf.mupdf.pdf_annot_obj(_nativeWidget);
            var aa = mupdf.mupdf.pdf_dict_get(obj, mupdf.mupdf.pdf_new_name("AA"));
            if (aa.m_internal == null) return null;
            var action = mupdf.mupdf.pdf_dict_gets(aa, trigger);
            return Helpers.JmGetScript(action);
        }

        /// <summary>Load widget attributes from the PDF (PyMuPDF <c>JM_make_widget</c>).</summary>
        internal void SyncFromNative()
        {
            if (_nativeWidget?.m_internal == null)
                return;
            var annotObj = mupdf.mupdf.pdf_annot_obj(_nativeWidget);
            InsertFieldType = (WidgetType)FieldType;
            var r = mupdf.mupdf.pdf_bound_annot(_nativeWidget);
            InsertRect = new Rect(r.x0, r.y0, r.x1, r.y1);
            InsertFieldName = FieldName;
            InsertFieldLabel = FieldLabel;
            InsertFieldValue = FieldValue;
            InsertFieldFlags = FieldFlags;
            InsertBorderStyle = mupdf.mupdf.pdf_field_border_style(annotObj) ?? "S";
            InsertBorderWidth = mupdf.mupdf.pdf_to_real(
                Helpers.PdfDictGetl(annotObj, mupdf.mupdf.pdf_new_name("BS"), mupdf.mupdf.pdf_new_name("W")));
            if (InsertBorderWidth == 0)
                InsertBorderWidth = 1;

            var dashObj = Helpers.PdfDictGetl(annotObj, mupdf.mupdf.pdf_new_name("BS"), mupdf.mupdf.pdf_new_name("D"));
            if (dashObj.m_internal != null && mupdf.mupdf.pdf_is_array(dashObj) != 0)
            {
                int n = mupdf.mupdf.pdf_array_len(dashObj);
                InsertBorderDashes = new List<int>(n);
                for (int i = 0; i < n; i++)
                    InsertBorderDashes.Add(mupdf.mupdf.pdf_to_int(mupdf.mupdf.pdf_array_get(dashObj, i)));
            }

            InsertFillColor = ReadColorArray(Helpers.PdfDictGetl(annotObj, mupdf.mupdf.pdf_new_name("MK"), mupdf.mupdf.pdf_new_name("BG")));
            InsertBorderColor = ReadColorArray(Helpers.PdfDictGetl(annotObj, mupdf.mupdf.pdf_new_name("MK"), mupdf.mupdf.pdf_new_name("BC")));
            InsertChoiceValues = new List<string>(ChoiceValues);
            InsertTextMaxLen = MaxLen;
            InsertScript = GetTopLevelScript();
            InsertScriptStroke = GetScript("K");
            InsertScriptFormat = GetScript("F");
            InsertScriptChange = GetScript("V");
            InsertScriptCalc = GetScript("C");
            InsertScriptBlur = GetScript("Bl");
            InsertScriptFocus = GetScript("Fo");

            var da = mupdf.mupdf.pdf_to_text_string(
                mupdf.mupdf.pdf_dict_get_inheritable(annotObj, mupdf.mupdf.pdf_new_name("DA"))) ?? "";
            InsertTextDa = da;
            ParseDa(da);
        }

        private static List<float> ToFloatList(IList<float> value)
        {
            if (value == null)
                return null;
            return value as List<float> ?? new List<float>(value);
        }

        private static List<int> ToIntList(IList<int> value)
        {
            if (value == null)
                return null;
            return value as List<int> ?? new List<int>(value);
        }

        private static List<float> ReadColorArray(mupdf.PdfObj obj)
        {
            if (obj.m_internal == null || mupdf.mupdf.pdf_is_array(obj) == 0)
                return null;
            int n = mupdf.mupdf.pdf_array_len(obj);
            var col = new List<float>(n);
            for (int i = 0; i < n; i++)
                col.Add((float)mupdf.mupdf.pdf_to_real(mupdf.mupdf.pdf_array_get(obj, i)));
            return col;
        }

        private static string GetInheritableLabel(mupdf.PdfObj node)
        {
            var tu = mupdf.mupdf.pdf_new_name("TU");
            var parent = mupdf.mupdf.pdf_new_name("Parent");
            var slow = node;
            int halfbeat = 11;
            while (node.m_internal != null)
            {
                var val = mupdf.mupdf.pdf_dict_get(node, tu);
                if (val.m_internal != null)
                {
                    var label = mupdf.mupdf.pdf_to_text_string(val);
                    if (!string.IsNullOrEmpty(label))
                        return label;
                }
                node = mupdf.mupdf.pdf_dict_get(node, parent);
                if (node.m_internal == slow.m_internal)
                    break;
                halfbeat--;
                if (halfbeat == 0)
                {
                    slow = mupdf.mupdf.pdf_dict_get(slow, parent);
                    halfbeat = 2;
                }
            }
            return null;
        }

        /// <summary>
        /// PyMuPDF <c>Widget._checker</c>: if setting a radio button to ON, set Off on siblings (MuPDF does not do this).
        /// </summary>
        private void TurnOffSiblingRadioButtons()
        {
            if (Parent?.Parent == null)
                return;
            var doc = Parent.Parent;
            var annotObj = mupdf.mupdf.pdf_annot_obj(_nativeWidget);
            var (_, kidsValue) = doc.XrefGetKey(Xref, "Parent/Kids");
            if (kidsValue == null || !kidsValue.StartsWith("["))
                return;
            foreach (var part in kidsValue.Trim('[', ']').Replace("0 R", "")
                .Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries))
            {
                if (!int.TryParse(part, out int xref) || xref == Xref)
                    continue;
                doc.XrefSetKey(xref, "AS", "/Off");
            }
        }

        /// <summary>Legacy checker alias.</summary>
        public void Checker() => TurnOffSiblingRadioButtons();

        /// <summary>
        /// Extract font, size and color from /DA (PyMuPDF <c>Widget._parse_da</c>, pdf_parse_default_appearance).
        /// </summary>
        private void ParseDa(string da)
        {
            if (string.IsNullOrEmpty(da))
                return;
            string font = "Helv";
            float fsize = 0;
            var col = new List<float> { 0, 0, 0 };
            var dat = da.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < dat.Length; i++)
            {
                if (dat[i] == "Tf" && i >= 2)
                {
                    font = dat[i - 2].TrimStart('/');
                    if (float.TryParse(dat[i - 1], System.Globalization.NumberStyles.Float,
                            System.Globalization.CultureInfo.InvariantCulture, out var fs))
                        fsize = fs;
                }
                else if (dat[i] == "g" && i >= 1
                    && float.TryParse(dat[i - 1], System.Globalization.NumberStyles.Float,
                        System.Globalization.CultureInfo.InvariantCulture, out var gray))
                    col = new List<float> { gray };
                else if (dat[i] == "rg" && i >= 3)
                {
                    if (float.TryParse(dat[i - 3], System.Globalization.NumberStyles.Float,
                            System.Globalization.CultureInfo.InvariantCulture, out var r)
                        && float.TryParse(dat[i - 2], System.Globalization.NumberStyles.Float,
                            System.Globalization.CultureInfo.InvariantCulture, out var g)
                        && float.TryParse(dat[i - 1], System.Globalization.NumberStyles.Float,
                            System.Globalization.CultureInfo.InvariantCulture, out var b))
                        col = new List<float> { r, g, b };
                }
            }
            InsertTextFont = font;
            InsertTextFontsize = fsize;
            InsertTextColor = col;
        }

        /// <summary>Legacy no-argument DA parser.</summary>
        public void ParseDa() => ParseDa(InsertTextDa);

        /// <summary>Releases managed wrapper state (native object owned by the page).</summary>
        public void Dispose()
        {
            if (!_disposed) { _disposed = true; }
            GC.SuppressFinalize(this);
        }

        ~Widget() => Dispose();

        public override string ToString() => $"Widget('{FieldTypeString}', '{FieldName}')";

        /// <summary>Legacy raw widget annotation handle.</summary>
        public mupdf.PdfAnnot _annot
        {
            get => _nativeWidget;
            set => _nativeWidget = value;
        }

        /// <summary>Legacy ownership compatibility flag.</summary>
        public bool ThisOwn { get; set; } = true;

        // ─── PyMuPDF API names (internal, same assembly) ─────────────────

        internal void update(bool sync_flags = false) => Update(sync_flags);
        internal void reset() => Reset();
        internal Dictionary<string, List<string>> button_states() => ButtonStates();
        internal string on_state() => OnState();
        internal string field_name { get => FieldName; set => FieldName = value; }
        internal int field_flags { get => FieldFlags; set => FieldFlags = value; }
        internal int xref => Xref;
        internal void set_value(string value) => SetFieldValue(value);
    }
}
