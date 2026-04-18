using System.Text.Json;
using System.Text.Json.Serialization;

namespace Demo
{
    /// <summary>
    /// GitHub samples entry point. With no arguments, all samples run; see <see cref="SampleMenu"/>.
    /// </summary>
    internal partial class Program
    {
        private static void Main(string[] args)
        {
            SampleMenu.Run(args);
        }
    }
}
