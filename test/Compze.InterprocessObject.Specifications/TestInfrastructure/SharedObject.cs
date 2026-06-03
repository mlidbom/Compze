using JetBrains.Annotations;
using MemoryPack;

namespace Compze.InterprocessObject.Specifications.TestInfrastructure;

[MemoryPackable]
[UsedImplicitly] public partial class SharedObject
{
   // ReSharper disable once MemberCanBeInternal
   public string Name { get; set; } = "Default";
}
