namespace Compze;

static class ObsoleteMessage
{
    internal const string ForInternalUseOnly = @"
This methods breaks encapsulation. 
The interface it is in should always be implemented using explicit interface implementation so that it is hidden from normal use of instances.
It is for building infrastructure code, usually within the framework itself.
Be VERY sure you know what you are doing before you invoke this method from application code.
";
}
