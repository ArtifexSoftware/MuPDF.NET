using System;
using System.Text;
using System.Drawing;

namespace BarcodeWriter.Core.Internal
{
    abstract class SymbologyDrawing2D : SymbologyDrawing
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SymbologyDrawing2D"/> class.
        /// </summary>
        public SymbologyDrawing2D()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SymbologyDrawing2D"/> class.
        /// </summary>
        /// <param name="type">The type of the new symbology drawing.</param>
        public SymbologyDrawing2D(TrueSymbologyType type)
            : base(type)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SymbologyDrawing2D"/> class.
        /// </summary>
        /// <param name="prototype">The existing SymbologyDrawing object to
        /// use as parameter prototype.</param>
        /// <param name="type">The new symbology drawing type.</param>
        public SymbologyDrawing2D(SymbologyDrawing prototype, TrueSymbologyType type)
            : base(prototype, type)
        {
        }

        /// <summary>
        /// Gets a value indicating whether a caption may be drawn.
        /// </summary>
        protected override bool CaptionMayBeDrawn
        {
            get
            {
                return (DrawCaption2D && !PreventCaptionDrawing);
            }
        }
    }
}
