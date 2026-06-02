using System;

namespace MuPDF.NET
{
    /// <summary>
    /// Raised for documents with file structure issues.
    /// <para>Ports PyMuPDF <c>FileDataError</c> (<c>src/__init__.py</c>).</para>
    /// </summary>
    public class FileDataException : Exception
    {
        public FileDataException() { }

        public FileDataException(string message) : base(message) { }

        public FileDataException(string message, Exception inner) : base(message, inner) { }
    }

    /// <summary>
    /// Raised if a file does not exist.
    /// <para>Ports PyMuPDF <c>FileNotFoundError</c> (<c>src/__init__.py</c>).</para>
    /// <para>Not the same as <see cref="System.IO.FileNotFoundException"/>; PyMuPDF defines this as a separate <c>RuntimeError</c> subclass.</para>
    /// </summary>
    public class FileNotFoundError : Exception
    {
        public FileNotFoundError() { }

        public FileNotFoundError(string message) : base(message) { }

        public FileNotFoundError(string message, Exception inner) : base(message, inner) { }
    }

    /// <summary>Internal alias for same-assembly throw sites.</summary>
    internal class FileNotFoundException : FileNotFoundError
    {
        public FileNotFoundException() { }
        public FileNotFoundException(string message) : base(message) { }
        public FileNotFoundException(string message, Exception inner) : base(message, inner) { }
    }

    /// <summary>
    /// Raised when creating documents from zero-length data.
    /// <para>Ports PyMuPDF <c>EmptyFileError</c> (<c>src/__init__.py</c>), a subclass of <c>FileDataError</c>.</para>
    /// </summary>
    public class EmptyFileException : FileDataException
    {
        public EmptyFileException() { }

        public EmptyFileException(string message) : base(message) { }

        public EmptyFileException(string message, Exception inner) : base(message, inner) { }
    }

    /// <summary>
    /// Operation on a closed document.
    /// <para>PyMuPDF often raises <c>ValueError</c> with message <c>document closed</c>; this type is available when a dedicated exception is preferred.</para>
    /// </summary>
    public class DocumentClosedException : InvalidOperationException
    {
        public DocumentClosedException() : base("document closed") { }

        public DocumentClosedException(string message) : base(message) { }
    }

    /// <summary>
    /// Invalid argument in the PyMuPDF API sense (Python <c>ValueError</c>).
    /// <para>Used for bad rectangles, wrong types, closed/encrypted documents, etc.</para>
    /// </summary>
    public class ValueErrorException : ArgumentException
    {
        public ValueErrorException(string message) : base(message) { }

        public ValueErrorException(string message, Exception innerException) : base(message, innerException) { }
    }

    // ─── PyMuPDF exception type names (internal, same assembly) ─────────

    /// <summary>PyMuPDF <c>FileDataError</c> name alias for ported throw sites.</summary>
    internal class FileDataError : FileDataException
    {
        public FileDataError() { }
        public FileDataError(string message) : base(message) { }
        public FileDataError(string message, Exception inner) : base(message, inner) { }
    }

    /// <summary>PyMuPDF <c>EmptyFileError</c> name alias.</summary>
    internal class EmptyFileError : EmptyFileException
    {
        public EmptyFileError() { }
        public EmptyFileError(string message) : base(message) { }
        public EmptyFileError(string message, Exception inner) : base(message, inner) { }
    }
}
