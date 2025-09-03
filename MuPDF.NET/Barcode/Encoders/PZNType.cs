/**************************************************
 Bytescout BarCode SDK
 Copyright (c) 2008 - 2010 Bytescout
 All rights reserved
 www.bytescout.com
**************************************************/

using System;
using System.Text;

namespace BarcodeWriter.Core
{
    /// <summary>
	/// Specifies the type of PZN symbology.
    /// </summary>
    public enum PZNType
    {
        /// <summary>
		/// The original PZN barcode with 6 significant digits. PZN7 is obsolete since 2013-01-01, but can be used up to 2020-01-01.
        /// </summary>
        PZN7 = 0,

        /// <summary>
		/// New PZN8 standard with 7 significant digits. Supercedes PZN7 since 2013-01-01. 
		/// Default. 
        /// </summary>
        PZN8 = 1
    }
}