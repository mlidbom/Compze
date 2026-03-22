namespace Compze.TypeIdentifiers;

/// <summary>
/// A type — leaf or composite — where every component comes from a stable assembly.
/// The string representation is the unmodified <c>AssemblyQualifiedName</c>.
/// Resolution: pass directly to <c>Type.GetType()</c>.
/// </summary>
sealed class StableNameTypeIdentifier : TypeIdentifier
{
   public override string StringRepresentation { get; }

   public StableNameTypeIdentifier(string assemblyQualifiedName) => StringRepresentation = assemblyQualifiedName;
}
