namespace Compze.TypeIdentifiers.Interning;

/// <summary>
/// A point-in-time read of the type-identity tables, used to warm the interner's in-memory model: the
/// conceptual ids with their current names, and every spelling mapped to its id.
/// </summary>
public sealed class InternerSnapshot(
   IReadOnlyList<(int Id, string CurrentName)> types,
   IReadOnlyList<(int TypeId, string TypeString)> spellings)
{
   public IReadOnlyList<(int Id, string CurrentName)> Types { get; } = types;
   public IReadOnlyList<(int TypeId, string TypeString)> Spellings { get; } = spellings;
}
