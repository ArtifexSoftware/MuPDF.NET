using System;
using System.Collections.Generic;

namespace MuPDF.NET
{

    /// <summary>
    /// form-field hierarchy: flat radio groups, parent/Kids, empty /Annots.
    /// </summary>

    public partial class Page
    {

        /// <summary>
        /// PDF keys that must not stay on a widget once it has a parent field
        /// (<c>NON_WIDGET_KEYS</c>).
        /// </summary>

        internal static readonly string[] NonWidgetKeys =
        {
            "AA", "FT", "Ff", "DA", "Opt", "TU", "I", "TM", "DV", "MaxLen",
            "Q", "DS", "DR", "TI", "RV", "T", "V",
        };

        static bool PdfNull(mupdf.PdfObj obj)
            => obj == null || obj.m_internal == null || obj.pdf_is_null() != 0;

        static bool PdfIsArray(mupdf.PdfObj obj)
            => obj != null && obj.m_internal != null && mupdf.mupdf.pdf_is_array(obj) != 0;

        static bool PdfIsDict(mupdf.PdfObj obj)
            => obj != null && obj.m_internal != null && mupdf.mupdf.pdf_is_dict(obj) != 0;

        static void RemoveObjectFromArray(mupdf.PdfObj array, int xref)
        {
            if (!PdfIsArray(array))
                return;
            int count = mupdf.mupdf.pdf_array_len(array);
            for (int i = count - 1; i >= 0; i--)
            {
                using var item = mupdf.mupdf.pdf_array_get(array, i);
                if (mupdf.mupdf.pdf_to_num(item) == xref)
                    mupdf.mupdf.pdf_array_delete(array, i);
            }
        }

        /// <summary>
        /// Identify and convert "flat" radio button groups (<c>Page.repair_flat_rbgs</c>).
        /// </summary>

        public void RepairFlatRbgs()
        {
            Document doc = RequireParent();
            if (!doc.IsPDF)
                return;
            var pdoc = doc.NativePdfDocument;
            using var trailer = mupdf.mupdf.pdf_trailer(pdoc);
            var fields = Helpers.PdfDictGetl(trailer, "Root", "AcroForm", "Fields");
            if (!PdfIsArray(fields) || mupdf.mupdf.pdf_array_len(fields) == 0)
                return;

            var pdfPage = NativePdfPage;
            using var pageObj = pdfPage.obj();
            var annots = Helpers.PdfDictGets(pageObj, "Annots");
            if (!PdfIsArray(annots))
                return;

            int IsInFields(mupdf.PdfObj widget)
            {
                int wxref = mupdf.mupdf.pdf_to_num(widget);
                int n = mupdf.mupdf.pdf_array_len(fields);
                for (int i = 0; i < n; i++)
                {
                    using var f = mupdf.mupdf.pdf_array_get(fields, i);
                    if (mupdf.mupdf.pdf_to_num(f) == wxref)
                        return i;
                }
                return -1;
            }

            var flatWidgets = new Dictionary<string, List<(mupdf.PdfObj widget, int idx)>>();
            int annotCount = mupdf.mupdf.pdf_array_len(annots);
            for (int i = 0; i < annotCount; i++)
            {
                var w = mupdf.mupdf.pdf_array_get(annots, i);
                if (w.pdf_field_type() != (int)WidgetType.RadioButton)
                    continue;
                var parent = Helpers.PdfDictGets(w, "Parent");
                if (!PdfNull(parent))
                    continue;
                string name = mupdf.mupdf.pdf_to_text_string(Helpers.PdfDictGets(w, "T"));
                if (string.IsNullOrEmpty(name))
                    continue;
                if (!flatWidgets.TryGetValue(name, out var list))
                {
                    list = new List<(mupdf.PdfObj, int)>();
                    flatWidgets[name] = list;
                }
                list.Add((w, IsInFields(w)));
            }

            foreach (var kv in flatWidgets)
            {
                var widgets = kv.Value;
                int count = 0;
                foreach (var item in widgets)
                {
                    if (item.idx >= 0)
                        count++;
                }
                if (count != 1)
                {
                    Helpers.message($"Skipping flat RBG '{kv.Key}' with count {count}");
                    continue;
                }

                int nxref = doc.GetNewXref();
                doc.UpdateObject(nxref, "<<>>");
                using var parent = mupdf.mupdf.pdf_new_indirect(pdoc, nxref, 0);
                using var kids = Helpers.PdfDictPutArray(parent, "Kids", widgets.Count);
                int fieldIdx = -1;
                foreach (var (widget, idx) in widgets)
                {
                    int wxref = mupdf.mupdf.pdf_to_num(widget);
                    if (idx >= 0)
                        fieldIdx = idx;
                    foreach (string key in NonWidgetKeys)
                    {
                        var wObj = Helpers.PdfDictGets(widget, key);
                        var pObj = Helpers.PdfDictGets(parent, key);
                        if (PdfNull(wObj))
                            continue;
                        if (PdfNull(pObj))
                            Helpers.PdfDictPuts(parent, key, wObj);
                        Helpers.PdfDictDel(widget, key);
                    }
                    Helpers.PdfDictPuts(widget, "Parent", mupdf.mupdf.pdf_new_indirect(pdoc, nxref, 0));
                    mupdf.mupdf.pdf_array_push(kids, mupdf.mupdf.pdf_new_indirect(pdoc, wxref, 0));
                }
                if (fieldIdx >= 0)
                    mupdf.mupdf.pdf_array_put(fields, fieldIdx, parent);
            }

            mupdf.mupdf.pdf_sync_annots(pdfPage);
        }

