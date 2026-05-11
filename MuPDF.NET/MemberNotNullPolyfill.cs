#if !NET5_0_OR_GREATER
// BCL exposes this only from .NET 5; net472/net48/netstandard2.0 need a local copy for nullable flow attributes.
#nullable disable
namespace System.Diagnostics.CodeAnalysis
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Property, Inherited = false)]
    internal sealed class MemberNotNullAttribute : Attribute
    {
        public MemberNotNullAttribute(string member) { }

        public MemberNotNullAttribute(params string[] members) { }
    }
}
#nullable restore
#endif
