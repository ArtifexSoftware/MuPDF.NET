using mupdf;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace MuPDF.NET
{
    public class Widget
    {
        static Widget()
        {
            Utils.InitApp();
        }

        /// <summary>
        /// A list of up to 4 floats defining the field’s border color
        /// </summary>
        public float[] BorderColor { get; set; }

        /// <summary>
        /// A string defining the line style of the field’s border
        /// </summary>
        public string BorderStyle { get; set; }

        /// <summary>
        /// A float defining the width of the border line
        /// </summary>
        public float BorderWidth { get; set; }

        /// <summary>
        /// A list/tuple of integers defining the dash properties of the border line
        /// </summary>
        public int[] BorderDashes { get; set; }

        /// <summary>
        /// A sequence of strings defining the valid choices of list boxes and combo boxes
        /// </summary>
        public List<dynamic> ChoiceValues { get; set; }

        public int RbParent { get; set; }

        /// <summary>
        /// A mandatory string defining the field’s name
        /// </summary>
        public string FieldName { get; set; }

        /// <summary>
        /// An optional string containing an “alternate” field name
        /// </summary>
        public string FieldLabel { get; set; }

        /// <summary>
        /// The value of the field
        /// </summary>
        public string FieldValue { get; set; }

        /// <summary>
        /// An integer defining a large amount of properties of a field
        /// </summary>
        public int FieldFlags { get; set; }

        /// <summary>
        /// A mandatory integer defining the field type
        /// </summary>
        public int FieldType { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public int FieldDisplay { get; set; }

        /// <summary>
        /// A string describing (and derived from) the field type
        /// </summary>
        public string FieldTypeString { get; set; }

        /// <summary>
        /// A list of up to 4 floats defining the field’s background color
        /// </summary>
        public float[] FillColor { get; set; }

        /// <summary>
        /// The caption string of a button-type field
        /// </summary>
        public string ButtonCaption { get; set; }

        /// <summary>
        /// A bool indicating the signing status of a signature field, else false
        /// </summary>
        public bool IsSigned { get; set; }

        /// <summary>
        /// A list of 1, 3 or 4 floats defining the text color
        /// </summary>
        public float[] TextColor { get; set; }

        /// <summary>
        /// A string defining the font to be used
        /// </summary>
        public string TextFont { get; set; }

        /// <summary>
        /// A float defining the text fontsize
        /// </summary>
        public float TextFontSize { get; set; }

        /// <summary>
        /// An integer defining the maximum number of text characters
        /// </summary>
        public int TextMaxLen { get; set; }

        public int TextFormat { get; set; }

        public string TextDa { get; set; }

        /// <summary>
        /// JavaScript text (unicode) for an action associated with the widget, or null
        /// </summary>
        public string Script { get; set; }

        /// <summary>
        /// JavaScript text (unicode) to be performed when the user types a key-stroke into a text field or combo box or modifies the selection in a scrollable list box
        /// </summary>
        public string ScriptStroke { get; set; }

        /// <summary>
        /// JavaScript text (unicode) to be performed before the field is formatted to display its current value
        /// </summary>
        public string ScriptFormat { get; set; }

        /// <summary>
        /// JavaScript text (unicode) to be performed when the field’s value is changed
        /// </summary>
        public string ScriptChange { get; set; }

        /// <summary>
        /// JavaScript text (unicode) to be performed to recalculate the value of this field when that of another field changes
        /// </summary>
        public string ScriptCalc { get; set; }

        /// <summary>
        /// JavaScript text (unicode) to be performed on focusing this field
        /// </summary>
        public string ScriptBlur { get; set; }

        /// <summary>
        /// JavaScript text (unicode) to be performed on focusing this field
        /// </summary>
        public string ScriptFocus { get; set; }

        /// <summary>
        /// The PDF xref of the widget
        /// </summary>
        public int Xref { get; set; }

        /// <summary>
        /// The rectangle containing the field
        /// </summary>
        public Rect Rect { get; set; }

        public Page Parent { get; set; }

        public PdfAnnot _annot { get; set; }

        public Widget(Page page)
        {
            Parent = page;
            BorderColor = null;
            BorderStyle = "S";
            BorderWidth = 0;
            BorderDashes = null;
            ChoiceValues = null;
            RbParent = 0;

            FieldName = null;
            FieldLabel = null;
            FieldValue = null;
            FieldFlags = 0;
            FieldType = 0;
            FieldDisplay = 0;
            FieldTypeString = null;

            FillColor = null;
            ButtonCaption = null;
            IsSigned = false;
            TextColor = new float[] { 0, 0, 0 };
            TextFont = "Helv";
            TextFontSize = 0;
            TextMaxLen = 0;
            TextFormat = 0;
            TextDa = "";

            Script = null;
            ScriptStroke = null;
            ScriptFormat = null;
            ScriptCalc = null;
            ScriptChange = null;
            ScriptBlur = null;
            ScriptFocus = null;

            Rect = null;
            Xref = 0;
        }

        public override string ToString()
        {
            return $"Widget:(field_type={FieldTypeString}) script={Script}";
        }

        /// <summary>
        /// Ensure text_font is from our list and correctly spelled.
        /// </summary>
        /// <returns></returns>
        public void AdjustFont()
        {
            if (string.IsNullOrEmpty(TextFont))
            {
                TextFont = "Helv";
                return;
            }
            List<string> validFonts = new List<string>() { "Cour", "TiRo", "Helv", "ZaDb" };
            foreach (string f in validFonts)
                if (TextFont.ToLower() == f.ToLower())
                {
                    TextFont = f;
                    return;
                }
            TextFont = "Helv";
            return;
        }

        /// <summary>
        /// Any widget type checks.
        /// </summary>
        /// <returns></returns>
        public void Checker()
        {
            if (!(FieldType >= 1 && FieldType < 8))
                throw new Exception("bad field type");

            if (FieldType == (int)PdfWidgetType.PDF_WIDGET_TYPE_RADIOBUTTON)
            {
                Document doc = Parent.Parent;
                (string kidsType, string kidsValue) = doc.GetKeyXref(Xref, "Parent/Kids");
                if (kidsType == "array")
                {
                    //List<int> xrefs = kidsValue.Substring(1, kidsValue.Length - 2).Replace("0 R", "").Split("").Select(x => int.Parse(x)).ToList();
                    List<int> xrefs = kidsValue.Substring(1, kidsValue.Length - 2).Replace("0 R", " ").Split(' ').Select(x => int.Parse(x)).ToList();
                    foreach (int xref in xrefs)
                    {
                        if (xref != Xref)
                            doc.SetKeyXRef(xref, "AS", "/Off");
                    }
                }
            }
        }

        /// <summary>
        /// Extract font name, size and color from default appearance string (/DA object).
        /// Equivalent to 'pdf_parse_default_appearance' function in MuPDF's 'pdf-annot.c'.
        /// </summary>
        /// <returns></returns>
        public void ParseDa()
        {
            if (string.IsNullOrEmpty(TextDa))
                return;
            string font = "Helv";
            float fontSize = 0;
            float[] col = { 0, 0, 0 };
            string[] dat = TextDa.Split(' ');    // split on any whitespace
            for (int i = 0; i < dat.Length; i++)
            {
                string item = dat[i];
                if (item == "Tf")
                {
                    font = dat[i - 2].Substring(1);
                    fontSize = float.Parse(dat[i - 1]);
                    dat[i] = dat[i - 1] = dat[i - 2] = "";
                    continue;
                }
                if (item == "g")
                {
                    col = new float[] { float.Parse(dat[i - 1]) };
                    dat[i] = dat[i - 1] = "";
                    continue;
                }
                if (item == "rg")
                {
                    col = new float[3];
                    for (int j = i - 3; j < i; j++)
                        col[j - i + 3] = float.Parse(dat[j]);
                    dat[i] = dat[i - 1] = dat[i - 2] = dat[i - 3] = "";
                    continue;
                }
            }
            TextFont = font;
            TextFontSize = fontSize;
            TextColor = col;
            TextDa = "";
        }

        /// <summary>
        /// Validate the class entries.
        /// </summary>
        /// <returns></returns>
        public void Validate()
        {
            if (Rect.IsInfinite || Rect.IsEmpty)
                throw new Exception("bad rect");
            if (string.IsNullOrEmpty(FieldName))
                throw new Exception("field name missing");
            if (FieldLabel == "Unnamed")
                FieldLabel = null;
            Utils.CheckColor(BorderColor);
            Utils.CheckColor(FillColor);
            if (TextColor == null)
                TextColor = new float[] { 0, 0, 0 };
            Utils.CheckColor(TextColor);

            if (BorderWidth == 0)
                BorderWidth = 0;
            if (TextFontSize == 0)
                TextFontSize = 0;

            BorderStyle = BorderStyle.ToUpper().Substring(0, 1);

            // standardize content of JavaScript entries
            bool btnType = (new List<PdfWidgetType> {
                PdfWidgetType.PDF_WIDGET_TYPE_BUTTON,
                PdfWidgetType.PDF_WIDGET_TYPE_CHECKBOX,
                PdfWidgetType.PDF_WIDGET_TYPE_RADIOBUTTON}).Contains((PdfWidgetType)FieldType);
            if (string.IsNullOrEmpty(Script))
                Script = null;

            // buttons cannot have the following script actions
            if (btnType || string.IsNullOrEmpty(ScriptCalc))
                ScriptCalc = null;

            if (btnType || string.IsNullOrEmpty(ScriptChange))
                ScriptChange = null;

            if (btnType || string.IsNullOrEmpty(ScriptFormat))
                ScriptFormat = null;

            if (btnType || string.IsNullOrEmpty(ScriptStroke))
                ScriptStroke = null;

            if (btnType || string.IsNullOrEmpty(ScriptBlur))
                ScriptBlur = null;

            if (btnType || string.IsNullOrEmpty(ScriptFocus))
                ScriptFocus = null;

            Checker(); // any field_type specific checks
        }

        /// <summary>
        /// Propagate the field flags.
        /// If this widget has a "/Parent", set its field flags and that of all
        /// its /Kids widgets to the value of the current widget.
        /// Only possible for widgets existing in the PDF.
        /// </summary>
        /// <returns>true/false</returns>
        public bool SyncFlags()
        {
            if (Xref == 0)
                return false;  // no xref: widget not in the PDF
            Document doc = this.Parent.Parent; // the owning document
            if (doc == null)
                return false;
            PdfDocument pdf = Document.AsPdfDocument(doc);
            // load underlying PDF object
            PdfObj pdf_widget = pdf.pdf_load_object(Xref);
            PdfObj parent = pdf_widget.pdf_dict_get(new PdfObj("Parent"));
            if (parent.pdf_is_dict() == 0)
                return false;  // no /Parent: nothing to do

            // put the field flags value into the parent field flags:
            parent.pdf_dict_put_int(new PdfObj("Ff"), this.FieldFlags);

            // also put that value into all kids of the Parent
            PdfObj kids = parent.pdf_dict_get(new PdfObj("Kids"));
            if (kids.pdf_is_array() == 0)
            {
                Console.WriteLine("warning: malformed PDF, Parent has no Kids array");
                return false;  // no /Kids: should never happen!
            }

            for (int i = 0; i < kids.pdf_array_len(); i++)
            {
                // access kid widget, and do some precautionary checks
                PdfObj kid = kids.pdf_array_get(i);
                if (kid.pdf_is_dict() == 0)
                    continue;  // not a dict: skip
                int xref = kid.pdf_to_num();  // get xref of the kid
                if (xref == this.Xref)  // skip self widget
                    continue;
                PdfObj subtype = kid.pdf_dict_get(new PdfObj("Subtype"));
                if (subtype.pdf_to_name() != "Widget")
                    continue;
                // put the field flags value into the kid field flags:
                kid.pdf_dict_put_int(new PdfObj("Ff"), this.FieldFlags);
            }

            return true;  // all done
        }

        /// <summary>
        /// Return the names of On / Off (i.e. selected / clicked or not) states a button field may have. While the ‘Off’ state usually is also named like so, the ‘On’ state is often given a name relating to the functional context, for example ‘Yes’, ‘Female’, etc.
        /// A button may have 'normal' or 'pressed down' appearances. While the 'Off'
        /// state is usually called like this, the 'On' state is often given a name
        /// relating to the functional context.
        /// </summary>
        /// <returns></returns>
        public Dictionary<string, List<string>> ButtonStates()
        {
            if (!(FieldType == 2 || FieldType == 5))
                return null;    // no button type
            Document doc = this.Parent.Parent;  // field already exists on page
            if (doc == null)
                return null;

            int xref = Xref;
            Dictionary<string, List<string>> states = new Dictionary<string, List<string>>();
            states.Add("normal", null);
            states.Add("down", null);
            (string, string) apn = doc.GetKeyXref(xref, "AP/N");
            if (apn.Item1 == "dict")
            {
                List<string> nstates = new List<string>();
                string t = apn.Item2.Substring(2, apn.Item2.Length - 2 - 2);
                string[] apnt = t.Split('/').Skip(1).ToArray();
                foreach (string x in apnt)
                    nstates.Add(x.Split()[0]);
                states["normal"] = nstates;
            }
            if (apn.Item1 == "xref")
            {
                List<string> nstates = new List<string>();
                int nxref = int.Parse(apn.Item2.Split(' ')[0]);
                string t = doc.GetXrefObject(nxref);
                string[] apnt = t.Split('/').Skip(1).ToArray();
                foreach (string x in apnt)
                    nstates.Add(x.Split()[0]);
                states["normal"] = nstates;
            }
            (string, string) apd = doc.GetKeyXref(xref, "AP/D");
            if (apd.Item1 == "dict")
            {
                List<string> dstates = new List<string>();
                string t = apd.Item2.Substring(2, apd.Item2.Length - 2 - 2);
                string[] apdt = t.Split('/').Skip(1).ToArray();
                foreach (string x in apdt)
                    dstates.Add(x.Split()[0]);
                states["down"] = dstates;
            }
            if (apd.Item1 == "xref")
            {
                List<string> dstates = new List<string>();
                int dxref = int.Parse(apd.Item2.Split(' ')[0]);
                string t = doc.GetXrefObject(dxref);
                string[] apdt = t.Split('/').Skip(1).ToArray();
                foreach (string x in apdt)
                    dstates.Add(x.Split()[0]);
                states["down"] = dstates;
            }
            return states;
        }

        /// <summary>
        /// Point to the next form field on the page.
        /// </summary>
        public dynamic Next
        {
            get
            {
                return (new Annot(_annot, Parent)).Next;
            }
        }

        /// <summary>
        /// Return the value of the “ON” state of check boxes and radio buttons. For check boxes this is always the value “Yes”. For radio buttons, this is the value to select / activate the button.
        /// </summary>
        /// <returns></returns>
        public string OnState()
        {
            if (!(FieldType == 2 || FieldType == 5))
                return null;
            if (FieldType == 2)
                return "Yes";
            Dictionary<string, List<string>> bstate = ButtonStates();
            if (bstate == null)
                bstate = new Dictionary<string, List<string>>();
            foreach (string k in bstate.Keys)
            {
                foreach (string v in bstate[k])
                {
                    if (v != "Off")
                        return v;
                }
            }
            Console.WriteLine("warning: radio button has no 'On' value");
            return "";
        }

        /// <summary>
        /// Reset the field’s value to its default – if defined – or remove it. Do not forget to issue update() afterwards.
        /// </summary>
        public void Reset()
        {
            Utils.ResetWidget(_annot);
        }

        /// <summary>
        /// After any changes to a widget, this method must be used to store them in the PDF 
        /// <param name="syncFlags">propagate field flags to parent and kids</param>
        /// </summary>
        public void Update(bool syncFlags = false)
        {
            Validate();
            AdjustFont(); // ensure valid text_font name

            // now create the /DA string
            TextDa = "";
            string fmt = "";

            if (TextColor != null && TextColor.Length == 3)
                fmt = $"{TextColor[0]} {TextColor[1]} {TextColor[2]} rg /" + "{0} {1} Tf" + TextDa;
            else if (TextColor.Length == 1)
                fmt = $"{TextColor[0]} g /" + "{0} {1} Tf" + TextDa;
            else if (TextColor.Length == 4)
                fmt = $"{TextColor[0]} {TextColor[1]} {TextColor[2]} {TextColor[3]} k /" + "{0} {1} Tf" + TextDa;
            TextDa = string.Format(fmt, TextFont, TextFontSize);

            // if widget has a '/AA/C' script, make sure it is in the '/CO'
            // array of the '/AcroForm' dictionary.
            if (!string.IsNullOrEmpty(ScriptCalc)) // there is a "calculation" script:
            {
                // make sure we are in the /CO array
                Utils.EnsureWidgetCalc(_annot);
            }
            
            Utils.SaveWidget(_annot, this);
            TextDa = "";

            // finally update the widget
            if (syncFlags)
                SyncFlags();    // propagate field flags to parent and kids
        }
    }
}
