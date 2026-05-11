using System;

namespace MuPDF.NET
{
    /// <summary>
    /// Exception for invalid or corrupted file data.
    /// </summary>
    public class FileDataException : Exception
    {
        public FileDataException() { }
        public FileDataException(string message) : base(message) { }
        public FileDataException(string message, Exception inner) : base(message, inner) { }
    }

    /// <summary>
    /// Exception for empty file or stream.
    /// </summary>
    public class EmptyFileException : FileDataException
    {
        public EmptyFileException() { }
        public EmptyFileException(string message) : base(message) { }
        public EmptyFileException(string message, Exception inner) : base(message, inner) { }
    }

    /// <summary>
    /// Legacy exception type. PyMuPDF-compatible closed-document errors use
    /// <see cref="ValueErrorException"/> with message <c>document closed</c> (see <c>Document.page_count</c> / <c>xref_object</c> in Python).
    /// </summary>
    public class DocumentClosedException : InvalidOperationException
    {
        public DocumentClosedException() : base("document closed") { }
        public DocumentClosedException(string message) : base(message) { }
    }

    /// <summary>
    /// Same situations as Python <c>ValueError</c> in the PyMuPDF API (e.g. bad rectangle).
    /// </summary>
    public class ValueErrorException : ArgumentException
    {
        public ValueErrorException(string message) : base(message) { }
        public ValueErrorException(string message, Exception innerException) : base(message, innerException) { }
    }

    /// <summary>
    /// Python-name alias for <see cref="FileDataException"/> (`FileDataError` in PyMuPDF).
    /// </summary>
    public class FileDataError : FileDataException
    {
        public FileDataError() { }
        public FileDataError(string message) : base(message) { }
        public FileDataError(string message, Exception inner) : base(message, inner) { }
    }

    /// <summary>
    /// Python-name alias for missing file situations (`FileNotFoundError` in PyMuPDF).
    /// </summary>
    public class FileNotFoundError : FileDataError
    {
        public FileNotFoundError() { }
        public FileNotFoundError(string message) : base(message) { }
        public FileNotFoundError(string message, Exception inner) : base(message, inner) { }
    }

    /// <summary>
    /// Python-name alias for <see cref="EmptyFileException"/> (`EmptyFileError` in PyMuPDF).
    /// </summary>
    public class EmptyFileError : EmptyFileException
    {
        public EmptyFileError() { }
        public EmptyFileError(string message) : base(message) { }
        public EmptyFileError(string message, Exception inner) : base(message, inner) { }
    }
}
