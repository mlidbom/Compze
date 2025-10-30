namespace Compze.Core.Public;

public static class ObsoleteMessage
{
    public const string ForInternalUseOnly = @"
This member breaks encapsulation. 
It is for building infrastructure code, usually within the framework itself.
Be VERY sure you know what you are doing before you invoke this method from application code.
";
}