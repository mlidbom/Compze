using Mono.Cecil;
using PostReleaseBindingCheck.Failures;
using PostReleaseBindingCheck.Packages;

namespace PostReleaseBindingCheck.Checks;

/// <summary>
/// Checks that every type and member one assembly references in another package of the <see cref="PostReleasePackageSet"/><br/>
/// exists there, with the signature the assembly was compiled against, and is accessible to the assembly.
/// </summary>
/// <remarks>
/// Cecil resolves a reference the way the runtime binds it: a type through the assembly's type forwarders too, and a method<br/>
/// or field on the type the reference names or any of that type's base classes, so moving a member to a base class is fine.
/// </remarks>
sealed class ReferenceCheck
{
   readonly PostReleasePackageSet _packageSet;
   readonly Package _referencingPackage;
   readonly AssemblyDefinition _referencingAssembly;
   readonly AssemblyAccess _access;

   public ReferenceCheck(PostReleasePackageSet packageSet, Package referencingPackage, AssemblyDefinition referencingAssembly)
   {
      _packageSet = packageSet;
      _referencingPackage = referencingPackage;
      _referencingAssembly = referencingAssembly;
      _access = new AssemblyAccess(referencingAssembly);
   }

   public IEnumerable<BindingFailure> FindFailures() => TypeReferenceFailures().Concat(MemberReferenceFailures());

   IEnumerable<BindingFailure> TypeReferenceFailures()
   {
      foreach(var typeReference in _referencingAssembly.MainModule.GetTypeReferences())
      {
         if(_packageSet.PackageReferencedBy(typeReference) is not { } referencedPackage) continue;

         var type = typeReference.Resolve();
         if(type == null)
            yield return Failure(referencedPackage, BindingFailureKind.MissingType, $"type {typeReference.FullName} is missing");
         else if(!_access.CanAccess(type))
            yield return Failure(referencedPackage, BindingFailureKind.InaccessibleType, $"type {type.FullName} is not accessible to {_referencingAssembly.Name.Name}");
      }
   }

   IEnumerable<BindingFailure> MemberReferenceFailures()
   {
      foreach(var memberReference in _referencingAssembly.MainModule.GetMemberReferences())
      {
         if(IsRuntimeProvidedArrayMethod(memberReference)) continue;
         if(_packageSet.PackageReferencedBy(memberReference.DeclaringType) is not { } referencedPackage) continue;
         if(IsOnAMissingType(memberReference)) continue;

         var failure = memberReference switch
         {
            MethodReference method => MethodFailure(method, referencedPackage),
            FieldReference field => FieldFailure(field, referencedPackage),
            _ => throw new InvalidOperationException($"A member reference that is neither a method nor a field: {memberReference.FullName}")
         };
         if(failure != null) yield return failure;
      }
   }

   /// <summary>An array type's <c>Get</c>, <c>Set</c> and <c>Address</c> methods are made by the runtime, not declared by the assembly of the element type.</summary>
   static bool IsRuntimeProvidedArrayMethod(MemberReference member) => member.DeclaringType is ArrayType;

   /// <summary>A member of a missing type: <see cref="TypeReferenceFailures"/> reports the type, once, rather than once per member used on it.</summary>
   static bool IsOnAMissingType(MemberReference member) => member.DeclaringType.Resolve() == null;

   BindingFailure? MethodFailure(MethodReference methodReference, Package referencedPackage)
   {
      var method = methodReference.Resolve();
      if(method == null)
         return Failure(referencedPackage, BindingFailureKind.MissingMember, $"{methodReference.FullName} is missing");
      return _access.CanAccess(method)
                ? null
                : Failure(referencedPackage, BindingFailureKind.InaccessibleMember, $"{method.FullName} is not accessible to {_referencingAssembly.Name.Name}");
   }

   BindingFailure? FieldFailure(FieldReference fieldReference, Package referencedPackage)
   {
      var field = fieldReference.Resolve();
      if(field == null)
         return Failure(referencedPackage, BindingFailureKind.MissingMember, $"{fieldReference.FullName} is missing");
      return _access.CanAccess(field)
                ? null
                : Failure(referencedPackage, BindingFailureKind.InaccessibleMember, $"{field.FullName} is not accessible to {_referencingAssembly.Name.Name}");
   }

   BindingFailure Failure(Package referencedPackage, BindingFailureKind kind, string explanation) => new(_referencingPackage, referencedPackage, kind, explanation);
}
