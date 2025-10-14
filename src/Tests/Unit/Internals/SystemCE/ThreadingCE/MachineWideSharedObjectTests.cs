using JetBrains.Annotations;

namespace Compze.Tests.Unit.Internals.SystemCE.ThreadingCE;

// Helper class for Performance.Internals tests
// The actual tests have been migrated to Unit.Internals.XUnit
[UsedImplicitly] public class SharedObject
{
   public string Name { get; set; } = "Default";
}
