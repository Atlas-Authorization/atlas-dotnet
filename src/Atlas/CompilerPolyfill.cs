// Polyfills so record `init` accessors compile on netstandard2.1, where the
// framework does not ship `System.Runtime.CompilerServices.IsExternalInit`.
#if !NET5_0_OR_GREATER
namespace System.Runtime.CompilerServices
{
    using System.ComponentModel;

    /// <summary>Reserved for the compiler; enables C# 9 <c>init</c> accessors on older targets.</summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    internal static class IsExternalInit { }
}
#endif
