using System.Reflection;
using Compze.Core.Refactoring.Naming.Internal;
using Compze.Core.Tessaging.Teventive.Infrastructure.Validation;
using Compze.Core.Tessaging.Teventive.Public.Taggregates.BaseClasses.Public;
using Compze.Utilities.DependencyInjection;
using Compze.Utilities.DependencyInjection.Abstractions;
using Compze.Utilities.SystemCE.ReflectionCE;
using JetBrains.Annotations;

namespace Compze.Core.Tessaging.Teventive.Internal.Implementation;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface)]
public sealed class AllowPublicSettersAttribute : Attribute;

static class TaggregateTypeValidator<TDomainClass, TTeventImplementation, TTevent>
{
   public static void AssertStaticStructureIsValid()
   {
      var typesToInspect = EnumerableCE.OfTypes<TDomainClass, TTeventImplementation, TTevent>().ToList();

      typesToInspect.AddRange(GetAllInheritingClassesOrInterfaces(typeof(TDomainClass)));
      typesToInspect.AddRange(GetAllInheritingClassesOrInterfaces(typeof(TTeventImplementation)));
      typesToInspect.AddRange(GetAllInheritingClassesOrInterfaces(typeof(TTevent)));

      typesToInspect = typesToInspect.Distinct().ToList();

      var illegalMembers = typesToInspect.SelectMany(GetBrokenMembers).Distinct().ToList();

      if(illegalMembers.Any())
      {
         // ReSharper disable once PossibleNullReferenceException
         var brokenMembers = illegalMembers.Select(illegal => $"{illegal.DeclaringType?.FullName ?? "No declaring type or unnamed declaring type"}.{illegal.Name}").Distinct().OrderBy(me => me).Join(Environment.NewLine);
         var tessage = $"""
                        Types used by taggregate contains public setters or public  fields. This is a dangerous design. 
                        If you ever mutate an tevent or an taggregate except by raising tevents your state is likely to become corrupt in our caches etc. 
                        List of problem members:{Environment.NewLine}{brokenMembers}{Environment.NewLine}{Environment.NewLine}
                        """;

         throw new Exception(tessage);
      }
   }

   static IEnumerable<MemberInfo> GetBrokenMembers(Type type)
   {
      var publicFields = type.GetMembers(BindingFlags.Public | BindingFlags.Instance).Where(member => member.MemberType.HasFlag(MemberTypes.Field)).ToList();

      var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

      var publicProperties = properties
                            .Where(member => member.SetMethod?.IsPublic == true)
                            .ToList();

      var totalMutableProperties = publicFields.Concat(publicProperties).ToList();
      // ReSharper disable once AssignNullToNotNullAttribute
      // ReSharper disable once ConditionIsAlwaysTrueOrFalse
      totalMutableProperties = totalMutableProperties.Where(member => member.DeclaringType?.GetCustomAttribute<AllowPublicSettersAttribute>() == null).ToList();

      return totalMutableProperties;
   }

   static IReadOnlyList<Type> GetAllInheritingClassesOrInterfaces(Type type) => type.Assembly.GetTypes()
                                                                                    .Where(type.IsAssignableFrom)
                                                                                    .ToList();
}

[UsedImplicitly] public class TaggregateTypeValidator : ITaggregateTypeValidator
{
   public static void RegisterWith(IComponentRegistrar registrar)
      => registrar.Register(Singleton.For<ITaggregateTypeValidator>()
                                     .CreatedBy((ITypeMapper typeMapper) => new TaggregateTypeValidator(typeMapper)));

   readonly ITypeMapper _typeMapper;
   TaggregateTypeValidator(ITypeMapper typeMapper) => _typeMapper = typeMapper;

   public void AssertIsValid<TTaggregate>() => ValidatorFor<TTaggregate>.AssertValid(_typeMapper);

   static class ValidatorFor<TTaggregate>
   {
      // ReSharper disable once StaticMemberInGenericType (This is exactly the effect we are after...)
      static bool _validated;

      internal static void AssertValid(ITypeMapper typeMapper)
      {
         if(_validated) return;

         AssertValidInternal(typeMapper);

         _validated = true;
      }

      static void AssertValidInternal(ITypeMapper typeMapper)
      {
         var classInheritanceChain = typeof(TTaggregate).ClassInheritanceChain().ToList();
         var inheritedTaggregateType = classInheritanceChain.Single(baseClass => baseClass.IsConstructedGenericType && baseClass.GetGenericTypeDefinition() == typeof(Taggregate<,,,,>));

         var detectedTeventImplementationType = inheritedTaggregateType.GenericTypeArguments[1];
         var detectedTeventType = inheritedTaggregateType.GenericTypeArguments[2];

         var teventTypesToInspect = new List<Type> {detectedTeventType, detectedTeventImplementationType};

         teventTypesToInspect.AddRange(GetAllInheritingClassesOrInterfaces(detectedTeventImplementationType));
         teventTypesToInspect.AddRange(GetAllInheritingClassesOrInterfaces(detectedTeventType));

         teventTypesToInspect = teventTypesToInspect.Distinct().ToList();

         typeMapper.AssertMappingsExistFor(teventTypesToInspect.Append(typeof(TTaggregate)));

         TessageInspector.AssertValid(teventTypesToInspect);
      }

      static IReadOnlyList<Type> GetAllInheritingClassesOrInterfaces(Type type) => type.Assembly.GetTypes()
                                                                                       .Where(type.IsAssignableFrom)
                                                                                       .ToList();
   }
}
