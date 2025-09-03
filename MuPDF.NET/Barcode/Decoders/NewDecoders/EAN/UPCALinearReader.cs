using System;
using BarcodeReader.Core.Common;

namespace BarcodeReader.Core.EAN
{
    /// <summary>
    /// UPC-A reader.
    /// Also it can read supplemented EAN2 and EAN5.
    /// </summary>
#if CORE_DEV
    public
#else
    internal
#endif
    class UPCALinearReader : EAN13LinearReader
    {
        public override SymbologyType GetBarCodeType()
        {
            return SymbologyType.UPCA;
        }

        protected override FoundBarcode FindBarcode(Candidate cand, bool reverse)
        {
            var res = base.FindBarcode(cand, reverse);

            if (res != null)
            {
                var barData = res.ParentRegion.Data[0] as NumericBarCodeData;
                var data = barData.Value;

                // If it's EAN-13 with leading zero then convert it to UPCA.
                // Otherwise return null to avoid false positive.
                if (data.Length == 13)
                {
                    if (data[0] == 0)
                    {
                        for (int i = 0; i < data.Length - 1; i++)
                            data[i] = data[i + 1];
                        Array.Resize(ref data, data.Length - 1);
                        barData.Value = data;

                        res.Value = barData.ToString();
                    }
                    else
                        return null;
                }
            }

            return res;
        }
    }
}