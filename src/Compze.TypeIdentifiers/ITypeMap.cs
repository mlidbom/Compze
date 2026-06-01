using System.Diagnostics.CodeAnalysis;

namespace Compze.TypeIdentifiers;

/// <summary>
/// Converts between .NET <see cref="Type"/> objects and their canonical <see cref="TypeId"/> identities,
/// and resolves persisted <c>$type</c> strings back to identities. The sole factory for <see cref="TypeId"/>.
/// </summary>
public interface ITypeMap
{
   /// <summary>Returns the canonical <see cref="TypeId"/> for any type — leaf, constructed, or stable.</summary>
   TypeId GetId(Type type);

   /// <summary>
   /// Resolves a persisted canonical <c>$type</c> string to its canonical <see cref="TypeId"/>. Read
   /// <see cref="TypeId.Type"/> for the resolved .NET type and <see cref="TypeId.CanonicalString"/> for the
   /// persisted form. Throws if the string resolves to a type that is neither mapped nor registered stable.
   /// </summary>
   TypeId GetId(string persistedTypeString);

   /// <summary>
   /// Returns the canonical <see cref="TypeId"/> for <paramref name="type"/>, or <c>false</c> if it is neither
   /// mapped nor resolvable as a stable type — i.e. when <see cref="GetId(Type)"/> would throw.
   /// </summary>
   bool TryGetId(Type type, [NotNullWhen(true)] out TypeId? id);

   /// <summary>Throws if any of the given types lack a mapping.</summary>
   void AssertMappingsExistFor(IEnumerable<Type> types);
}
