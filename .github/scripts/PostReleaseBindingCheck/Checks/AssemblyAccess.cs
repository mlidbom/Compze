using Mono.Cecil;

namespace PostReleaseBindingCheck.Checks;

/// <summary>
/// What one assembly may use of another's types and members at runtime: whatever is public, whatever is protected (the check<br/>
/// does not follow which of the assembly's types derives from which, so it takes protected as accessible), and whatever is<br/>
/// internal in an assembly that grants this one <c>InternalsVisibleTo</c>, as Compze's libraries do to each other.
/// </summary>
sealed class AssemblyAccess
{
   const string InternalsVisibleToAttribute = "System.Runtime.CompilerServices.InternalsVisibleToAttribute";

   readonly AssemblyDefinition _accessingAssembly;

   public AssemblyAccess(AssemblyDefinition accessingAssembly) => _accessingAssembly = accessingAssembly;

   public bool CanAccess(TypeDefinition type) => IsVisible(type) && (type.DeclaringType == null || CanAccess(type.DeclaringType));

   public bool CanAccess(MethodDefinition method) => (method.Attributes & MethodAttributes.MemberAccessMask) switch
   {
      MethodAttributes.Public or MethodAttributes.Family or MethodAttributes.FamORAssem => true,
      MethodAttributes.Assembly or MethodAttributes.FamANDAssem => SeesTheInternalsOf(method.Module.Assembly),
      _ => false
   };

   public bool CanAccess(FieldDefinition field) => (field.Attributes & FieldAttributes.FieldAccessMask) switch
   {
      FieldAttributes.Public or FieldAttributes.Family or FieldAttributes.FamORAssem => true,
      FieldAttributes.Assembly or FieldAttributes.FamANDAssem => SeesTheInternalsOf(field.Module.Assembly),
      _ => false
   };

   bool IsVisible(TypeDefinition type) => (type.Attributes & TypeAttributes.VisibilityMask) switch
   {
      TypeAttributes.Public or TypeAttributes.NestedPublic or TypeAttributes.NestedFamily or TypeAttributes.NestedFamORAssem => true,
      TypeAttributes.NotPublic or TypeAttributes.NestedAssembly or TypeAttributes.NestedFamANDAssem => SeesTheInternalsOf(type.Module.Assembly),
      _ => false
   };

   bool SeesTheInternalsOf(AssemblyDefinition assembly) =>
      assembly.CustomAttributes.Where(attribute => attribute.AttributeType.FullName == InternalsVisibleToAttribute)
              .Select(attribute => (string)attribute.ConstructorArguments[0].Value)
              .Any(grantee => string.Equals(GranteeAssemblyName(grantee), _accessingAssembly.Name.Name, StringComparison.OrdinalIgnoreCase));

   /// <summary>An <c>InternalsVisibleTo</c> grantee may carry a public key after the name: <c>Compze.Hosting, PublicKey=0024...</c>.</summary>
   static string GranteeAssemblyName(string grantee) => grantee.Split(',')[0].Trim();
}
