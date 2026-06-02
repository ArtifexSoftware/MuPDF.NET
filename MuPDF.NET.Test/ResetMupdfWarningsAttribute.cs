using System.Reflection;
using MuPDF.NET;
using Xunit.Sdk;

namespace MuPDF.NET.Test
{
    /// <summary>
    /// PyMuPDF <c>tests/conftest.py</c> — drain/clear <see cref="Tools.MupdfWarnings"/> before each test
    /// so warning assertions see only the current test's MuPDF output.
    /// </summary>
    [AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public sealed class ResetMupdfWarningsAttribute : BeforeAfterTestAttribute
    {
        public override void Before(MethodInfo methodUnderTest) => Tools.MupdfWarnings();
    }
}
