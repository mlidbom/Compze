using System.Diagnostics.CodeAnalysis;
using Mono.Cecil;
using PostReleaseBindingCheck.Failures;
using PostReleaseBindingCheck.Packages;

namespace PostReleaseBindingCheck.Checks;

/// <summary>
/// Checks that the runtime can still load every type of one assembly, given what the classes it derives from and the interfaces<br/>
/// it implements have become in the other packages of the <see cref="PostReleasePackageSet"/>: none of those classes is sealed,<br/>
/// and a concrete type implements every abstract member of them, including any they gained after the assembly was compiled.
/// </summary>
/// <remarks>
/// The check sees only what the set holds: an interface or base class from outside it, such as <see cref="IDisposable"/>,<br/>
/// is left alone, and so is everything inherited through it.
/// </remarks>
sealed class TypeLoadCheck
{
   readonly PostReleasePackageSet _packageSet;
   readonly Package _referencingPackage;
   readonly AssemblyDefinition _assembly;

   public TypeLoadCheck(PostReleasePackageSet packageSet, Package referencingPackage, AssemblyDefinition assembly)
   {
      _packageSet = packageSet;
      _referencingPackage = referencingPackage;
      _assembly = assembly;
   }

   public IEnumerable<BindingFailure> FindFailures() =>
      _assembly.MainModule.GetTypes().SelectMany(type => SealedBaseClassFailures(type).Concat(IsConcrete(type) ? UnimplementedAbstractMemberFailures(type) : []));

   IEnumerable<BindingFailure> SealedBaseClassFailures(TypeDefinition type)
   {
      if(type.BaseType?.Resolve() is { IsSealed: true } baseClass && IsInAnotherPackage(baseClass, out var basePackage))
         yield return Failure(basePackage, BindingFailureKind.SealedBaseClass, $"{type.FullName} derives from {baseClass.FullName}, which is sealed");
   }

   IEnumerable<BindingFailure> UnimplementedAbstractMemberFailures(TypeDefinition concreteType)
   {
      var classes = TypeInstantiation.Of(concreteType).WithBaseClasses();
      var interfaces = classes.SelectMany(type => type.AllInterfaces()).DistinctBy(implementedInterface => implementedInterface.Name).ToList();
      return UnimplementedInterfaceMemberFailures(concreteType, classes, interfaces)
        .Concat(UnimplementedBaseClassMemberFailures(concreteType, classes));
   }

   IEnumerable<BindingFailure> UnimplementedInterfaceMemberFailures(TypeDefinition concreteType, IReadOnlyList<TypeInstantiation> classes, IReadOnlyList<TypeInstantiation> interfaces)
   {
      foreach(var implementedInterface in interfaces)
      {
         if(!IsInAnotherPackage(implementedInterface.Definition, out var interfacePackage)) continue;
         foreach(var abstractMethod in implementedInterface.Definition.Methods.Where(method => method.IsAbstract))
         {
            if(IsImplemented(abstractMethod, implementedInterface, classes, interfaces)) continue;
            yield return Failure(interfacePackage, BindingFailureKind.UnimplementedAbstractMember,
                                 $"{concreteType.FullName} does not implement {implementedInterface.Name}::{abstractMethod.Name}, {implementedInterface.SignatureOf(abstractMethod)}");
         }
      }
   }

   IEnumerable<BindingFailure> UnimplementedBaseClassMemberFailures(TypeDefinition concreteType, IReadOnlyList<TypeInstantiation> classes)
   {
      for(var index = 1; index < classes.Count; index++)
      {
         var baseClass = classes[index];
         if(!IsInAnotherPackage(baseClass.Definition, out var basePackage)) continue;
         var moreDerivedClasses = classes.Take(index).ToList();
         foreach(var abstractMethod in baseClass.Definition.Methods.Where(method => method.IsAbstract))
         {
            if(IsOverridden(abstractMethod, baseClass, moreDerivedClasses)) continue;
            yield return Failure(basePackage, BindingFailureKind.UnimplementedAbstractMember,
                                 $"{concreteType.FullName} does not override {baseClass.Name}::{abstractMethod.Name}, {baseClass.SignatureOf(abstractMethod)}");
         }
      }
   }

   /// <summary>
   /// Whether one of <paramref name="classes"/> implements <paramref name="abstractMethod"/>: explicitly, or by a method of the same name<br/>
   /// and signature. Or whether one of <paramref name="interfaces"/> does, with a default implementation in an interface derived from it.
   /// </summary>
   static bool IsImplemented(MethodDefinition abstractMethod, TypeInstantiation declaringInterface, IReadOnlyList<TypeInstantiation> classes, IReadOnlyList<TypeInstantiation> interfaces)
   {
      var signature = declaringInterface.SignatureOf(abstractMethod);
      return classes.Any(type => type.Definition.Methods.Any(method => IsExplicitOverrideOf(method, abstractMethod)
                                                                    || method.Name == abstractMethod.Name && type.SignatureOf(method) == signature))
          || interfaces.Any(type => type.Definition.Methods.Any(method => IsExplicitOverrideOf(method, abstractMethod)));
   }

   /// <summary>Whether one of <paramref name="moreDerivedClasses"/> overrides <paramref name="abstractMethod"/> with a method that is not abstract itself.</summary>
   static bool IsOverridden(MethodDefinition abstractMethod, TypeInstantiation baseClass, IReadOnlyList<TypeInstantiation> moreDerivedClasses)
   {
      var signature = baseClass.SignatureOf(abstractMethod);
      return moreDerivedClasses.Any(type => type.Definition.Methods.Any(method => !method.IsAbstract
                                                                               && (IsExplicitOverrideOf(method, abstractMethod) || IsImplicitOverride(method, type))));

      bool IsImplicitOverride(MethodDefinition method, TypeInstantiation type) =>
         method.Name == abstractMethod.Name && method is { IsVirtual: true, IsNewSlot: false } && type.SignatureOf(method) == signature;
   }

   /// <summary>Whether <paramref name="method"/> names <paramref name="abstractMethod"/> as the method it overrides or implements, as an explicit interface implementation does.</summary>
   static bool IsExplicitOverrideOf(MethodDefinition method, MethodDefinition abstractMethod) =>
      method.HasOverrides && method.Overrides.Any(overridden => overridden.Resolve() == abstractMethod);

   static bool IsConcrete(TypeDefinition type) => type is { IsInterface: false, IsAbstract: false };

   bool IsInAnotherPackage(TypeDefinition type, [NotNullWhen(true)] out Package? package)
   {
      var containingPackage = _packageSet.PackageContaining(type);
      package = containingPackage == _referencingPackage ? null : containingPackage;
      return package != null;
   }

   BindingFailure Failure(Package referencedPackage, BindingFailureKind kind, string explanation) => new(_referencingPackage, referencedPackage, kind, explanation);
}
