﻿using mupdf;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace MuPDF.NET
{
    public class Annot
    {
        static Annot()
        {
            Utils.InitApp();
        }

        internal PdfAnnot _nativeAnnotion;

        public bool IsOwner { get; set; }

        public bool ThisOwn { get; set; }

        public bool Yielded { get; set; }

        delegate string LE_FUNCTION(
            Annot annot,
            Point p1,
            Point p2,
            bool lr,
            float[] fillColor
        );

        private Page _parent;

        public Rect Rect
        {
            get { return new Rect(mupdf.mupdf.pdf_bound_annot(_nativeAnnotion)); }
        }

        internal Page Parent
        {
            get { return _parent; }
            set { _parent = value; }
        }

        public Rect ApnBbox
        {
            get
            {
                PdfAnnot annot = _nativeAnnotion;
                PdfObj annotObj = annot.pdf_annot_obj();
                PdfObj ap = Utils.pdf_dict_getl(annotObj, new string[] { "AP", "N" });

                FzRect val = null;
                if (ap.m_internal == null)
                    val = new FzRect(FzRect.Fixed.Fixed_EMPTY);
                else
                    val = ap.pdf_dict_get_rect(new PdfObj("BBOX"));

                Rect ret = new Rect(val);
                ret = ret * Parent.TransformationMatrix;
                ret = ret * Parent.DerotationMatrix;

                return ret;
            }
        }

        public Matrix ApnMatrix
        {
            get
            {
                PdfAnnot annot = _nativeAnnotion;
                PdfObj ap = Utils.pdf_dict_getl(annot.pdf_annot_obj(), new string[] { "AP", "N" });
                if (ap.m_internal == null)
                {
                    return new Matrix();
                }

                FzMatrix mat = ap.pdf_dict_get_matrix(new PdfObj("MATRIX"));

                return new Matrix(mat);
            }
        }

        public string BlendMode
        {
            get
            {
                PdfAnnot annot = _nativeAnnotion;
                PdfObj annotObj = annot.pdf_annot_obj();
                PdfObj obj = annotObj.pdf_dict_get(new PdfObj("BM"));
                string blendMode = "";

                if (obj.m_internal != null)
                {
                    blendMode = obj.pdf_to_name();
                    return blendMode;
                }

                obj = Utils.pdf_dict_getl(
                    annotObj,
                    new string[] { "AP", "N", "Resources", "ExtGState" }
                );
                
                if (obj.pdf_is_dict() != 0)
                {
                    int n = obj.pdf_dict_len();
                    for (int i = 0; i < n; i++)
                    {
                        PdfObj obj1 = obj.pdf_dict_get_val(i);
                        if (obj1.pdf_is_dict() != 0)
                        {
                            int m = obj1.pdf_dict_len();
                            for (int j = 0; j < m; j++)
                            {   
                                PdfObj obj2 = obj1.pdf_dict_get_key(j);
                                if (obj2.pdf_objcmp(new PdfObj("BM")) == 0)
                                {
                                    blendMode = obj1.pdf_dict_get_val(j).pdf_to_name();
                                    return blendMode;
                                }
                            }
                        }
                    }
                }

                return blendMode;
            }
        }

        public FileInfo FileInfo
        {
            get
            {
                FileInfo ret = new FileInfo();
                int length = -1;
                int size = -1;
                string filename = "";
                string desc = "";

                PdfAnnot annot = _nativeAnnotion;
                PdfObj annotObj = annot.pdf_annot_obj();
                pdf_annot_type type = annot.pdf_annot_type();

                if ((PdfAnnotType)type != PdfAnnotType.PDF_ANNOT_FILE_ATTACHMENT)
                {
                    throw new Exception("bad annot type");
                }

                PdfObj stream = Utils.pdf_dict_getl(annotObj, new string[] { "FS", "EF", "F" });
                if (stream == null)
                {
                    throw new Exception("bad PDF: file entry not found");
                }

                PdfObj fs = annotObj.pdf_dict_get(new PdfObj("FS"));
                PdfObj o = fs.pdf_dict_get(new PdfObj("UF"));
                if (o != null)
                {
                    filename = o.pdf_to_text_string();
                }
                else
                {
                    o = fs.pdf_dict_get(new PdfObj("F"));
                    if (o != null)
                    {
                        filename = o.pdf_to_text_string();
                    }
                }

                o = fs.pdf_dict_get(new PdfObj("Desc"));
                if (o != null)
                    desc = o.pdf_to_text_string();

                o = stream.pdf_dict_get(new PdfObj("Length"));
                if (o != null)
                {
                    length = o.pdf_to_int();
                }

                o = Utils.pdf_dict_getl(stream, new string[] { "Params", "Size" });
                if (o != null)
                {
                    size = o.pdf_to_int();
                }

                ret.FileName = EscapeStrFromStr(filename);
                ret.Desc = EscapeStrFromStr(desc);
                ret.Size = size;
                ret.Length = length;

                return ret;
            }
        }

        public bool HasPopup
        {
            get
            {
                PdfAnnot annot = _nativeAnnotion;
                PdfObj obj = annot.pdf_annot_obj().pdf_dict_get(new PdfObj("Popup"));

                if (obj.m_internal != null)
                    return true;

                return false;
            }
        }

        public AnnotInfo Info
        {
            get
            {
                AnnotInfo res = new AnnotInfo();
                PdfAnnot annot = _nativeAnnotion;
                PdfObj annotObj = annot.pdf_annot_obj();

                res.Content = annot.pdf_annot_contents();

                PdfObj o = annotObj.pdf_dict_get(new PdfObj("Name"));
                res.Name = o.pdf_to_name();

                o = annotObj.pdf_dict_get(new PdfObj("T"));
                res.Title = o.pdf_to_text_string();

                o = annotObj.pdf_dict_gets("CreationDate");
                res.CreationDate = o.pdf_to_text_string();

                o = annotObj.pdf_dict_get(new PdfObj("M"));
                res.ModDate = o.pdf_to_text_string();

                o = annotObj.pdf_dict_gets("Subj");
                res.Subject = o.pdf_to_text_string();

                o = annotObj.pdf_dict_gets("NM");
                res.Id = o.pdf_to_text_string();

                return res;
            }
        }

        public int Flags
        {
            get { return _nativeAnnotion.pdf_annot_flags(); }
        }

        public bool IsOpen
        {
            get { return _nativeAnnotion.pdf_annot_is_open() == 0 ? false : true; }
        }

        public string Language
        {
            get
            {
                fz_text_language lang;
                PdfAnnot annot = _nativeAnnotion;
                lang = mupdf.mupdf.pdf_annot_language(annot);

                if (lang == fz_text_language.FZ_LANG_UNSET)
                    return null;
                
                return mupdf.mupdf.fz_string_from_text_language("", lang);
            }
        }

        public int Xref
        {
            get
            {
                PdfAnnot annot = _nativeAnnotion;
                return annot.pdf_annot_obj().pdf_to_num();
            }
        }

        public (PdfLineEnding, PdfLineEnding) LineEnds
        {
            get
            {
                PdfAnnot annot = _nativeAnnotion;
                if (annot.pdf_annot_has_line_ending_styles() == 0)
                    return (0, 0);

                PdfLineEnding lstart = (PdfLineEnding)annot.pdf_annot_line_start_style();
                PdfLineEnding lend = (PdfLineEnding)annot.pdf_annot_line_end_style();

                return (lstart, lend);
            }
        }

        /// <summary>
        /// return MuPDFAnnot or Widget
        /// </summary>
        public dynamic Next
        {
            get
            {
                try
                {
                    PdfAnnot annot = _nativeAnnotion;
                    pdf_annot_type type = annot.pdf_annot_type();

                    if (type != pdf_annot_type.PDF_ANNOT_WIDGET)
                        annot = annot.pdf_next_annot();
                    else
                        annot = annot.pdf_next_widget();

                    Annot val = (annot == null ? null : new Annot(annot, _parent));
                    if (val == null)
                    {
                        return null;
                    }
                    
                    val.IsOwner = true;
                    if (val.GetParent() == null)
                        throw new Exception("null parent");
                    
                    val.Parent.AnnotRefs[val.GetHashCode()] = val;

                    if (val.Type.Item1 == PdfAnnotType.PDF_ANNOT_WIDGET)
                    {
                        Widget widget = new Widget(Parent);
                        Utils.FillWidget(val, widget);
                        return widget;
                    }
                    else if (val.Type.Item1 == PdfAnnotType.PDF_ANNOT_UNKNOWN)
                    {
                        return null;
                    }

                        return val;
                }
                catch(Exception e)
                {
                    throw new Exception(e.Message);
                }
            }
        }

        public float Opacity
        {
            get
            {
                float opy = -1.0f;
                PdfAnnot annot = _nativeAnnotion;
                PdfObj ca = annot.pdf_annot_obj().pdf_dict_get(new PdfObj("CA"));

                if (ca.pdf_is_number() != 0)
                    opy = ca.pdf_to_real();
                
                return opy;
            }
        }

        public Rect PopupRect
        {
            get
            {
                PdfAnnot annot = _nativeAnnotion;
                PdfObj annotObj = annot.pdf_annot_obj();
                FzRect rect = new FzRect(FzRect.Fixed.Fixed_INFINITE);
                PdfObj obj = annotObj.pdf_dict_get(new PdfObj("Popup"));

                if (obj.m_internal != null)
                {
                    rect = obj.pdf_dict_get_rect(new PdfObj("Rect"));
                }

                Rect val = new Rect(rect);
                val = val * Parent.TransformationMatrix;
                val *= Parent.DerotationMatrix;

                return val;
            }
        }

        public int PopupXref
        {
            get
            {
                int xref = 0;
                PdfAnnot annot = _nativeAnnotion;
                PdfObj annotObj = annot.pdf_annot_obj();
                PdfObj obj = annotObj.pdf_dict_get(new PdfObj("Popup"));

                if (obj.m_internal != null)
                {
                    xref = obj.pdf_to_num();
                }

                return xref;
            }
        }

        public (float, float, float, float) RectDelta
        {
            get
            {
                PdfObj annotObj = _nativeAnnotion.pdf_annot_obj();
                PdfObj arr = annotObj.pdf_dict_get(new PdfObj("RD"));

                if (arr.pdf_array_len() == 4)
                {
                    return (
                        arr.pdf_array_get(0).pdf_to_real(),
                        arr.pdf_array_get(1).pdf_to_real(),
                        arr.pdf_array_get(2).pdf_to_real(),
                        arr.pdf_array_get(3).pdf_to_real()
                    );
                }
                else
                    return (0, 0, 0, 0);
            }
        }

        public int Rotation
        {
            get
            {
                PdfAnnot annot = _nativeAnnotion;
                PdfObj rotation = annot.pdf_annot_obj().pdf_dict_get(new PdfObj("Rotate"));

                if (rotation.m_internal == null)
                    return -1;

                return rotation.pdf_to_int();
            }
        }

        public Border Border
        {
            get
            {
                PdfAnnotType atype = Type.Item1;
                if (
                    !(
                        new List<PdfAnnotType>()
                        {
                            PdfAnnotType.PDF_ANNOT_CIRCLE,
                            PdfAnnotType.PDF_ANNOT_FREE_TEXT,
                            PdfAnnotType.PDF_ANNOT_INK,
                            PdfAnnotType.PDF_ANNOT_LINE,
                            PdfAnnotType.PDF_ANNOT_POLY_LINE,
                            PdfAnnotType.PDF_ANNOT_POLYGON,
                            PdfAnnotType.PDF_ANNOT_SQUARE
                        }
                    ).Contains(atype)
                )
                {
                    return new Border();
                }

                PdfObj annotObj = _nativeAnnotion.pdf_annot_obj();

                return GetBorderFromAnnot(annotObj);
            }
        }

        public Color Colors
        {
            get
            {
                try
                {
                    PdfAnnot annot = _nativeAnnotion;
                    return GetColorFromAnnot(annot.pdf_annot_obj());
                }
                catch
                {
                    throw;
                }
            }
        }

        public dynamic Vertices
        {
            get
            {
                PdfAnnot annot = _nativeAnnotion;
                FzMatrix pageCtm = new FzMatrix();
                FzRect dummy = new FzRect(0);
                annot.pdf_annot_page().pdf_page_transform(dummy, pageCtm);
                FzMatrix derot = Utils.DerotatePageMatrix(annot.pdf_annot_page());
                pageCtm = mupdf.mupdf.fz_concat(pageCtm, derot);

                PdfObj obj = annot.pdf_annot_obj().pdf_dict_get(new PdfObj("Vertices"));
                if (obj == null)
                    obj = annot.pdf_annot_obj().pdf_dict_get(new PdfObj("L"));
                if (obj == null)
                    obj = annot.pdf_annot_obj().pdf_dict_get(new PdfObj("QuadPoints"));
                if (obj == null)
                    obj = annot.pdf_annot_obj().pdf_dict_get(new PdfObj("CL"));

                if (obj.m_internal != null)
                {
                    List<Point> ret = new List<Point>();
                    for (int i = 0; i < obj.pdf_array_len(); i += 2)
                    {
                        float x = obj.pdf_array_get(i).pdf_to_real();
                        float y = obj.pdf_array_get(i + 1).pdf_to_real();
                        FzPoint p = new FzPoint(x, y);
                        p = mupdf.mupdf.fz_transform_point(p, pageCtm);
                        ret.Add(new Point(p));
                    }

                    return ret;
                }
                else
                {
                    List<List<Point>> ret = new List<List<Point>>();
                    for (int j = 0; j < obj.pdf_array_len(); j++)
                    {
                        List<Point> t = new List<Point>();
                        PdfObj o = obj.pdf_array_get(j);
                        for (int i = 0; i < o.pdf_array_len(); i += 2)
                        {
                            float x = o.pdf_array_get(i).pdf_to_real();
                            float y = o.pdf_array_get(i + 1).pdf_to_real();
                            FzPoint p = new FzPoint(x, y);
                            p = mupdf.mupdf.fz_transform_point(p, pageCtm);
                            t.Add(new Point(p));
                        }
                        ret.Add(t);
                    }

                    return ret;
                }
            }
        }

        public static string EscapeStrFromStr(string c)
        {
            if (c == null || c == "")
                return "";
            byte[] b = Encoding.UTF8.GetBytes(c);
            string ret = "";
            foreach (byte bb in b)
            {
                ret += (char)bb;
            }

            return ret;
        }

        public static string UnicodeFromStr(dynamic s)
        {
            string ret = "";
            if (s is string && string.IsNullOrEmpty(s))
                return "";

            if (s is byte[])
                ret = Encoding.UTF8.GetString(s);

            return ret;
        }

        public (PdfAnnotType, string, string) Type
        {
            get
            {
                if (_nativeAnnotion.m_internal == null)
                    return (PdfAnnotType.PDF_ANNOT_UNKNOWN, null, null);

                pdf_annot_type t = _nativeAnnotion.pdf_annot_type();
                string c = mupdf.mupdf.pdf_string_from_annot_type(t);
                PdfObj obj = _nativeAnnotion.pdf_annot_obj().pdf_dict_gets("IT");

                if (obj.m_internal == null || obj.pdf_is_name() != 0)
                    return ((PdfAnnotType)t, c, null);
                string it = obj.pdf_to_name();

                return ((PdfAnnotType)t, c, it);
            }
        }

        public override string ToString()
        {
            return string.Format("'{0}' annotation on", 0);
        }

        public Annot(PdfAnnot annotion, Page parent)
        {
            _nativeAnnotion = new PdfAnnot(annotion);
            _parent = parent;
            IsOwner = true;
        }

        public void Erase()
        {
            IsOwner = false;
        }

        internal AnnotValues GetRedactValues()
        {
            PdfAnnot annot = _nativeAnnotion;

            AnnotValues values = new AnnotValues();
            try
            {
                PdfObj obj = annot.pdf_annot_obj().pdf_dict_gets("RO");
                if (obj.m_internal != null)
                {
                    int xref = obj.pdf_to_num();
                    values.Xref = xref;
                }

                obj = annot.pdf_annot_obj().pdf_dict_gets("OverlayText");
                if (obj.m_internal != null)
                {
                    string text = obj.pdf_to_text_string();
                    values.Text = text;
                }
                else
                {
                    values.Text = "";
                }

                int align = 0;
                obj = annot.pdf_annot_obj().pdf_dict_gets("Q");
                if (obj.m_internal != null)
                {
                    align = obj.pdf_to_int();
                }
                values.Align = align;
            }
            catch (Exception)
            {
                return null;
            }

            if (values == null)
                return values;

            values.Rect = this.Rect;
            List<float> tColor = new List<float>();
            (tColor, values.FontName, values.FontSize) = ParseData(this);
            values.TextColor = tColor.ToArray();
            values.Fill = new float[Colors.Fill.Length];
            Array.Copy(Colors.Fill, values.Fill, values.Fill.Length);

            return values;
        }

        public static (List<float>, string, float) ParseData(Annot annot)
        {
            PdfObj obj = annot._nativeAnnotion.pdf_annot_obj();
            PdfDocument pdf = obj.pdf_get_bound_document();
            string da_string = "";
            try
            {
                da_string = obj.pdf_dict_get_inheritable(new PdfObj("DA")).pdf_to_text_string();
                /*
                PdfObj da = obj.pdf_dict_get_inheritable(new PdfObj("DA"));
                if (da.m_internal != null)
                {
                    PdfObj tailer = pdf.pdf_trailer();
                    da = Utils.pdf_dict_getl(tailer, new string[] { "Root", "AcroForm", "DA" });
                    da_string = da.pdf_to_text_string();
                }
                */
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error: {e.Message}");  // Example logging
                da_string = "";
            }

            if (string.IsNullOrEmpty(da_string))
            {
                return (new List<float>(), "", 0.0f);
            }

            string font = "Helv";
            float fsize = 12.0f;
            List<float> col = new List<float>() { 0.0f, 0.0f, 0.0f };
            string[] dat = da_string.Split();

            for (int i = 0; i < dat.Length; i++)
            {
                string item = dat[i];
                if (item == "Tf")
                {
                    font = dat[i - 2].Substring(1);
                    fsize = float.Parse(dat[i - 1]);
                    dat[i] = dat[i - 1] = dat[i - 2] = "";
                    continue;
                }

                if (item == "g")
                {
                    col = new List<float>() { float.Parse(dat[i - 1]) };
                    dat[i] = dat[i - 1] = "";
                    continue;
                }

                if (item == "rg")
                {
                    col = new List<float>();
                    for (int j = i - 3; j < i; j++)
                    {
                        col.Add(float.Parse(dat[j]));
                    }
                    dat[i] = dat[i - 1] = dat[i - 2] = dat[i - 3] = "";
                    continue;
                }

                if (item == "k")
                {
                    col = new List<float>();
                    for (int j = i - 4; j < i; j++)
                    {
                        col.Add(float.Parse(dat[j]));
                    }

                    dat[i] = dat[i - 1] = dat[i - 2] = dat[i - 3] = dat[i - 4] = "";
                    continue;
                }
            }

            return (col, font, fsize);
        }

        public static void UpdateData(PdfAnnot annot_, string dataStr)
        {
            try
            {
                PdfAnnot annot = annot_;
                annot.pdf_annot_obj().pdf_dict_put_text_string(new PdfObj("DA"), dataStr);
                annot.pdf_annot_obj().pdf_dict_del(new PdfObj("DS"));
                annot.pdf_annot_obj().pdf_dict_del(new PdfObj("RC"));
            }
            catch
            {
                return;
            }
        }

        public byte[] GetAP()
        {
            PdfObj obj = _nativeAnnotion.pdf_annot_obj();
            PdfObj ap = Utils.pdf_dict_getl(obj, new string[] { "AP", "N" });

            FzBuffer ret = null;
            if (ap.pdf_is_stream() != 0)
            {
                ret = ap.pdf_load_stream();
            }

            byte[] r = null;
            if (ret != null && ret.m_internal != null)
                r = Utils.BinFromBuffer(ret);

            return r;
        }

        public void SetAP(byte[] buffer, int rect = 0)
        {
            try
            {
                PdfObj annotObj = _nativeAnnotion.pdf_annot_obj();
                PdfPage page = _nativeAnnotion.pdf_annot_page();
                PdfObj apObj = Utils.pdf_dict_getl(annotObj, new string[] { "AP", "N" });

                if (apObj.m_internal == null)
                {
                    throw new Exception("bad or missing annot AP/N");
                }
                if (apObj.pdf_is_stream() == 0)
                {
                    throw new Exception("bad or missing annot AP/N");
                }
                FzBuffer buf = Utils.BufferFromBytes(buffer);
                if (buf.m_internal == null)
                {
                    throw new Exception("bad type: 'buffer'");
                }
                Utils.UpdateStream(page.doc(), apObj, buf, 1);
                if (rect != 0)
                {
                    FzRect bbox = annotObj.pdf_dict_get_rect(new PdfObj("Rect"));
                    apObj.pdf_dict_put_rect(new PdfObj("Rect"), bbox);
                }
            }
            catch (Exception) { }
        }

        public static float[] ColorFromSequence(float[] seq)
        {
            if (!(seq is float[]) || seq == null)
                return null;

            if (!(new List<int>() { 0, 1, 3, 4 }).Contains(seq.Length))
                return null;

            for (int i = 0; i < seq.Length; i++)
            {
                if (seq[i] < 0 || seq[i] > 1)
                    seq[i] = 1.0f;
            }

            return seq;
        }

        private bool UpdateAppearance(
            float opacity = -1.0f,
            string blendMode = null,
            float[] fillColor = null,
            float rotate = -1.0f
        )
        {
            PdfObj annotObj = _nativeAnnotion.pdf_annot_obj();
            PdfPage page = _nativeAnnotion.pdf_annot_page();
            PdfDocument doc = page.doc();
            pdf_annot_type type = _nativeAnnotion.pdf_annot_type();
            float[] cols = ColorFromSequence(fillColor);
            int nCols = cols.Length;

            IntPtr colsPtr = Marshal.AllocHGlobal(nCols * sizeof(float));
            Marshal.Copy(cols, 0, colsPtr, nCols);
            SWIGTYPE_p_float swigCols = new SWIGTYPE_p_float(colsPtr, false);

            try
            {
                if (
                    nCols == 0
                    || !(
                        new List<PdfAnnotType>()
                        {
                            PdfAnnotType.PDF_ANNOT_SQUARE,
                            PdfAnnotType.PDF_ANNOT_CIRCLE,
                            PdfAnnotType.PDF_ANNOT_LINE,
                            PdfAnnotType.PDF_ANNOT_POLY_LINE,
                            PdfAnnotType.PDF_ANNOT_POLYGON
                        }
                    ).Contains((PdfAnnotType)type)
                )
                {
                    annotObj.pdf_dict_del(new PdfObj("IC"));
                }
                else if (nCols > 0)
                {
                    _nativeAnnotion.pdf_set_annot_interior_color(nCols, swigCols);
                }

                int insertRot = 0;
                if (rotate >= 0)
                    insertRot = 1;

                if (
                    !(
                        new List<PdfAnnotType>()
                        {
                            PdfAnnotType.PDF_ANNOT_CARET,
                            PdfAnnotType.PDF_ANNOT_CIRCLE,
                            PdfAnnotType.PDF_ANNOT_FREE_TEXT,
                            PdfAnnotType.PDF_ANNOT_FILE_ATTACHMENT,
                            PdfAnnotType.PDF_ANNOT_INK,
                            PdfAnnotType.PDF_ANNOT_LINE,
                            PdfAnnotType.PDF_ANNOT_POLY_LINE,
                            PdfAnnotType.PDF_ANNOT_POLYGON,
                            PdfAnnotType.PDF_ANNOT_SQUARE,
                            PdfAnnotType.PDF_ANNOT_STAMP,
                            PdfAnnotType.PDF_ANNOT_TEXT,
                        }
                    ).Contains((PdfAnnotType)type)
                )
                {
                    insertRot = 0;
                }

                if (insertRot != 0)
                {
                    annotObj.pdf_dict_put_int(new PdfObj("Rotate"), (long)rotate);
                }

                // insert fill color
                if ((PdfAnnotType)type == PdfAnnotType.PDF_ANNOT_FREE_TEXT)
                {
                    if (nCols > 0)
                    {
                        _nativeAnnotion.pdf_set_annot_color(nCols, swigCols);
                    }
                }
                else if (nCols > 0)
                {
                    PdfObj col = doc.pdf_new_array(nCols);
                    for (int i = 0; i < nCols; i++)
                    {
                        mupdf.mupdf.pdf_array_push_real(col, cols[i]);
                    }
                    annotObj.pdf_dict_put(new PdfObj("IC"), col);
                }
                _nativeAnnotion.pdf_dirty_annot();
                _nativeAnnotion.pdf_update_annot();
                doc.m_internal.resynth_required = 0;
            }
            catch (Exception e) 
            {
                throw new Exception("cannot update annot:" + e.Message);
            }

            if ((opacity < 0 || opacity >= 1) && blendMode == null) // no opacity, no blend_mode
            {
                return true;
            }

            try // create or update /ExtGState
            {
                PdfObj ap = Utils.pdf_dict_getl(
                    _nativeAnnotion.pdf_annot_obj(),
                    new string[] { "AP", "N" }
                );
                if (ap.m_internal == null)
                {
                    // should never happen
                    throw new Exception("bad or missing annot AP/N");
                }

                PdfObj resources = ap.pdf_dict_get(new PdfObj("Resources"));
                if (resources.m_internal == null)
                    resources = ap.pdf_dict_put_dict(new PdfObj("Resources"), 2);

                PdfObj alp0 = doc.pdf_new_dict(3);
                if (opacity >= 0 && opacity < 1)
                {
                    alp0.pdf_dict_put_real(new PdfObj("CA"), opacity);
                    alp0.pdf_dict_put_real(new PdfObj("ca"), opacity);
                    annotObj.pdf_dict_put_real(new PdfObj("CA"), opacity);
                }

                if (!string.IsNullOrEmpty(blendMode))
                {
                    alp0.pdf_dict_put_name(new PdfObj("BM"), blendMode);
                    annotObj.pdf_dict_put_name(new PdfObj("BM"), blendMode);
                }

                PdfObj extg = resources.pdf_dict_get(new PdfObj("ExtGState"));
                if (extg.m_internal == null) // no ExtGState yet: make one
                    extg = resources.pdf_dict_put_dict(new PdfObj("ExtGState"), 2);

                extg.pdf_dict_put(new PdfObj("H"), alp0);
            }
            catch (Exception)
            {
                Console.WriteLine("cannot set opacity or blend mode");
            }

            return true;
        }

        public void CleanContents(int sanitize = 1)
        {
            PdfAnnot annot = _nativeAnnotion;
            PdfDocument pdf = annot.pdf_annot_obj().pdf_get_bound_document();
            PdfFilterOptions filter = Utils.MakePdfFilterOptions(
                recurse: 1,
                instanceForms: 0,
                ascii: 0,
                sanitize: sanitize
            );
            pdf.pdf_filter_annot_contents(annot, filter);
        }

        public PdfAnnot ToPdfAnnot()
        {
            return _nativeAnnotion;
        }

        public static PdfAnnot FindAnnotIRT(PdfAnnot annot)
        {
            PdfAnnot irtAnnot = null;
            PdfObj annotObj = annot.pdf_annot_obj();
            int found = 0;
            PdfPage page = annot.pdf_annot_page();
            irtAnnot = page.pdf_first_annot();

            while (true)
            {
                if (irtAnnot.m_internal == null)
                {
                    break;
                }

                PdfObj irtAnnotObj = irtAnnot.pdf_annot_obj();
                PdfObj irt = irtAnnotObj.pdf_dict_gets("IRT");
                if (irt.m_internal != null)
                {
                    if (irt.pdf_objcmp(annotObj) != 0)
                    {
                        found = 1;
                        break;
                    }
                }
                irtAnnot = irtAnnot.pdf_next_annot();
            }

            if (found != 0)
                return irtAnnot;

            return null;
        }

        public void DeleteResponses()
        {
            PdfAnnot annot = _nativeAnnotion;
            PdfObj annotObj = annot.pdf_annot_obj();
            PdfPage page = annot.pdf_annot_page();

            while (true)
            {
                PdfAnnot irtAnnot = FindAnnotIRT(annot);
                if (irtAnnot == null)
                    break;
                page.pdf_delete_annot(irtAnnot);
            }

            annotObj.pdf_dict_del(new PdfObj("Popup"));
            PdfObj annots = page.obj().pdf_dict_get(new PdfObj("Annots"));
            int n = annots.pdf_array_len();
            int found = 0;

            for (int i = n - 1; i >= 0; i--)
            {
                PdfObj o = annots.pdf_array_get(i);
                PdfObj p = o.pdf_dict_get(new PdfObj("Parent"));
                if (o.m_internal == null)
                {
                    continue;
                }
                if (p.pdf_objcmp(annotObj) == 0)
                {
                    annots.pdf_array_delete(i);
                    found = 1;
                }
            }

            if (found != 0)
                page.obj().pdf_dict_put(new PdfObj("Annots"), annots);
        }

        public FzBuffer GetFile()
        {
            PdfAnnot annot = _nativeAnnotion;
            PdfObj annotObj = annot.pdf_annot_obj();
            PdfAnnotType type = (PdfAnnotType)annot.pdf_annot_type();

            if (type != PdfAnnotType.PDF_ANNOT_FILE_ATTACHMENT)
            {
                throw new Exception("bad annot type");
            }

            PdfObj stream = Utils.pdf_dict_getl(annotObj, new string[] { "FS", "EF", "F" });
            if (stream.m_internal == null)
            {
                throw new Exception("bad PDF: file entry not found");
            }

            FzBuffer buf = stream.pdf_load_stream();

            return buf;
        }

        public int GetOC()
        {
            int oc = 0;
            PdfAnnot annot = _nativeAnnotion;
            PdfObj annotObj = annot.pdf_annot_obj();
            PdfObj obj = annotObj.pdf_dict_get(new PdfObj("OC"));
            if (obj.m_internal != null)
                oc = obj.pdf_to_num();

            return oc;
        }

        public Pixmap GetPixmap(
            Matrix matrix = null,
            int dpi = 0,
            ColorSpace colorSpace = null,
            int alpha = 0
        )
        {
            List<ColorSpace> colorSpaces = new List<ColorSpace>()
            {
                new ColorSpace(Utils.CS_GRAY),
                new ColorSpace(Utils.CS_RGB),
                new ColorSpace(Utils.CS_CMYK),
            };

            if (dpi != 0)
                matrix = new Matrix(dpi / 72, dpi / 72);

            FzMatrix ctm = matrix == null ? new FzMatrix() : matrix.ToFzMatrix();
            FzColorspace cs = null;
            if (colorSpace == null)
                cs = mupdf.mupdf.fz_device_rgb();
            else
                cs = colorSpace.ToFzColorspace();

            FzPixmap pix = mupdf.mupdf.pdf_new_pixmap_from_annot(
                _nativeAnnotion,
                ctm,
                cs,
                new FzSeparations(0),
                alpha
            );

            Pixmap ret = new Pixmap(pix);
            if (dpi != 0)
                ret.SetDpi(dpi, dpi);

            return ret;
        }

        public Sound GetSound()
        {
            PdfAnnot annot = _nativeAnnotion;
            PdfObj annotObj = annot.pdf_annot_obj();
            PdfAnnotType type = (PdfAnnotType)annot.pdf_annot_type();
            PdfObj sound = annotObj.pdf_dict_get(new PdfObj("Sound"));

            if (type != PdfAnnotType.PDF_ANNOT_SOUND || sound.m_internal == null)
            {
                throw new Exception(Utils.ErrorMessages["MSG_BAD_ANNOT_TYPE"]);
            }

            if (sound.pdf_dict_get(new PdfObj("F")).m_internal != null)
            {
                throw new Exception("Unsupported Sound Stream");
            }

            Sound ret = new Sound();
            PdfObj obj = sound.pdf_dict_get(new PdfObj("R"));
            if (obj.m_internal != null)
                ret.Rate = obj.pdf_to_real();
            obj = sound.pdf_dict_get(new PdfObj("C"));
            if (obj.m_internal != null)
                ret.Channels = obj.pdf_to_int();
            obj = sound.pdf_dict_get(new PdfObj("B"));
            if (obj.m_internal != null)
                ret.Bps = obj.pdf_to_int();
            obj = sound.pdf_dict_get(new PdfObj("E"));
            if (obj.m_internal != null)
                ret.Encoding = obj.pdf_to_name();
            obj = sound.pdf_dict_gets("CO");
            if (obj.m_internal != null)
                ret.Compression = obj.pdf_to_name();

            FzBuffer buf = sound.pdf_load_stream();
            byte[] stream = Utils.BinFromBuffer(buf);
            ret.Stream = stream;

            return ret;
        }

        public TextPage GetTextPage(Rect clip = null, int flags = 0)
        {
            FzStextOptions options = new FzStextOptions();
            PdfAnnot annot = _nativeAnnotion;
            options.flags = flags;
            FzStextPage stPage = new FzStextPage(annot, options);

            return new TextPage(stPage);
        }

        public int IrtXref
        {
            get
            {
                PdfAnnot annot = _nativeAnnotion;
                PdfObj annotObj = annot.pdf_annot_obj();
                PdfObj irt = annotObj.pdf_dict_get(new PdfObj("IRT"));
                if (irt == null)
                    return 0;
                return irt.pdf_to_num();
            }
        }

        private void SetApnBbox(Rect bbox)
        {
            Page page = Parent;
            Matrix rot = page.DerotationMatrix;
            Matrix mat = page.TransformationMatrix;
            bbox = bbox * (rot * ~mat);

            PdfAnnot annot = _nativeAnnotion;
            PdfObj annotObj = annot.pdf_annot_obj();
            PdfObj ap = Utils.pdf_dict_getl(annotObj, new string[] { "AP", "N" });

            if (ap.m_internal == null)
                throw new Exception(Utils.ErrorMessages["MSG_BAD_APN"]);

            ap.pdf_dict_put_rect(new PdfObj("BBox"), bbox.ToFzRect());
        }

        private void SetApnMatrix(Matrix matrix)
        {
            PdfAnnot annot = _nativeAnnotion;
            PdfObj annotObj = annot.pdf_annot_obj();
            PdfObj ap = Utils.pdf_dict_getl(annotObj, new string[] { "AP", "N" });

            if (ap.m_internal == null)
                throw new Exception(Utils.ErrorMessages["MSG_BAD_APN"]);

            ap.pdf_dict_put_matrix(new PdfObj("Matrix"), matrix.ToFzMatrix());
        }

        public void SetBlendMode(string blendMode)
        {
            PdfAnnot annot = _nativeAnnotion;
            PdfObj annotObj = annot.pdf_annot_obj();
            annotObj.pdf_dict_put_name(new PdfObj("BM"), blendMode);
        }

        public void SetBorder(
            Border border = null,
            float width = -1,
            string style = null,
            int[] dashes = null,
            int clouds = -1
        )
        {
            (PdfAnnotType atype, string atname, string _) = this.Type;

            if (
                !(
                    new List<PdfAnnotType>()
                    {
                        PdfAnnotType.PDF_ANNOT_CIRCLE,
                        PdfAnnotType.PDF_ANNOT_FREE_TEXT,
                        PdfAnnotType.PDF_ANNOT_INK,
                        PdfAnnotType.PDF_ANNOT_LINE,
                        PdfAnnotType.PDF_ANNOT_POLY_LINE,
                        PdfAnnotType.PDF_ANNOT_POLYGON,
                        PdfAnnotType.PDF_ANNOT_SQUARE
                    }
                ).Contains(atype)
            )
            {
                Console.WriteLine(string.Format("Cannot set cloudy border for '{0}'.", atname));
                return;
            }

            if (
                !(
                    new List<PdfAnnotType>()
                    {
                        PdfAnnotType.PDF_ANNOT_CIRCLE,
                        PdfAnnotType.PDF_ANNOT_FREE_TEXT,
                        PdfAnnotType.PDF_ANNOT_POLYGON,
                        PdfAnnotType.PDF_ANNOT_SQUARE
                    }
                ).Contains(atype)
            )
            {
                if (clouds > 0)
                {
                    Console.WriteLine(string.Format("Cannot set cloudy border for '{0}'.", atname));
                    clouds = -1;
                }
            }

            Border border_ = new Border();
            if (border == null)
            {
                border_.Width = width;
                border_.Style = style;
                border_.Dashes = dashes;
                border_.Clouds = clouds;
            }

            PdfAnnot annot = _nativeAnnotion;
            PdfObj annotObj = annot.pdf_annot_obj();
            PdfDocument pdf = annotObj.pdf_get_bound_document();
            SetBorderAnnot(border_, pdf, annotObj);
        }

        public static Border GetBorderFromAnnot(PdfObj annotObj)
        {
            List<int> dashes = new List<int>();
            string style = "";
            float width = -1.0f;
            int clouds = -1;
            PdfObj obj = annotObj.pdf_dict_get(new PdfObj("Border"));

            if (obj.pdf_is_array() != 0)
            {
                width = obj.pdf_array_get(2).pdf_to_real();
                if (obj.pdf_array_len() == 4)
                {
                    PdfObj dash = obj.pdf_array_get(3);
                    for (int i = 0; i < dash.pdf_array_len(); i++)
                    {
                        int v = dash.pdf_array_get(i).pdf_to_int();
                        dashes.Add(v);
                    }
                }
            }

            PdfObj bsObj = annotObj.pdf_dict_get(new PdfObj("BS"));
            if (bsObj.m_internal != null)
            {
                width = bsObj.pdf_dict_get(new PdfObj("W")).pdf_to_real();
                style = bsObj.pdf_dict_get(new PdfObj("S")).pdf_to_name();

                if (style == "")
                    style = null;
                obj = bsObj.pdf_dict_get(new PdfObj("D"));
                if (obj != null)
                    for (int i = 0; i < obj.pdf_array_len(); i++)
                    {
                        int v = obj.pdf_array_get(i).pdf_to_int();
                        dashes.Add(v);
                    }
            }

            obj = annotObj.pdf_dict_get(new PdfObj("BE"));
            if (obj.m_internal != null)
                clouds = obj.pdf_dict_get(new PdfObj("I")).pdf_to_int();

            Border ret = new Border();
            ret.Width = width;
            ret.Dashes = dashes.ToArray();
            ret.Style = style;
            ret.Clouds = clouds;

            return ret;
        }

        public static Color GetColorFromAnnot(PdfObj annotObj)
        {
            Color ret = new Color();
            List<float> bc = new List<float>();
            List<float> fc = new List<float>();

            PdfObj obj = annotObj.pdf_dict_get(new PdfObj("C"));
            if (obj.pdf_is_array() != 0)
            {
                int n = obj.pdf_array_len();
                for (int i = 0; i < n; i++)
                {
                    float col = obj.pdf_array_get(i).pdf_to_real();
                    bc.Add(col);
                }
            }
            ret.Stroke = bc.ToArray();

            obj = annotObj.pdf_dict_gets("IC");
            if (obj.pdf_is_array() != 0)
            {
                int n = obj.pdf_array_len();
                for (int i = 0; i < n; i++)
                {
                    float col = obj.pdf_array_get(i).pdf_to_real();
                    fc.Add(col);
                }
            }
            ret.Fill = fc.ToArray();

            return ret;
        }

        public static PdfObj GetBorderStyle(string s)
        {
            PdfObj val = new PdfObj("S");
            if (string.IsNullOrEmpty(s))
                return val;

            if (s.StartsWith("b") || s.StartsWith("B"))
                val = new PdfObj("B");
            else if (s.StartsWith("d") || s.StartsWith("D"))
                val = new PdfObj("D");
            else if (s.StartsWith("i") || s.StartsWith("I"))
                val = new PdfObj("I");
            else if (s.StartsWith("u") || s.StartsWith("U"))
                val = new PdfObj("U");
            else if (s.StartsWith("s") || s.StartsWith("S"))
                val = new PdfObj("S");

            return val;
        }

        public static void SetBorderAnnot(Border border, PdfDocument doc, PdfObj annotObj)
        {
            int dashLen = 0;
            float nWidth = border.Width;
            int[] nDashes = border.Dashes;
            string nStyle = border.Style;
            float nClouds = border.Clouds;

            Border oldBorder = GetBorderFromAnnot(annotObj);

            annotObj.pdf_dict_del(new PdfObj("BS"));
            annotObj.pdf_dict_del(new PdfObj("BE"));
            annotObj.pdf_dict_del(new PdfObj("Border"));

            if (nWidth < 0)
                nWidth = oldBorder.Width;
            if (nDashes == null)
                nDashes = oldBorder.Dashes;
            if (nStyle == null)
                nStyle = oldBorder.Style;
            if (nClouds < 0)
                nClouds = oldBorder.Clouds;

            if (nDashes != null && nDashes.Length > 0)
            {
                dashLen = nDashes.Length;
                PdfObj darr = doc.pdf_new_array(dashLen);
                foreach (int b in nDashes)
                {
                    darr.pdf_array_push_int(b);
                }
                Utils.pdf_dict_putl(annotObj, darr, new string[] { "BS", "D" });
            }

            Utils.pdf_dict_putl(
                annotObj,
                mupdf.mupdf.pdf_new_real(nWidth),
                new string[] { "BS", "W" }
            );

            PdfObj obj = null;
            if (dashLen == 0)
                obj = GetBorderStyle(nStyle);
            else
                obj = new PdfObj("D");

            Utils.pdf_dict_putl(annotObj, obj, new string[] { "BS", "S" });

            if (nClouds > 0)
            {
                annotObj.pdf_dict_put_dict(new PdfObj("BE"), 2);
                obj = annotObj.pdf_dict_get(new PdfObj("BE"));
                obj.pdf_dict_put(new PdfObj("S"), new PdfObj("C"));
                obj.pdf_dict_put_int(new PdfObj("I"), (long)nClouds);
            }
        }

        public void SetColors(Color colors = null, float[] stroke = null, float[] fill = null)
        {
            pdf_annot_type type = _nativeAnnotion.pdf_annot_type();
            if (type == pdf_annot_type.PDF_ANNOT_FREE_TEXT)
            {
                throw new Exception("cannot be used for FreeText annotations");
            }
            Document doc = Parent.Parent;

            Color colors_ = new Color();
            if (colors == null)
            {
                colors_.Fill = fill;
                colors_.Stroke = stroke;
            }
            
            fill = colors_.Fill;
            stroke = colors_.Stroke;

            List<PdfAnnotType> fillAnnots = new List<PdfAnnotType>()
            {
                PdfAnnotType.PDF_ANNOT_CIRCLE,
                PdfAnnotType.PDF_ANNOT_SQUARE,
                PdfAnnotType.PDF_ANNOT_LINE,
                PdfAnnotType.PDF_ANNOT_POLY_LINE,
                PdfAnnotType.PDF_ANNOT_POLYGON,
                PdfAnnotType.PDF_ANNOT_REDACT
            };

            if (stroke != null && stroke.Length == 0)
            {
                doc.SetKeyXRef(this.Xref, "C", "[]");
            }
            else if (stroke != null)
            {
                string s = "";
                if (stroke.Length == 1)
                    s = string.Format("[{0}]", stroke[0]);
                if (stroke.Length == 3)
                    s = string.Format("[{0} {1} {2}]", stroke[0], stroke[1], stroke[2]);
                else
                    s = string.Format(
                        "[{0} {1} {2} {3}]",
                        stroke[0],
                        stroke[1],
                        stroke[2],
                        stroke[3]
                    );
                doc.SetKeyXRef(this.Xref, "C", s);
            }

            if (fill != null && !fillAnnots.Contains(this.Type.Item1))
            {
                Console.WriteLine(
                    string.Format(
                        "Warning: fill color ignored for annot type '{0}'",
                        this.Type.Item1
                    )
                );
                return;
            }

            if (fill != null && fill.Length == 0)
                doc.SetKeyXRef(this.Xref, "IC", "[]");
            else if (fill != null)
            {
                string s = "";
                if (fill.Length == 1)
                {
                    s = string.Format("[{0}]", fill[0]);
                }
                else if (fill.Length == 3)
                {
                    s = string.Format("[{0} {1} {2}]", fill[0], fill[1], fill[2]);
                }
                else
                {
                    s = string.Format("[{0} {1} {2} {3}]", fill[0], fill[1], fill[2], fill[3]);
                }
                doc.SetKeyXRef(this.Xref, "IC", s);
            }
        }

        public void SetFlags(int flags)
        {
            PdfAnnot annot = _nativeAnnotion;
            mupdf.mupdf.pdf_set_annot_flags(annot, flags);
        }

        /// <summary>
        /// Set various properties.
        /// </summary>
        /// <param name="info">a dictionary compatible with the info property</param>
        /// <param name="content">a string containing the text for type Text and FreeText annotations. Commonly used for filling the text field of annotation pop-up windows.</param>
        /// <param name="title">a string containing the title of the annotation pop-up window. By convention, this is used for the annotation author.</param>
        /// <param name="creationDate">creation timestamp.</param>
        /// <param name="modDate">last modified timestamp.</param>
        /// <param name="subject">subject.</param>
        public void SetInfo(
            AnnotInfo info = null,
            string content = null,
            string title = null,
            string creationDate = null,
            string modDate = null,
            string subject = null
        )
        {
            if (info != null)
            {
                content = info.Content;
                title = info.Title;
                creationDate = info.CreationDate;
                modDate = info.ModDate;
                subject = info.Subject;
                info = null;
            }

            PdfAnnot annot = _nativeAnnotion;
            bool isMarkup = Convert.ToBoolean(annot.pdf_annot_has_author());

            if (!string.IsNullOrEmpty(content))
                annot.pdf_set_annot_contents(content);
            if (isMarkup)
            {
                if (!string.IsNullOrEmpty(title))
                    annot.pdf_set_annot_author(title);
                if (!string.IsNullOrEmpty(creationDate))
                    annot
                        .pdf_annot_obj()
                        .pdf_dict_put_text_string(new PdfObj("CreationDate"), creationDate);

                if (!string.IsNullOrEmpty(modDate))
                    annot.pdf_annot_obj().pdf_dict_put_text_string(new PdfObj("M"), modDate);

                if (!string.IsNullOrEmpty(subject))
                    annot
                        .pdf_annot_obj()
                        .pdf_dict_puts("Subj", mupdf.mupdf.pdf_new_text_string(subject));
            }
        }

        public void SetIrtXref(int xref)
        {
            PdfAnnot annot = _nativeAnnotion;
            PdfObj annotObj = annot.pdf_annot_obj();
            PdfPage page = annot.pdf_annot_page();

            if (xref < 1 || xref >= page.doc().pdf_xref_len())
                throw new Exception(Utils.ErrorMessages["MSG_BAD_XREF"]);

            PdfObj irt = page.doc().pdf_new_indirect(xref, 0);
            PdfObj subt = irt.pdf_dict_get(new PdfObj("Subtype"));
            PdfAnnotType irtSubt = (PdfAnnotType)
                mupdf.mupdf.pdf_annot_type_from_string(subt.pdf_to_name());
            if ((int)irtSubt < 0)
                throw new Exception(Utils.ErrorMessages["MSG_IS_NO_ANNOT"]);

            annotObj.pdf_dict_put(new PdfObj("IRT"), irt);
        }

        public void SetLanguage(string language)
        {
            PdfAnnot annot = _nativeAnnotion;
            TextLanguage lang;
            if (language == null)
                lang = TextLanguage.FZ_LANG_UNSET;
            else
                lang = (TextLanguage)mupdf.mupdf.fz_text_language_from_string(language);

            annot.pdf_set_annot_language((fz_text_language)lang);
        }

        public void SetLineEnds(PdfLineEnding start, PdfLineEnding end)
        {
            PdfAnnot annot = _nativeAnnotion;
            if (annot.pdf_annot_has_line_ending_styles() != 0)
            {
                annot.pdf_set_annot_line_ending_styles(
                    (pdf_line_ending)start,
                    (pdf_line_ending)end
                );
            }
            else
                Console.WriteLine("bad annot type for line ends");
        }

        public void SetName(string name)
        {
            PdfAnnot annot = _nativeAnnotion;
            PdfObj annotObj = annot.pdf_annot_obj();
            annotObj.pdf_dict_put_name(new PdfObj("Name"), name);
        }

        public void SetOC(int oc = 0)
        {
            PdfAnnot annot = _nativeAnnotion;
            PdfObj annotObj = annot.pdf_annot_obj();
            if (oc == 0)
                annotObj.pdf_dict_del(new PdfObj("OC"));
            else
                AddOCObject(annotObj.pdf_get_bound_document(), annotObj, oc);
        }

        public static void AddOCObject(PdfDocument doc, PdfObj _ref, int xref)
        {
            PdfObj indObj = doc.pdf_new_indirect(xref, 0);
            if (indObj.pdf_is_dict() == 0)
                throw new Exception(Utils.ErrorMessages["MSG_BAD_OC_REF"]);

            PdfObj type = indObj.pdf_dict_get(new PdfObj("Type"));
            if (type.pdf_objcmp(new PdfObj("OCG")) == 0 || type.pdf_objcmp(new PdfObj("OCMD")) == 0)
            {
                _ref.pdf_dict_put(new PdfObj("OC"), indObj);
            }
            else
                throw new Exception(Utils.ErrorMessages["MSG_BAD_OC_REF"]);
        }

        public void SetOpacity(float opacity)
        {
            PdfAnnot annot = _nativeAnnotion;
            if (!Utils.INRANGE(opacity, 0.0f, 1.0f))
            {
                annot.pdf_set_annot_opacity(1);
                return;
            }

            annot.pdf_set_annot_opacity(opacity);
            if (opacity < 1.0f)
            {
                PdfPage page = annot.pdf_annot_page();
                page.m_internal.transparency = 1;
            }
        }

        public void SetOpen(int isOpen)
        {
            PdfAnnot annot = _nativeAnnotion;
            annot.pdf_set_annot_is_open(isOpen);
        }

        public void SetPopup(Rect rect)
        {
            PdfAnnot annot = _nativeAnnotion;
            PdfPage page = annot.pdf_annot_page();
            Matrix rot = Utils.RotatePageMatrix(page);

            FzRect r = rect.ToFzRect().fz_transform_rect(rot.ToFzMatrix());
            annot.pdf_set_annot_popup(r);
        }

        public void SetRect(Rect rect)
        {
            PdfAnnot annot = _nativeAnnotion;
            PdfPage pdfPage = annot.pdf_annot_page();
            Matrix rot = Utils.RotatePageMatrix(pdfPage);
            FzRect r = rect.ToFzRect().fz_transform_rect(rot.ToFzMatrix());

            if (r.fz_is_empty_rect() != 0 || r.fz_is_infinite_rect() != 0)
            {
                throw new Exception(Utils.ErrorMessages["MSG_BAD_RECT"]);
            }

            try
            {
                annot.pdf_set_annot_rect(r);
            }
            catch (Exception e)
            {
                //Console.WriteLine(string.Format("cannot set rect: {0}", e));
            }
            finally
            {
                r.Dispose();
            }
        }

        public Page GetParent()
        {
            Page ret = null;
            if (Parent != null)
                ret = Parent;
            else
            {
                PdfPage page = _nativeAnnotion.pdf_annot_page();
                Document doc = (page.m_internal == null) ? null : new Document(page.doc());
                ret = new Page(page, doc);

                Parent = ret;
            }

            return ret;
        }

        public void SetRotation(int rotate = 0)
        {
            PdfAnnot annot = _nativeAnnotion;
            PdfAnnotType type = (PdfAnnotType)annot.pdf_annot_type();
            if (
                !(
                    new List<PdfAnnotType>()
                    {
                        PdfAnnotType.PDF_ANNOT_CARET,
                        PdfAnnotType.PDF_ANNOT_CIRCLE,
                        PdfAnnotType.PDF_ANNOT_FREE_TEXT,
                        PdfAnnotType.PDF_ANNOT_FILE_ATTACHMENT,
                        PdfAnnotType.PDF_ANNOT_INK,
                        PdfAnnotType.PDF_ANNOT_LINE,
                        PdfAnnotType.PDF_ANNOT_POLY_LINE,
                        PdfAnnotType.PDF_ANNOT_POLYGON,
                        PdfAnnotType.PDF_ANNOT_SQUARE,
                        PdfAnnotType.PDF_ANNOT_STAMP,
                        PdfAnnotType.PDF_ANNOT_TEXT
                    }
                ).Contains((PdfAnnotType)type)
            )
            {
                return;
            }

            int rot = rotate;
            while (rot < 0)
                rot += 360;
            while (rot >= 360)
                rot -= 360;

            if (type == PdfAnnotType.PDF_ANNOT_FREE_TEXT && rot % 90 != 0)
                rot = 0;

            PdfObj annotObj = annot.pdf_annot_obj();
            annotObj.pdf_dict_put_int(new PdfObj("Rotate"), rot);
        }

        /// <summary>
        /// Update annot appearance.
        /// Depending on the annot type, some parameters make no sense,
        /// while others are only available in this method to achieve the
        /// desired result.This is especially true for 'FreeText' annots.
        /// </summary>
        /// <param name="blendMode">set the blend mode, all annotations.</param>
        /// <param name="opacity">set the opacity, all annotations.</param>
        /// <param name="fontSize">set fontsize, 'FreeText' only.</param>
        /// <param name="fontName">set the font, 'FreeText' only.</param>
        /// <param name="textColor">set text color, 'FreeText' only.</param>
        /// <param name="borderColor">set border color, 'FreeText' only.</param>
        /// <param name="fillColor">set fill color, all annotations.</param>
        /// <param name="crossOut">draw diagonal lines, 'Redact' only.</param>
        /// <param name="rotate">set rotation, 'FreeText' and some others.</param>
        /// <return></return>
        public void Update(
            string blendMode = null,
            float opacity = float.MaxValue,
            float fontSize = 0.0f,
            string fontName = null,
            float[] textColor = null,
            float[] borderColor = null,
            float[] fillColor = null,
            bool crossOut = true,
            int rotate = -1
        )
        {
            PdfAnnot annot = _nativeAnnotion;
            PdfObj annotObj = annot.pdf_annot_obj();

            if (borderColor != null)
            {
                PdfObj isRichText = annotObj.pdf_dict_get(new PdfObj("RC"));
                if (isRichText != null)
                    throw new Exception("cannot set border_color if rich_text is False");
            }
            
            PdfAnnotType annotType = Type.Item1;

            int[] dt = Border.Dashes;
            float borderWidth = Border.Width;
            float[] stroke = Colors.Stroke;
            List<float> fill = null;
            if (fillColor != null)
                fill = new List<float>(fillColor);
            else
                fill = new List<float>(Colors.Fill);

            Rect rect = null;
            Matrix apnMat = ApnMatrix;
            if (rotate != -1)
            {
                while (rotate < 0)
                    rotate += 360;
                while (rotate >= 360)
                    rotate -= 360;
                if (annotType == PdfAnnotType.PDF_ANNOT_FREE_TEXT && rotate % 90 != 0)
                    rotate = 0;
            }

            // handle opacity and blend mode
            if (string.IsNullOrEmpty(blendMode))
                blendMode = BlendMode;
            if (opacity == float.MaxValue)
                opacity = Opacity;

            string opaCode = "";
            if ((opacity >= 0 && opacity < 1) || !string.IsNullOrEmpty(blendMode))
                opaCode = "/H gs\n";
            else
                opaCode = "";

            if (annotType == PdfAnnotType.PDF_ANNOT_FREE_TEXT)
            {
                Utils.CheckColor(textColor);
                Utils.CheckColor(fillColor);
                (List<float> tcol, string fname, float fsize) = ParseData(this);

                bool updateDefaultAppearance = false;
                if (fsize <= 0)
                {
                    fsize = 12;
                    updateDefaultAppearance = true;
                }

                if (textColor != null)
                {
                    tcol = new List<float>(textColor);
                    updateDefaultAppearance = true;
                }

                if (fontName != null)
                {
                    fname = fontName;
                    updateDefaultAppearance = true;
                }

                if (fontSize > 0)
                {
                    fsize = fontSize;
                    updateDefaultAppearance = true;
                }

                if (updateDefaultAppearance)
                {
                    Utils.MakeAnnotDA(annot, tcol == null ? -1 : tcol.Count, tcol.ToArray(), fname, fsize);
                    blendMode = null; // not supported for free text annotations!
                }
            }

            // now invoke MuPDF to update the annot appearance
            bool res = UpdateAppearance(
                opacity: opacity,
                blendMode: blendMode,
                fillColor: fill.ToArray(),
                rotate: rotate
            );
            if (!res)
                throw new Exception("Error updating annotation");

            if (annotType == PdfAnnotType.PDF_ANNOT_FREE_TEXT)
            {
                // in absence of previous opacity, we may need to modify the AP
                string apStr = Encoding.UTF8.GetString(this.GetAP());
                if (opacity >= 0 && opacity < 1 && !apStr.StartsWith("/H gs"))
                    this.SetAP(Encoding.UTF8.GetBytes("/H gs\n" + apStr));
                return;
            }

            byte[] bFill = ColorString(fill, "f");
            byte[] bStroke = ColorString(stroke, "c");

            Matrix pCTM = Parent.TransformationMatrix;
            Matrix iMat = ~pCTM; // inverse page transf. matrix

            string dashStr = "";
            List<byte> bDash = new List<byte>();
            UTF8Encoding utf8 = new UTF8Encoding();
            if (dt != null && dt.Length > 0)
            {
                string[] dashes = new string[dt.Length];
                for (int i = 0; i < dt.Length; i++)
                {
                    dashes[i] = Convert.ToString(dt[i]);
                }
                dashStr = "[" + string.Join(" ", dashes) + "] 0 d \n";
                bDash.AddRange(utf8.GetBytes(dashStr));
            }
            else
                bDash = null;
                
            PdfLineEnding line_end_le = LineEnds.Item1;
            PdfLineEnding line_end_ri = LineEnds.Item2;

            // read contents as created by MuPDF
            byte[] ap = GetAP();
            List<string> apTab = new List<string>(Encoding.UTF8.GetString(ap).Split('\n'));
            bool apUpdated = false;

            if (annotType == PdfAnnotType.PDF_ANNOT_REDACT)
            {
                if (crossOut)
                {
                    apUpdated = true;
                    apTab.RemoveAt(apTab.Count - 1);
                    string t = apTab[0];
                    string ll = apTab[1];
                    string lr = apTab[2];
                    string ur = apTab[3];
                    string ul = apTab[4];
                    apTab.Add(lr);
                    apTab.Add(ll);
                    apTab.Add(ur);
                    apTab.Add(ll);
                    apTab.Add(ul);
                    apTab.Add("S");
                }

                List<string> nTab = new List<string>();
                if (borderWidth > 0 || bStroke != null)
                {
                    apUpdated = true;
                    nTab =
                        borderWidth > 0
                            ? new List<string>() { string.Format("{0} w", borderWidth) }
                            : new List<string>();
                    for (int i = 0; i < apTab.Count; i++)
                    {
                        string line = apTab[i];
                        if (line.EndsWith("w"))
                            continue;
                        if (line.EndsWith("RG") && bStroke != null)
                            line = bStroke.Take(bStroke.Length - 1).ToString();
                        nTab.Add(line);
                    }
                    apTab = nTab;
                }
                ap = utf8.GetBytes(string.Join("\n", apTab.ToArray()));
            }

            if (
                annotType == PdfAnnotType.PDF_ANNOT_POLYGON
                || annotType == PdfAnnotType.PDF_ANNOT_POLY_LINE
            )
            {
                List<string> newApTab = apTab;
                newApTab.RemoveAt(newApTab.Count - 1);
                ap = MergeByte(
                    Utils.ToByte(string.Join("\n", newApTab.ToArray())),
                    Utils.ToByte("\n")
                );
                apUpdated = true;
                if (bFill != null && bFill.Length > 0)
                {
                    if (annotType == PdfAnnotType.PDF_ANNOT_POLYGON)
                        ap = MergeByte(MergeByte(ap, bFill), Utils.ToByte("b"));
                    else if (annotType == PdfAnnotType.PDF_ANNOT_POLY_LINE)
                        ap = MergeByte(ap, Utils.ToByte("S"));
                }
                else
                {
                    if (annotType == PdfAnnotType.PDF_ANNOT_POLYGON)
                        ap = MergeByte(ap, Utils.ToByte("s"));
                    else if (annotType == PdfAnnotType.PDF_ANNOT_POLY_LINE)
                        ap = MergeByte(ap, Utils.ToByte("S"));
                }
            }
                                
            if (bDash != null)
            {
                ap = MergeByte(bDash.ToArray(), ap);
                // reset dashing -only applies for LINE annots with line ends given
                ap = Utils.ReplaceBytes(ap, utf8.GetBytes("\nS\n"), utf8.GetBytes("\nS\n[] 0 d\n"), 1);
                apUpdated = true;
            }

            if (!string.IsNullOrEmpty(opaCode))
            {
                ap = MergeByte(utf8.GetBytes(opaCode), ap);
                apUpdated = true;
            }

            ap = MergeByte(MergeByte(Utils.ToByte("q\n"), ap), Utils.ToByte("\nQ\n"));

            // the following handles line end symbols for 'Polygon' and 'Polyline'
            if (
                ((int)line_end_le + (int)line_end_ri) > 0
                && (
                    annotType == PdfAnnotType.PDF_ANNOT_POLYGON
                    || annotType == PdfAnnotType.PDF_ANNOT_POLY_LINE
                )
            )
            {
                List<LE_FUNCTION> leFuncs = new List<LE_FUNCTION>()
                {
                    null,
                    le_square,
                    le_circle,
                    le_diamond,
                    le_openarrow,
                    le_closedarrow,
                    le_butt,
                    le_ropenarrow,
                    le_rclosedarrow,
                    le_slash
                };

                float d = 2 * Math.Max(1, Border.Width);
                rect = Rect + new Rect(-d, -d, d, d);
                apUpdated = true;
                List<Point> points = Vertices;
                Point p1 = null;
                Point p2 = null;

                if ((int)line_end_le > 0 && (int)line_end_le < leFuncs.Count)
                {
                    p1 = points[0] * iMat;
                    p2 = points[1] * iMat;
                    string left = leFuncs[(int)line_end_le](this, p1, p2, false, fillColor);
                    ap = MergeByte(ap, Utils.ToByte(left));
                }

                if ((int)line_end_ri > 0 && (int)line_end_ri < leFuncs.Count)
                {
                    p1 = points[points.Count-2] * iMat;
                    p2 = points[points.Count-1] * iMat;
                    string left = leFuncs[(int)line_end_ri](this, p1, p2, true, fillColor);
                    ap = MergeByte(ap, Utils.ToByte(left));
                }
            }

            if (apUpdated)
            {
                if (rect != null)
                {
                    SetRect(rect);
                    SetAP(ap, 1);
                }
                else
                    SetAP(ap, 0);
            }

            // handle annotation rotations
            if (    // only these types are supported
                !(
                    new List<PdfAnnotType>()
                    {
                        PdfAnnotType.PDF_ANNOT_CARET,
                        PdfAnnotType.PDF_ANNOT_CIRCLE,
                        PdfAnnotType.PDF_ANNOT_FILE_ATTACHMENT,
                        PdfAnnotType.PDF_ANNOT_INK,
                        PdfAnnotType.PDF_ANNOT_LINE,
                        PdfAnnotType.PDF_ANNOT_POLY_LINE,
                        PdfAnnotType.PDF_ANNOT_POLYGON,
                        PdfAnnotType.PDF_ANNOT_SQUARE,
                        PdfAnnotType.PDF_ANNOT_STAMP,
                        PdfAnnotType.PDF_ANNOT_TEXT
                    }
                ).Contains((PdfAnnotType)annotType)
            )
            {
                return;
            }

            int rot = Rotation; // get value from annot object
            if (rot == -1) // nothing to change
                return;
            Point M = (this.Rect.TopLeft + this.Rect.BottomRight) / 2.0f;
            Quad quad = null;
            if (rot == 0)
            {
                if ((apnMat - new Matrix(1, 1)).Abs() < 1e-5)
                    return;

                quad = this.Rect.Morph(M, ~apnMat);
                SetRect(quad.Rect);
                SetApnMatrix(new Matrix(1.0f, 1.0f));
                return;
            }

            Matrix mat = new Matrix(rot);
            quad = this.Rect.Morph(M, mat);
            SetRect(quad.Rect);
            SetApnMatrix(apnMat * mat);
        }

        internal static string le_square(
            Annot annot,
            Point p1,
            Point p2,
            bool lr,
            float[] fillColor
        )
        {
            (
                Matrix m,
                Matrix im,
                Point L,
                Point R,
                float w,
                string scol,
                string fcol,
                string opacity
            ) = le_annot_parms(annot, p1, p2, fillColor);
            float shift = 2.5f; // 2*shift*width = length of square edge
            float d = shift * Math.Max(1, w);
            Point M = lr ? R - new Point(d / 2.0f, 0) : L + new Point(d / 2.0f, 0);

            Rect r = new Rect(M, M) + new Rect(-d, -d, d, d); // the square
            // the square makes line longer by (2*shift - 1)*width
            Point p = r.TopLeft * im;
            string ap = string.Format("q\n{0}{1} {2} m\n", opacity, p.X, p.Y);
            p = r.TopRight * im;
            ap += string.Format("{0} {1} l\n", p.X, p.Y);
            p = r.BottomRight * im;
            ap += string.Format("{0} {1} l\n", p.X, p.Y);
            p = r.BottomLeft * im;
            ap += string.Format("{0} {1} l\n", p.X, p.Y);
            ap += string.Format("{0} w\n", w);
            ap += scol + fcol + "b\nQ\n";
            return ap;
        }

        internal static string le_diamond(
            Annot annot,
            Point p1,
            Point p2,
            bool lr,
            float[] fillColor
        )
        {
            (
                Matrix m,
                Matrix im,
                Point L,
                Point R,
                float w,
                string scol,
                string fcol,
                string opacity
            ) = le_annot_parms(annot, p1, p2, fillColor);
            float shift = 2.5f; // 2*shift*width = length of square edge
            float d = shift * Math.Max(1, w);
            Point M = lr ? R - new Point(d / 2.0f, 0) : L + new Point(d / 2.0f, 0);

            Rect r = new Rect(M, M) + new Rect(-d, -d, d, d); // the square
            // the square makes line longer by(2 * shift - 1)*width
            Point p = (r.TopLeft + (r.BottomLeft - r.TopLeft) * 0.5f) * im;
            string ap = string.Format("q\n{0}{1} {2} m\n", opacity, p.X, p.Y);
            p = (r.TopLeft + (r.TopRight - r.TopLeft) * 0.5f) * im;
            ap += string.Format("{0} {1} l\n", p.X, p.Y);
            p = (r.TopRight + (r.BottomLeft - r.BottomRight) * 0.5f) * im;
            ap += string.Format("{0} {1} l\n", p.X, p.Y);
            p = (r.BottomRight + (r.BottomLeft - r.BottomRight) * 0.5f) * im;
            ap += string.Format("{0} {1} l\n", p.X, p.Y);
            ap += string.Format("{0} w\n", w);
            ap += scol + fcol + "b\nQ\n";
            return ap;
        }

        internal static string le_openarrow(
            Annot annot,
            Point p1,
            Point p2,
            bool lr,
            float[] fillColor
        )
        {
            (
                Matrix m,
                Matrix im,
                Point L,
                Point R,
                float w,
                string scol,
                string fcol,
                string opacity
            ) = le_annot_parms(annot, p1, p2, fillColor);
            float shift = 2.5f;
            float d = shift * Math.Max(1, w);
            p2 = lr ? R + new Point(d / 2.0f, 0) : L - new Point(d / 2.0f, 0);

            p1 = lr ? p2 + new Point(-2 * d, -d) : p2 + new Point(2 * d, -d);

            Point p3 = lr ? p2 + new Point(-2 * d, d) : p2 + new Point(2 * d, d);

            p1 *= im;
            p2 *= im;
            p3 *= im;
            string ap = "\nq\n" + opacity + p1.X.ToString() + " " + p1.Y.ToString() + " m\n";
            ap += p2.X.ToString() + " " + p2.Y.ToString() + " l\n";
            ap += p3.X.ToString() + " " + p3.Y.ToString() + " l\n";
            ap += w.ToString() + " w\n";
            ap += scol + "S\nQ\n";
            return ap;
        }

        internal static string le_closedarrow(
            Annot annot,
            Point p1,
            Point p2,
            bool lr,
            float[] fillColor
        )
        {
            (
                Matrix m,
                Matrix im,
                Point L,
                Point R,
                float w,
                string scol,
                string fcol,
                string opacity
            ) = le_annot_parms(annot, p1, p2, fillColor);
            float shift = 2.5f;
            float d = shift * Math.Max(1, w);
            p2 = lr ? R + new Point(d / 2.0f, 0) : L - new Point(d / 2.0f, 0);
            p1 = lr ? p2 + new Point(-2 * d, -d) : p2 + new Point(2 * d, -d);
            Point p3 = lr ? p2 + new Point(-2 * d, d) : p2 + new Point(2 * d, d);
            p1 *= im;
            p2 *= im;
            p3 *= im;
            string ap = $"\nq\n{opacity}{p1.X} {p1.Y} m\n";
            ap += $"{p2.X} {p2.Y} l\n";
            ap += $"{p3.X} {p3.Y} l\n";
            ap += $"{w} w\n";
            ap += $"{scol}{fcol}b\nQ\n";
            return ap;
        }

        internal static string le_butt(
            Annot annot,
            Point p1,
            Point p2,
            bool lr,
            float[] fillColor
        )
        {
            (
                Matrix m,
                Matrix im,
                Point L,
                Point R,
                float w,
                string scol,
                string fcol,
                string opacity
            ) = le_annot_parms(annot, p1, p2, fillColor);
            float shift = 3;
            float d = shift * Math.Max(1, w);
            var M = lr ? R : L;
            var top = new Point(M.X, M.Y - d / 2.0f) * im;
            var bot = new Point(M.X, M.Y + d / 2.0f) * im;
            var ap = $"\nq\n{opacity}{top.X} {top.Y} m\n";
            ap += $"{bot.X} {bot.Y} l\n";
            ap += $"{w} w\n";
            ap += $"{scol}s\nQ\n";
            return ap;
        }

        internal static string le_ropenarrow(
            Annot annot,
            Point p1,
            Point p2,
            bool lr,
            float[] fillColor
        )
        {
            (
                Matrix m,
                Matrix im,
                Point L,
                Point R,
                float w,
                string scol,
                string fcol,
                string opacity
            ) = le_annot_parms(annot, p1, p2, fillColor);
            float shift = 2.5f;
            float d = shift * Math.Max(1, w);
            p2 = lr ? new Point(R.X - (d / 3.0f), R.Y) : new Point(L.X + (d / 3.0f), L.Y);
            p1 = lr ? new Point(p2.X + (2 * d), p2.Y - d) : new Point(p2.X - (2 * d), p2.Y - d);
            Point p3 = lr
                ? new Point(p2.X + (2 * d), p2.Y + d)
                : new Point(p2.X - (2 * d), p2.Y + d);
            p1 *= im;
            p2 *= im;
            p3 *= im;
            string ap = "\nq\n" + opacity + p1.X.ToString() + " " + p1.Y.ToString() + " m\n";
            ap += p2.X.ToString() + " " + p2.Y.ToString() + " l\n";
            ap += p3.X.ToString() + " " + p3.Y.ToString() + " l\n";
            ap += w.ToString() + " w\n";
            ap += scol + fcol + "S\nQ\n";

            return ap;
        }

        internal static string le_rclosedarrow(
            Annot annot,
            Point p1,
            Point p2,
            bool lr,
            float[] fillColor
        )
        {
            (
                Matrix m,
                Matrix im,
                Point L,
                Point R,
                float w,
                string scol,
                string fcol,
                string opacity
            ) = le_annot_parms(annot, p1, p2, fillColor);
            float shift = 2.5f;
            float d = shift * Math.Max(1, w);
            p2 = lr ? new Point(R.X - (2 * d), R.Y) : new Point(L.X + (2 * d), L.Y);
            p1 = lr ? p2 + new Point(2 * d, -d) : p2 + new Point(-2 * d, -d);
            Point p3 = lr ? p2 + new Point(2 * d, d) : p2 + new Point(-2 * d, d);
            p1 *= im;
            p2 *= im;
            p3 *= im;
            string ap = "\nq\n" + opacity + p1.X + " " + p1.Y + " m\n";
            ap += p2.X + " " + p2.Y + " l\n";
            ap += p3.X + " " + p3.Y + " l\n";
            ap += w + " w\n";
            ap += scol + fcol + "b\nQ\n";
            
            return ap;
        }

        internal static string le_slash(
            Annot annot,
            Point p1,
            Point p2,
            bool lr,
            float[] fillColor
        )
        {
            (
                Matrix m,
                Matrix im,
                Point L,
                Point R,
                float w,
                string scol,
                string fcol,
                string opacity
            ) = le_annot_parms(annot, p1, p2, fillColor);
            float rw = 1.1547f * Math.Max(1, w) * 1.0f;
            Point M = lr ? R : L;
            Rect r = new Rect(M.X - rw, M.Y - 2 * w, M.X + rw, M.Y + 2 * w);
            Point top = r.TopLeft * im;
            Point bot = r.BottomRight * im;
            string ap = "\nq\n" + opacity + top.X + " " + top.Y + " m\n";
            ap += bot.X + " " + bot.Y + " l\n";
            ap += w + " w\n";
            ap += scol + "b\nQ\n";
            
            return ap;
        }

        internal static string le_circle(
            Annot annot,
            Point p1,
            Point p2,
            bool lr,
            float[] fillColor
        )
        {
            (
                Matrix m,
                Matrix im,
                Point L,
                Point R,
                float w,
                string scol,
                string fcol,
                string opacity
            ) = le_annot_parms(annot, p1, p2, fillColor);
            float shift = 2.5f;
            float d = shift * Math.Max(1, w);
            Point M = lr ? R - new Point(d / 2.0f, 0) : L + new Point(d / 2.0f, 0);

            Rect r = (new Rect(M, M)) + new Rect(-d, -d, d, d);
            string ap =
                "q\n"
                + opacity
                + oval_string(
                    r.TopLeft * im,
                    r.TopRight * im,
                    r.BottomRight * im,
                    r.BottomLeft * im
                );
            ap += string.Format("{0} w\n", w);
            ap += scol + fcol + "b\nQ\n";
            
            return ap;
        }

        internal static string bezier(Point p, Point q, Point r)
        {
            string f = "{0} {1} {2} {3} {4} {5} c\n";
            
            return string.Format(f, p.X, p.Y, q.X, q.Y, r.X, r.Y);
        }

        internal static string oval_string(Point p1, Point p2, Point p3, Point p4)
        {
            float kappa = 0.55228474983f;
            Point ml = p1 + (p4 - p1) * 0.5f;
            Point mo = p1 + (p2 - p1) * 0.5f;
            Point mr = p2 + (p3 - p2) * 0.5f;
            Point mu = p4 + (p3 - p4) * 0.5f;
            Point ol1 = ml + (p1 - ml) * kappa;
            Point ol2 = mo + (p1 - mo) * kappa;
            Point or1 = mo + (p2 - mo) * kappa;
            Point or2 = mr + (p2 - mr) * kappa;
            Point ur1 = mr + (p3 - mr) * kappa;
            Point ur2 = mu + (p3 - mu) * kappa;
            Point ul1 = mu + (p4 - mu) * kappa;
            Point ul2 = ml + (p4 - ml) * kappa;

            string ap = string.Format("{0} {1} m\n", ml.X, ml.Y);
            ap += Annot.bezier(ol1, ol2, mo);
            ap += Annot.bezier(or1, or2, mr);
            ap += Annot.bezier(ur1, ur2, mu);
            ap += Annot.bezier(ul1, ul2, ml);
            
            return ap;
        }

        internal static (
            Matrix,
            Matrix,
            Point,
            Point,
            float,
            string,
            string,
            string
        ) le_annot_parms(Annot annot, Point p1, Point p2, float[] fillColor)
        {
            float w = annot.Border.Width;
            float[] sc = annot.Colors.Stroke;
            if (sc == null)
                sc = new float[3] { 0, 0, 0 };

            string[] scStr = new string[sc.Length];
            for (int i = 0; i < sc.Length; i++)
            {
                scStr[i] = sc[i].ToString();
            }
            
            string scol = string.Join(" ", scStr) + " RG\n";
            scStr = null;
            float[] fc = null;
            if (fillColor != null)
                fc = fillColor;
            else
                fc = annot.Colors.Fill;
            if (fc == null)
                fc = new float[3] { 1, 1, 1 };

            string[] fcolStr = new string[fc.Length];
            for (int i = 0; i < fc.Length; i++)
            {
                fcolStr[i] = fc[i].ToString();
            }
            string fcol = string.Join(" ", fcolStr) + " rg\n";

            Point np1 = p1;
            Point np2 = p2;
            Matrix m = new Matrix(Utils.HorMatrix(np1, np2));
            Matrix im = ~m;
            Point L = np1 * m;
            Point R = np2 * m;
            string opacity = "";
            if (0 <= annot.Opacity && annot.Opacity < 1)
                opacity = "/H gs\n";
            else
                opacity = "";

            return (m, im, L, R, w, scol, fcol, opacity);
        }

        public static byte[] MergeByte(byte[] a, byte[] b)
        {
            return Enumerable.Concat(a, b).ToArray();
        }

        public static byte[] ColorString(dynamic cs, string code)
        {
            UTF8Encoding utf8 = new UTF8Encoding();
            if (cs is List<float>)
                cs = cs.ToArray();
            string cc = ColorCode(cs, code);
            if (cc == "")
                return utf8.GetBytes("");
            
            return utf8.GetBytes(cc + "\n");
        }

        public static string ColorCode(float cs, string code)
        {
            string s = "";

            s = string.Format("{0} ", cs);
            if (code == "c")
                return s + "G ";

            return s + "g ";
        }

        public static string ColorCode(float[] cs, string code)
        {
            string s = "";
            if (cs == null || cs.Length == 0)
                return "";

            Utils.CheckColor(cs);
            if (cs.Length == 1)
            {
                s = string.Format("{0} ", cs[0]);
                if (code == "c")
                    return s + "G ";
                return s + "g ";
            }

            if (cs.Length == 3)
            {
                s = string.Format("{0} {1} {2} ", cs[0], cs[1], cs[2]);
                if (code == "c")
                    return s + "RG ";
                return s + "rg ";
            }

            s = string.Format("{0} {1} {2} {3} ", cs[0], cs[1], cs[2], cs[3]);
            if (code == "c")
                return s + "K ";
            
            return s + "k ";
        }

        public dynamic GetText(
            Page page,
            string option = "text",
            Rect clip = null,
            int flags = 0,
            TextPage stPage = null,
            bool sort = false,
            char[] delimiters = null
        )
        {
            return Utils.GetText(page, option, clip, flags, stPage, sort, delimiters);
        }

        /// <summary>
        /// Get text in the box area
        /// </summary>
        /// <param name="page"></param>
        /// <param name="rect"></param>
        /// <param name="textPage"></param>
        /// <returns></returns>
        public string GetTextbox(Page page, Rect rect, TextPage textPage)
        {
            return Utils.GetTextbox(page, rect, textPage);
        }

        public void UpdateFile(
            byte[] buffer = null,
            string fileName = null,
            string uFilename = null,
            string desc = null
        )
        {
            PdfAnnot annot = _nativeAnnotion;
            PdfObj annotObj = annot.pdf_annot_obj();
            PdfDocument pdf = annotObj.pdf_get_bound_document();
            pdf_annot_type type = annot.pdf_annot_type();

            if (type != pdf_annot_type.PDF_ANNOT_FILE_ATTACHMENT)
            {
                throw new Exception(Utils.ErrorMessages["MSG_BAD_ANNOT_TYPE"]);
            }
            PdfObj stream = Utils.pdf_dict_getl(annotObj, new string[] { "FS", "EF", "F" });

            if (stream.m_internal == null)
                Console.WriteLine("bad PDF: no /EF object");
            
            PdfObj fs = annotObj.pdf_dict_get(new PdfObj("FS"));
            FzBuffer res = Utils.BufferFromBytes(buffer);

            if (buffer != null && res.m_internal == null)
                throw new Exception(Utils.ErrorMessages["MSG_BAD_BUFFER"]);
            if (res.m_internal != null)
            {
                Utils.UpdateStream(pdf, stream, res, 1);
                uint len = res.fz_buffer_storage(
                    new SWIGTYPE_p_p_unsigned_char(IntPtr.Zero, false)
                );
                PdfObj l = mupdf.mupdf.pdf_new_int((long)len);
                stream.pdf_dict_put(new PdfObj("DL"), l);
                Utils.pdf_dict_putl(stream, l, new string[] { "Params", "Size" });
            }
            
            if (fileName != null)
            {
                stream.pdf_dict_put_text_string(new PdfObj("F"), fileName);
                fs.pdf_dict_put_text_string(new PdfObj("F"), fileName);
                stream.pdf_dict_put_text_string(new PdfObj("UF"), fileName);
                fs.pdf_dict_put_text_string(new PdfObj("UF"), fileName);
                annotObj.pdf_dict_put_text_string(new PdfObj("Contents"), fileName);
            }
            
            if (uFilename != null)
            {
                stream.pdf_dict_put_text_string(new PdfObj("UF"), uFilename);
                fs.pdf_dict_put_text_string(new PdfObj("UF"), uFilename);
            }
            
            if (desc != null)
            {
                stream.pdf_dict_put_text_string(new PdfObj("Desc"), desc);
                fs.pdf_dict_put_text_string(new PdfObj("Desc"), desc);
            }
        }
    }

    internal class Factory : mupdf.PdfFilterFactory2
    {
        private PdfSanitizeFilterOptions sopts;

        public Factory(PdfSanitizeFilterOptions sopts)
            : base()
        {
            this.sopts = sopts;
            use_virtual_filter();
        }

        public override pdf_processor filter(
            fz_context arg_0,
            pdf_document doc,
            pdf_processor chain,
            int stParents,
            fz_matrix transform,
            pdf_filter_options options
        )
        {
            return mupdf.mupdf.ll_pdf_new_sanitize_filter(
                doc,
                chain,
                stParents,
                transform,
                options,
                new SWIGTYPE_p_void(
                    pdf_sanitize_filter_options.getCPtr(sopts.internal_()).Handle,
                    true
                )
            );
        }
    }
}
