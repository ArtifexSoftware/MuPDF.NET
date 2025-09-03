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
    /// Represents errors that occur during BarcodeWriter.Core class library execution.
    /// </summary>
    /// <example>
    /// The following example demonstrates usage of <see cref="BarcodeWriter.Core.BarcodeException"/> class.
    /// <include file='samples.xml' path='samples/sample[@id="barcodeexception"]/*' />
    /// </example>
    public class BarcodeException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BarcodeException"/> class with a specified error message.
        /// </summary>
        /// <param name="message">The exception message that describes the error.</param>
        public BarcodeException(string message) : base(message)
        {
        }

	    /// <summary>
	    /// Initializes a new instance of the <see cref="BarcodeException"/> class with a specified error message and inner exception.
	    /// </summary>
	    /// <param name="message">The exception message that describes the error.</param>
	    /// <param name="internalException">The Inner exception</param>
	    public BarcodeException(string message, Exception internalException) : base(message, internalException)
		{
		}
    }
}
