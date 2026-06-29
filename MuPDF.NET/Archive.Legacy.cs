namespace MuPDF.NET
{
    /// <summary>
    /// Legacy MuPDF.NET API notes for <see cref="Archive"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The readthedocs constructors map to the unified constructor as follows:
    /// </para>
    /// <list type="table">
    /// <listheader><term>Legacy</term><description>C# equivalent</description></listheader>
    /// <item><term><c>Archive()</c></term><description><c>new Archive()</c></description></item>
    /// <item><term><c>Archive(dirname, path)</c></term><description><c>new Archive(dirname, path)</c> (two-argument form)</description></item>
    /// <item><term><c>Archive(data, name)</c></term><description><c>new Archive(data, name)</c> for <see cref="byte"/>[]</description></item>
    /// </list>
    /// <para>
    /// See <see href="https://mupdfnet.readthedocs.io/en/latest/classes/Archive.html"/>.
    /// </para>
    /// </remarks>
    public partial class Archive
    {
    }
}
