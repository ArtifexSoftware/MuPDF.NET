using System;
using System.Collections.Generic;

namespace MuPDF.NET
{
    /// <summary>
    /// Class describing a PDF form field ('widget').
    /// </summary>
    public class Widget : IDisposable
    {
        private mupdf.PdfAnnot _nativeWidget;
        private bool _disposed;
        internal Page Parent { get; }

        internal Widget(mupdf.PdfAnnot widget, Page page)
        {
            _nativeWidget = widget;
            Parent = page;
        }

        // ─── Properties ─────────────────────────────────────────────────

        /// <summary>
        /// Field type as enum.
        /// </summary>
        public WidgetType FieldType
        {
            get
            {
                var t = mupdf.mupdf.pdf_widget_type(_nativeWidget);
                return (WidgetType)(int)t;
            }
        }

        /// <summary>
        /// Field type as string.
        /// </summary>
        public string FieldTypeString
        {
            get
            {
                return FieldType switch
                {
                    WidgetType.Button => "Button",
                    WidgetType.Text => "Text",
                    WidgetType.ComboBox => "Choice",
                    WidgetType.ListBox => "Choice",
                    WidgetType.Signature => "Signature",
                    _ => "Unknown"
                };
            }
        }

        /// <summary>
        /// Field name.
        /// </summary>
        public string FieldName
        {
            get
            {
                var name = mupdf.mupdf.pdf_load_field_name(mupdf.mupdf.pdf_annot_obj(_nativeWidget));
                return name ?? "";
            }
        }

        /// <summary>
        /// Field label (tooltip).
        /// </summary>
        public string FieldLabel
        {
            get
            {
                var label = mupdf.mupdf.pdf_dict_get_text_string(mupdf.mupdf.pdf_annot_obj(_nativeWidget),
                    mupdf.mupdf.pdf_new_name("TU"));
                return label ?? "";
            }
        }

        /// <summary>
        /// Field value.
        /// </summary>
        public string FieldValue
        {
            get => mupdf.mupdf.pdf_field_value(mupdf.mupdf.pdf_annot_obj(_nativeWidget)) ?? "";
            set
            {
                mupdf.mupdf.pdf_set_field_value(Parent.Parent.NativePdfDocument,
                    mupdf.mupdf.pdf_annot_obj(_nativeWidget), value, 0);
                mupdf.mupdf.pdf_update_annot(_nativeWidget);
            }
        }

        /// <summary>
        /// Field default value.
        /// </summary>
        public string FieldDefault
        {
            get
            {
                var dv = mupdf.mupdf.pdf_dict_get_text_string(mupdf.mupdf.pdf_annot_obj(_nativeWidget),
                    mupdf.mupdf.pdf_new_name("DV"));
                return dv ?? "";
            }
        }

        /// <summary>
        /// Field flags.
        /// </summary>
        public int FieldFlags => mupdf.mupdf.pdf_field_flags(mupdf.mupdf.pdf_annot_obj(_nativeWidget));

        /// <summary>
        /// Widget rectangle.
        /// </summary>
        public Rect Rect
        {
            get
            {
                var r = mupdf.mupdf.pdf_bound_annot(_nativeWidget);
                return new Rect(r.x0, r.y0, r.x1, r.y1);
            }
        }

        /// <summary>
        /// Widget xref number.
        /// </summary>
        public int Xref => mupdf.mupdf.pdf_to_num(mupdf.mupdf.pdf_annot_obj(_nativeWidget));

        /// <summary>
        /// Check if field is read only.
        /// </summary>
        public bool IsReadOnly
        {
            get
            {
                int flags = FieldFlags;
                return (flags & 1) != 0; // PDF_FIELD_IS_READ_ONLY
            }
        }

        /// <summary>
        /// Check if field is required.
        /// </summary>
        public bool IsRequired
        {
            get
            {
                int flags = FieldFlags;
                return (flags & 2) != 0;
            }
        }

        /// <summary>
        /// Maximum text length.
        /// </summary>
        public int MaxLen
        {
            get
            {
                var ml = mupdf.mupdf.pdf_dict_get_int(mupdf.mupdf.pdf_annot_obj(_nativeWidget),
                    mupdf.mupdf.pdf_new_name("MaxLen"));
                return ml;
            }
        }

        /// <summary>
        /// Check if text field is multi-line.
        /// </summary>
        public bool IsMultiline
        {
            get
            {
                int flags = FieldFlags;
                return (flags & (1 << 12)) != 0;
            }
        }

        /// <summary>
        /// Check if text field is a comb field.
        /// </summary>
        public bool IsComb
        {
            get
            {
                int flags = FieldFlags;
                return (flags & (1 << 24)) != 0;
            }
        }

        /// <summary>
        /// Next form field.
        /// </summary>
        public Widget Next
        {
            get
            {
                var next = mupdf.mupdf.pdf_next_widget(_nativeWidget);
                return next.m_internal != null ? new Widget(next, Parent) : null;
            }
        }

