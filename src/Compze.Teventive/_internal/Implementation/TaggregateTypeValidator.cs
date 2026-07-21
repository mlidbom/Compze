using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.SystemCE.ReflectionCE;
using Compze.Teventive.Taggregates.BaseClasses;
using Compze.TypeIdentifiers;
using JetBrains.Annotations;
using TessageTypeInspector = Compze.Tessaging.Validation._internal.TessageTypeInspector;

namespace Compze.Teventive._internal.Implementation;

[UsedImplicitly] class TaggregateTypeValidator : ITaggregateTypeValidator
{
   public static void RegisterWith(IComponentRegistrar registrar)
      => registrar.Register(Singleton.For<ITaggregateTypeValidator>()
                                     .CreatedBy((ITypeMap typeMap) => new TaggregateTypeValidator(typeMap)));

   readonly ITypeMap _typeMap;
   TaggregateTypeValidator(ITypeMap typeMap) => _typeMap = typeMap;

   public void AssertIsValid<TTaggregate>() => ValidatorFor<TTaggregate>.AssertValid(_typeMap);

   static class ValidatorFor<TTaggregate>
   {
      // ReSharper disable once StaticMemberInGenericType (This is exactly the effect we are after...)
      static bool _validated;

      internal static void AssertValid(ITypeMap typeMap)
      {
         if(_validated) return;

         AssertValidInternal(typeMap);

         _validated = true;
      }

      static void AssertValidInternal(ITypeMap typeMap)
      {
         var classInheritanceChain = typeof(TTaggregate).ClassInheritanceChain().ToList();
         var inheritedTaggregateType = classInheritanceChain.Single(baseClass => baseClass.IsConstructedGenericType && baseClass.GetGenericTypeDefinition() == typeof(Taggregate<,,,,>));

         var detectedTeventImplementationType = inheritedTaggregateType.GenericTypeArguments[1];
         var detectedTeventType = inheritedTaggregateType.GenericTypeArguments[2];

         var teventTypesToInspect = new List<Type> {detectedTeventType, detectedTeventImplementationType};

         teventTypesToInspect.AddRange(GetAllInheritingClassesOrInterfaces(detectedTeventImplementationType));
         teventTypesToInspect.AddRange(GetAllInheritingClassesOrInterfaces(detectedTeventType));

         teventTypesToInspect = teventTypesToInspect.Distinct().ToList();

         typeMap.AssertMappingsExistFor(teventTypesToInspect.Append(typeof(TTaggregate)));

         foreach(var teventType in teventTypesToInspect) TessageTypeInspector.AssertValid(teventType);
      }

      static IReadOnlyList<Type> GetAllInheritingClassesOrInterfaces(Type type) => type.Assembly.GetTypes()
                                                                                       .Where(type.IsAssignableFrom)
                                                                                       .ToList();
   }
}
