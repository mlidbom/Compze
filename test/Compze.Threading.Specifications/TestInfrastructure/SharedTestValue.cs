using MemoryPack;

namespace Compze.Threading.Specifications.TestInfrastructure;

[MemoryPackable]
partial class SharedTestValue
{
   public int Value { get; set; }
   public List<int> Items { get; set; } = [];
}