        /// <summary>
        /// Choice field option values.
        /// </summary>
        public List<string> ChoiceValues
        {
            get
            {
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
        }

        /// <summary>
        /// Calculate script (C) from the additional-actions dictionary.
        /// </summary>
        public string ScriptCalc => GetScript("C");
        /// <summary>
        /// Format script (F) from the additional-actions dictionary.
        /// </summary>
        public string ScriptFormat => GetScript("F");
        /// <summary>
        /// Keystroke script (K) from the additional-actions dictionary.
        /// </summary>
        public string ScriptKeystroke => GetScript("K");
        /// <summary>
        /// Validation script (V) from the additional-actions dictionary.
        /// </summary>
        public string ScriptValidation => GetScript("V");
        /// <summary>
        /// Blur script (Bl) from the additional-actions dictionary.
        /// </summary>
        public string ScriptBlur => GetScript("Bl");
        /// <summary>
        /// Focus script (Fo) from the additional-actions dictionary.
        /// </summary>
        public string ScriptFocus => GetScript("Fo");

        // ─── Methods ────────────────────────────────────────────────────

        /// <summary>
        /// Set the field value.
        /// </summary>
        public void SetValue(string value) => FieldValue = value;

        /// <summary>
        /// Set widget rectangle.
        /// </summary>
        public void SetRect(Rect rect)
        {
            mupdf.mupdf.pdf_set_annot_rect(_nativeWidget, rect.ToFzRect());
            mupdf.mupdf.pdf_update_annot(_nativeWidget);
        }

        /// <summary>
        /// Set choice field option values.
        /// </summary>
        public void SetChoiceValues(List<string> values)
        {
            if (values == null || values.Count == 0) return;
            var obj = mupdf.mupdf.pdf_annot_obj(_nativeWidget);
            var pdf = Parent.Parent.NativePdfDocument;
            var optArr = mupdf.mupdf.pdf_new_array(pdf, values.Count);
            foreach (var val in values)
                mupdf.mupdf.pdf_array_push(optArr, mupdf.mupdf.pdf_new_text_string(val));
            mupdf.mupdf.pdf_dict_puts(obj, "Opt", optArr);
            mupdf.mupdf.pdf_update_annot(_nativeWidget);
        }

        /// <summary>
        /// Update widget appearance.
        /// </summary>
        public void Update()
        {
            mupdf.mupdf.pdf_update_annot(_nativeWidget);
        }

        /// <summary>
        /// Button widget on/checked state from the toggle flag.
        /// </summary>
        public bool OnState => mupdf.mupdf.pdf_toggle_widget(_nativeWidget) != 0;

        /// <summary>
        /// Reset field to its default value.
        /// </summary>
        public void Reset()
        {
            var obj = mupdf.mupdf.pdf_annot_obj(_nativeWidget);
            var dv = mupdf.mupdf.pdf_dict_get(obj, mupdf.mupdf.pdf_new_name("DV"));
            if (dv.m_internal != null)
                mupdf.mupdf.pdf_dict_put(obj, mupdf.mupdf.pdf_new_name("V"), dv);
            else
                mupdf.mupdf.pdf_dict_del(obj, mupdf.mupdf.pdf_new_name("V"));
            mupdf.mupdf.pdf_update_annot(_nativeWidget);
        }

        /// <summary>
        /// Widget Pixmap.
        /// </summary>
        public Pixmap GetPixmap(Matrix matrix = null, Colorspace cs = null, bool alpha = false)
        {
            var ctm = (matrix ?? Matrix.Identity).ToFzMatrix();
            var colorspace = (cs ?? Colorspace.CsRGB).ToFzColorspace();
            var pix = mupdf.mupdf.pdf_new_pixmap_from_annot(_nativeWidget, ctm, colorspace, new mupdf.FzSeparations(), alpha ? 1 : 0);
            return new Pixmap(pix);
        }

        private string GetScript(string trigger)
        {
            var obj = mupdf.mupdf.pdf_annot_obj(_nativeWidget);
            var aa = mupdf.mupdf.pdf_dict_get(obj, mupdf.mupdf.pdf_new_name("AA"));
            if (aa.m_internal == null) return null;
            var action = mupdf.mupdf.pdf_dict_gets(aa, trigger);
            if (action.m_internal == null) return null;
            var js = mupdf.mupdf.pdf_dict_get(action, mupdf.mupdf.pdf_new_name("JS"));
            if (js.m_internal == null) return null;
            if (mupdf.mupdf.pdf_is_stream(js) != 0)
            {
                var buf = mupdf.mupdf.pdf_load_stream(js);
                return System.Text.Encoding.UTF8.GetString(buf.fz_buffer_extract());
            }
            return mupdf.mupdf.pdf_to_text_string(js);
        }

        // ─── IDisposable ────────────────────────────────────────────────

        /// <summary>
        /// Releases resources used by this widget wrapper.
        /// </summary>
        public void Dispose()
        {
            if (!_disposed) { _disposed = true; }
            GC.SuppressFinalize(this);
        }

        ~Widget() { Dispose(); }

        /// <summary>
        /// Returns a string representation of this widget.
        /// </summary>
        public override string ToString() => $"Widget('{FieldTypeString}', '{FieldName}')";
    }
}
