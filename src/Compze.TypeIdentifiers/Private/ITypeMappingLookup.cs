using Compze.TypeIdentifiers.Internal;

namespace Compze.TypeIdentifiers.Private;

/// <summary>
/// Provides the lookup operations that <see cref="TypeIdentifier"/> subtypes need to resolve themselves
/// to .NET <see cref="Type"/> instances and to transform themselves into persisted form.
/// </summary>
interface ITypeMappingLookup
{
   Type GetLeafType(Guid guid);
   Type GetOpenGenericType(Guid guid);
   bool TryGetLeafTypeGuid(Type type, out Guid guid);
   bool TryGetOpenGenericGuid(Type type, out Guid guid);
   bool IsStableType(Type type);
}
