using System;
using System.Diagnostics;

namespace BarcodeReader.Core
{
    /// <summary>
    /// Timeout exception thrown on timeout
    /// </summary>
#if CORE_DEV
    public
#else
    internal
#endif
    class TimeOutException : Exception { };
    /// <summary>
    /// 2D decoder timeout exception
    /// </summary>
#if CORE_DEV
    public
#else
    internal
#endif
    class SymbologyReader2DTimeOutException : TimeOutException { };

#if CORE_DEV
    public
#else
    internal
#endif
    abstract class SymbologyReader2D : IBarcodeDecoder
	{
	    /// <summary>
	    /// Threshold filter method to use by default
	    /// </summary>
	    public ThresholdFilterMethod ThresholdFilterMethodToUse { get; set;} = ThresholdFilterMethod.Block;

	    public BlackAndWhiteImage BWImage { get; private set; }
        public int ScanStep { get; set; } = 1;
        public int MinAllowedBarcodeSideSize { get; set; } = 0;
        public int MaxAllowedBarcodeSideSize { get; set; } = 100;
        public int ExpectedNumberOfBarcodes { get; set; } = 0;
        public bool StopOnFirstFoundBarcodeInTheRow { get; set; } = false;
	    protected System.Text.Encoding DefaultEncoding => System.Text.Encoding.GetEncoding(28591); //default is iso-8859-1 (8-bit encoding)
        public System.Text.Encoding Encoding { get; set; } = System.Text.Encoding.GetEncoding(28591);
        public abstract SymbologyType GetBarCodeType();

	    protected abstract FoundBarcode[] DecodeBarcode();

        public virtual FoundBarcode[] Decode(BlackAndWhiteImage bwImage)
        {
            BWImage = bwImage;
            return DecodeBarcode();
		}

		public virtual void Dispose() { }

        /// <summary>
        /// Skips the two modules.
        /// NOTE: we skip two modules in order to retain start module color.
        /// </summary>
        /// <param name="offset">The offset.</param>
        /// <param name="moduleWidths">The module widths.</param>
        protected void SkipTwoModules(ref int offset, int[] moduleWidths)
        {
            offset += moduleWidths[0] + moduleWidths[1];

            for (int i = 2; i < moduleWidths.Length; i++)
                moduleWidths[i - 2] = moduleWidths[i];

            moduleWidths[moduleWidths.Length - 2] = 0;
            moduleWidths[moduleWidths.Length - 1] = 0;
        }

        /// <summary>
        /// Timeout (in ticks) to abort decoding if exceeded
        /// 0 means no timeout is checked
        /// stores max allowed time, when the current time is greater 
        /// then we should throw the exception
        /// </summary>
        public long TimeoutTimeInTicks = 0;

	    protected bool IsTimeout()
        {
            if (TimeoutTimeInTicks == 0)
                return false;

            long curTicks = DateTime.Now.Ticks;

            // else check the current time against max end time
            if (TimeoutTimeInTicks < curTicks)
            {
                Debug.WriteLine(string.Format("Timeout: exceeded by {0} mseconds", (curTicks - TimeoutTimeInTicks) * TimeSpan.TicksPerMillisecond));
                return true;
            }

            return false;
        }

        public override string ToString()
        {
            return GetType().Name;
        }
    }
}