        /// <summary>
        /// Ensure AcroForm field parents exist for a dotted field name
        /// (<c>Page._ensure_field_hierarchy</c>).
        /// Returns the xref of the last name segment, or 0 if the name is empty.
        /// </summary>

        internal int EnsureFieldHierarchy(string fieldName)
        {
            if (string.IsNullOrEmpty(fieldName))
                return 0;
            string[] parts = fieldName.Split('.');
            foreach (string part in parts)
            {
                if (part.Length == 0)
                    throw new ValueErrorException($"Invalid field name '{fieldName}'.");
            }

            Document doc = RequireParent();
            var pdfdoc = doc.NativePdfDocument;
            using var trailer = mupdf.mupdf.pdf_trailer(pdfdoc);
            var root = Helpers.PdfDictGets(trailer, "Root");
            var acro = Helpers.PdfDictGets(root, "AcroForm");
            if (!PdfIsDict(acro))
                acro = Helpers.PdfDictPutDict(root, "AcroForm", 5);
            var rootArray = Helpers.PdfDictGets(acro, "Fields");
            if (!PdfIsArray(rootArray))
                rootArray = Helpers.PdfDictPutArray(acro, "Fields", 1);

            (int xref, mupdf.PdfObj obj) NameInArray(mupdf.PdfObj array, string name)
            {
                if (!PdfIsArray(array))
                    return (0, null);
                int n = mupdf.mupdf.pdf_array_len(array);
                for (int i = 0; i < n; i++)
                {
                    var item = mupdf.mupdf.pdf_array_get(array, i);
                    string tName = mupdf.mupdf.pdf_to_text_string(Helpers.PdfDictGets(item, "T"));
                    if (tName == name)
                        return (mupdf.mupdf.pdf_to_num(item), item);
                }
                return (0, null);
            }

            int parentXref = 0;
            for (int i = 0; i < parts.Length; i++)
            {
                string part = parts[i];
                mupdf.PdfObj kidsArray;
                mupdf.PdfObj parent = null;
                if (i == 0)
                {
                    kidsArray = Helpers.PdfDictGets(acro, "Fields");
                }
                else
                {
                    parent = mupdf.mupdf.pdf_load_object(pdfdoc, parentXref);
                    kidsArray = Helpers.PdfDictGets(parent, "Kids");
                    if (!PdfIsArray(kidsArray) || mupdf.mupdf.pdf_array_len(kidsArray) == 0)
                        kidsArray = Helpers.PdfDictPutArray(parent, "Kids", 1);
                }

                var (itemXref, itemObj) = NameInArray(kidsArray, part);
                if (parent == null && itemObj != null && !PdfIsArray(Helpers.PdfDictGets(itemObj, "Kids")))
                {
                    int newXref = doc.GetNewXref();
                    doc.UpdateObject(newXref, $"<</T ({part})>>");
                    using var newObj = mupdf.mupdf.pdf_new_indirect(pdfdoc, newXref, 0);
                    mupdf.mupdf.pdf_array_push(kidsArray, newObj);
                    itemXref = newXref;
                }
                if (itemXref != 0)
                {
                    parentXref = itemXref;
                    continue;
                }

                int partXref = doc.GetNewXref();
                if (parentXref != 0)
                    doc.UpdateObject(partXref, $"<</Parent {parentXref} 0 R /T ({part})>>");
                else
                    doc.UpdateObject(partXref, $"<</T ({part})>>");
                using var partObj = mupdf.mupdf.pdf_new_indirect(pdfdoc, partXref, 0);
                mupdf.mupdf.pdf_array_push(kidsArray, partObj);
                parentXref = partXref;
            }
            return parentXref;
        }

