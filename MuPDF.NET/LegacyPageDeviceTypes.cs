using System.Collections.Generic;

namespace MuPDF.NET
{
    // Legacy public type names from MuPDF.NET for source/binary compatibility.
    // Current internals use PageRunDevices/PageBboxLogDevice/JM_new_texttrace_device.

    public class BoxDevice : mupdf.FzDevice2
    {
        public List<BoxLog> rc { get; set; }
        public bool layers { get; set; }
        public string LayerName { get; set; } = "";

        public BoxDevice(List<BoxLog> rc, bool layers) : base()
        {
            this.rc = rc ?? new List<BoxLog>();
            this.layers = layers;
            use_virtual_begin_layer();
            use_virtual_end_layer();
        }

        public override void begin_layer(mupdf.fz_context arg_0, string arg_2) =>
            LayerName = string.IsNullOrEmpty(arg_2) ? "" : arg_2;

        public override void end_layer(mupdf.fz_context arg_0) => LayerName = "";
    }

    public class LineartDevice : mupdf.FzDevice2
    {
        public int SeqNo { get; set; }
        public int Depth { get; set; }
        public bool Clips { get; set; }
        public int Method { get; set; }
        public PathInfo PathDict { get; set; } = new PathInfo();
        public List<mupdf.FzRect> Scissors { get; set; } = new List<mupdf.FzRect>();
        public float LineWidth { get; set; }
        public mupdf.FzMatrix Ptm { get; set; } = new mupdf.FzMatrix();
        public mupdf.FzMatrix Ctm { get; set; } = new mupdf.FzMatrix();
        public mupdf.FzMatrix Rot { get; set; } = new mupdf.FzMatrix();
        public mupdf.FzPoint LastPoint { get; set; } = new mupdf.FzPoint();
        public mupdf.FzPoint FirstPoint { get; set; } = new mupdf.FzPoint();
        public int HaveMove { get; set; }
        public mupdf.FzRect PathRect { get; set; } = new mupdf.FzRect();
        public float PathFactor { get; set; }
        public int LineCount { get; set; }
        public int PathType { get; set; }
        public string LayerName { get; set; } = "";
        public List<PathInfo> Out { get; set; }

        public LineartDevice(List<PathInfo> rc, bool clips) : base()
        {
            Out = rc ?? new List<PathInfo>();
            Clips = clips;
            use_virtual_begin_layer();
            use_virtual_end_layer();
        }

        public override void begin_layer(mupdf.fz_context arg_0, string arg_2) =>
            LayerName = string.IsNullOrEmpty(arg_2) ? "" : arg_2;

        public override void end_layer(mupdf.fz_context arg_0) => LayerName = "";
    }

    public class Walker : mupdf.FzPathWalker2
    {
        public LineartDevice Dev;

        public Walker(LineartDevice dev) : base()
        {
            Dev = dev;
            use_virtual_moveto();
            use_virtual_lineto();
            use_virtual_curveto();
            use_virtual_closepath();
        }
    }

    public class TextTraceDevice : mupdf.FzDevice2
    {
        public int SeqNo { get; set; }
        public int Depth { get; set; }
        public bool Clips { get; set; }
        public int Method { get; set; }
        public PathInfo PathDict { get; set; } = new PathInfo();
        public List<mupdf.FzRect> Scissors { get; set; } = new List<mupdf.FzRect>();
        public float LineWidth { get; set; }
        public mupdf.FzMatrix Ptm { get; set; } = new mupdf.FzMatrix();
        public mupdf.FzMatrix Ctm { get; set; } = new mupdf.FzMatrix();
        public mupdf.FzMatrix Rot { get; set; } = new mupdf.FzMatrix();
        public mupdf.FzPoint LastPoint { get; set; } = new mupdf.FzPoint();
        public mupdf.FzRect PathRect { get; set; } = new mupdf.FzRect();
        public float PathFactor { get; set; }
        public int LineCount { get; set; }
        public int PathType { get; set; }
        public string LayerName { get; set; } = "";
        public List<SpanInfo> Out { get; set; }

        public TextTraceDevice(List<SpanInfo> o) : base()
        {
            Out = o ?? new List<SpanInfo>();
            use_virtual_begin_layer();
            use_virtual_end_layer();
        }

        public override void begin_layer(mupdf.fz_context arg_0, string arg_2) =>
            LayerName = string.IsNullOrEmpty(arg_2) ? "" : arg_2;

        public override void end_layer(mupdf.fz_context arg_0) => LayerName = "";
    }
}
