using System;

namespace CustomCompilerServices
{
    [AttributeUsage(AttributeTargets.Interface | AttributeTargets.Class)]
    public sealed class RequireSealedImplementationAttribute : Attribute { }
}
