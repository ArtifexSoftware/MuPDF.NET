using System;

namespace MuPDF.NET
{
    /// <summary>
    /// Raised for documents with file structure issues.
    /// </summary>
    public class FileDataException : Exception
    {
        public FileDataException() { }

        public FileDataException(string message) : base(message) { }

        public FileDataException(string message, Exception inner) : base(message, inner) { }
    }

    /// <summary>
    /// Raised if a file does not exist.
    /// </summary>
    /// <para>Not the same as <see cref="System.IO.FileNotFoundException"/>; MuPDF defines this as a separate <c>RuntimeError</c> subclass.</para>
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
    /// </summary>
    /// <para>Subclass of <see cref="FileDataException"/>.</para>
    public class EmptyFileException : FileDataException
    {
        public EmptyFileException() { }

        public EmptyFileException(string message) : base(message) { }

        public EmptyFileException(string message, Exception inner) : base(message, inner) { }
    }

    /// <summary>
    /// Operation on a closed document.
    /// </summary>
    /// <para>MuPDF often raises <c>ValueError</c> with message <c>document closed</c>; this type is available when a dedicated exception is preferred.</para>
    public class DocumentClosedException : InvalidOperationException
    {
        public DocumentClosedException() : base("document closed") { }

        public DocumentClosedException(string message) : base(message) { }
    }

    /// <summary>
    /// Invalid argument in the MuPDF API sense (<c>ValueError</c>).
    /// </summary>
    /// <para>Used for bad rectangles, wrong types, closed/encrypted documents, etc.</para>
    public class ValueErrorException : ArgumentException
    {
        public ValueErrorException(string message) : base(message) { }

        public ValueErrorException(string message, Exception innerException) : base(message, innerException) { }
    }

    // ─── MuPDF exception type names (internal, same assembly) ─────────

    /// <summary>Internal exception alias.</summary>
    internal class FileDataError : FileDataException
    {
        public FileDataError() { }
        public FileDataError(string message) : base(message) { }
        public FileDataError(string message, Exception inner) : base(message, inner) { }
    }

    /// <summary>Internal exception alias.</summary>
    internal class EmptyFileError : EmptyFileException
    {
        public EmptyFileError() { }
        public EmptyFileError(string message) : base(message) { }
        public EmptyFileError(string message, Exception inner) : base(message, inner) { }
    }
}