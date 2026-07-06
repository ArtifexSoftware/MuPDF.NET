using System.Reflection;
using MuPDF.NET;
using Xunit.Sdk;

namespace MuPDF.NET.Test
{
    /// <summary>
    /// Clears stored MuPDF warnings before each test so assertions see only the current test's output.
    /// </summary>
    /// <remarks>warning drain.</remarks>
    [AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public sealed class ResetMupdfWarningsAttribute : BeforeAfterTestAttribute
    {
        public override void Before(MethodInfo methodUnderTest) => Tools.MupdfWarnings();
    }
}