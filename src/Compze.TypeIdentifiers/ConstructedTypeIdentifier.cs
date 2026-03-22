namespace Compze.TypeIdentifiers;

/// <summary>
/// A composite type where at least one component is mapped.
/// The string representation is an <c>AssemblyQualifiedName</c>-format string
/// with <c>GUID, 0</c> in place of mapped components.
/// </summary>
sealed class ConstructedTypeIdentifier : TypeIdentifier
{
   public override string StringRepresentation { get; }

   public ConstructedTypeIdentifier(string structuralString) => StringRepresentation = structuralString;
}
