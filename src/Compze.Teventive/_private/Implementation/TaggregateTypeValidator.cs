using System.Reflection;

namespace Compze.Teventive._private.Implementation;

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

      return publicFields.Concat(publicProperties).ToList();
   }

   static IReadOnlyList<Type> GetAllInheritingClassesOrInterfaces(Type type) => type.Assembly.GetTypes()
                                                                                    .Where(type.IsAssignableFrom)
                                                                                    .ToList();
}