        /// <summary>
        /// Move a widget under its field parent (<c>Page._adjust_widget</c>).
        /// </summary>

        internal Annot AdjustWidget(Widget widget)
        {
            if (widget == null)
                return null;
            Document doc = RequireParent();
            int parentXref = EnsureFieldHierarchy(widget.FieldName);
            Annot annot = widget.BoundAnnot;
            if (parentXref == 0)
                return annot;

            int annotXref = widget.BoundAnnot != null ? widget.BoundAnnot.Xref : widget.Xref;
            var pdfdoc = doc.NativePdfDocument;
            using var trailer = mupdf.mupdf.pdf_trailer(pdfdoc);
            var acroform = Helpers.PdfDictGetl(trailer, "Root", "AcroForm");
            var fields = Helpers.PdfDictGets(acroform, "Fields");
            bool found = false;
            if (PdfIsArray(fields))
            {
                int n = mupdf.mupdf.pdf_array_len(fields);
                for (int i = 0; i < n; i++)
                {
                    using var item = mupdf.mupdf.pdf_array_get(fields, i);
                    if (mupdf.mupdf.pdf_to_num(item) == annotXref)
                    {
                        mupdf.mupdf.pdf_array_delete(fields, i);
                        found = true;
                        break;
                    }
                }
            }
            if (!found)
                throw new ValueErrorException(
                    $"Unexpected: Widget {annotXref} not found in root Fields array.");

            // Widget.field_value is the object attribute (InsertFieldValue), not pdf_field_value.
            string radioValue = widget.InsertFieldValue ?? "";
            if (widget.FieldType == (int)WidgetType.RadioButton)
            {
                doc.XrefSetKey(annotXref, "AS", "/" + radioValue);
                var apn = doc.XrefGetKey(annotXref, "AP/N");
                string apnVal = apn.Item2;
                if (!string.IsNullOrEmpty(apnVal))
                    doc.XrefSetKey(annotXref, "AP/N", apnVal.Replace("/Yes", "/" + radioValue));
            }

            // Reload after xref_set_key: that path may pdf_update_object a replacement dict.
            using var parentObj = mupdf.mupdf.pdf_new_indirect(pdfdoc, parentXref, 0);
            using var annotObj = mupdf.mupdf.pdf_new_indirect(pdfdoc, annotXref, 0);

            if (widget.FieldType == (int)WidgetType.RadioButton)
                Helpers.PdfDictDel(annotObj, "V");

            var kidsArray = Helpers.PdfDictGets(parentObj, "Kids");
            if (!PdfIsArray(kidsArray))
                kidsArray = Helpers.PdfDictPutArray(parentObj, "Kids", 1);
            mupdf.mupdf.pdf_array_push(kidsArray, annotObj);
            Helpers.PdfDictPuts(annotObj, "Parent", mupdf.mupdf.pdf_new_indirect(pdfdoc, parentXref, 0));
            Helpers.PdfDictPuts(annotObj, "P", mupdf.mupdf.pdf_new_indirect(pdfdoc, Xref, 0));

            foreach (string key in NonWidgetKeys)
            {
                var widgetItm = Helpers.PdfDictGets(annotObj, key);
                var parentItm = Helpers.PdfDictGets(parentObj, key);
                if (!PdfNull(widgetItm) && PdfNull(parentItm))
                    Helpers.PdfDictPuts(parentObj, key, widgetItm);
                Helpers.PdfDictDel(annotObj, key);
            }

            // Use InsertScriptCalc — ScriptCalc would re-read /AA/C after keys moved to parent.
            if (!string.IsNullOrEmpty(widget.InsertScriptCalc))
            {
                var co = Helpers.PdfDictGets(acroform, "CO");
                if (!PdfIsArray(co))
                    co = Helpers.PdfDictPutArray(acroform, "CO", 1);
                bool coFound = false;
                int coLen = mupdf.mupdf.pdf_array_len(co);
                for (int i = 0; i < coLen; i++)
                {
                    using var item = mupdf.mupdf.pdf_array_get(co, i);
                    if (mupdf.mupdf.pdf_to_num(item) == parentXref)
                    {
                        coFound = true;
                        break;
                    }
                }
                if (!coFound)
                    mupdf.mupdf.pdf_array_push(co, parentObj);
            }

            if (widget.FieldType == (int)WidgetType.CheckBox)
            {
                string fv = widget.FieldValue;
                if (fv == "Off" || string.IsNullOrEmpty(fv) || string.Equals(fv, "false", StringComparison.OrdinalIgnoreCase))
                {
                    doc.XrefSetKey(parentXref, "V", "/Off");
                    doc.XrefSetKey(annotXref, "AS", "/Off");
                }
                else
                {
                    doc.XrefSetKey(parentXref, "V", "/Yes");
                    doc.XrefSetKey(annotXref, "AS", "/Yes");
                }
                Helpers.PdfDictDel(parentObj, "AS");
            }

            if (widget.FieldType == (int)WidgetType.RadioButton)
            {
                Helpers.PdfDictDel(annotObj, "V");
                string parentV = mupdf.mupdf.pdf_to_name(Helpers.PdfDictGets(parentObj, "V"));
                if (!string.IsNullOrEmpty(parentV) && parentV != "Off")
                    Helpers.PdfDictPuts(annotObj, "AS", mupdf.mupdf.pdf_new_name("Off"));
                else
                {
                    using var onValue = mupdf.mupdf.pdf_new_name(radioValue);
                    Helpers.PdfDictPuts(annotObj, "AS", onValue);
                    Helpers.PdfDictPuts(parentObj, "V", mupdf.mupdf.pdf_new_name(radioValue));
                }
            }

            doc.need_appearances(false);
            return annot;
        }

        /// <summary>Drop an empty page <c>/Annots</c> array (<c>Page._delete_empty_annots</c>).</summary>
        internal void DeleteEmptyAnnots()
        {
            var pdfPage = NativePdfPage;
            if (pdfPage?.m_internal == null)
                return;
            using var pageObj = pdfPage.obj();
            var annots = Helpers.PdfDictGets(pageObj, "Annots");
            if (PdfIsArray(annots) && mupdf.mupdf.pdf_array_len(annots) == 0)
                Helpers.PdfDictDel(pageObj, "Annots");
        }

        internal void repair_flat_rbgs() => RepairFlatRbgs();
        internal int _ensure_field_hierarchy(string fieldName) => EnsureFieldHierarchy(fieldName);
        internal Annot _adjust_widget(Widget widget) => AdjustWidget(widget);
        internal void _delete_empty_annots() => DeleteEmptyAnnots();
    }
}
