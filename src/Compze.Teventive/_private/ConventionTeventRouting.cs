using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using Compze.Tessaging.TessageTypes;

namespace Compze.Teventive._private;

///<summary>Builds a class's <see cref="TeventRouting"/> by convention, once per class: its <c>Apply</c> methods are its tevent appliers, its <c>Handle</c><br/>
/// methods its tevent handlers, and every field that holds an <see cref="ITeventRoutedMember{TTevent}"/> a part its routing routes to.</summary>
///<remarks>A prototype (taggregate-experiments-opus-convention), the alternative to declaring the routing explicitly. A class takes part by carrying<br/>
/// <see cref="TeventRoutingByConventionAttribute"/>, which a final design would not need: the convention would simply be how routing works.</remarks>
static class ConventionTeventRouting
{
   const string ApplierMethodName = "Apply";
   const string HandlerMethodName = "Handle";

   static readonly ConcurrentDictionary<Type, TeventRouting?> AppliersByClass = new();
   static readonly ConcurrentDictionary<Type, TeventRouting?> HandlersByClass = new();

   internal static TeventRouting? AppliersOf(Type type) => AppliersByClass.GetOrAdd(type, static it => Build(it, toTeventHandlers: false));
   internal static TeventRouting? HandlersOf(Type type) => HandlersByClass.GetOrAdd(type, static it => Build(it, toTeventHandlers: true));

   static TeventRouting? Build(Type type, bool toTeventHandlers)
   {
      if(!type.IsDefined(typeof(TeventRoutingByConventionAttribute), inherit: true)) return null;

      var routes = ClassesFromTheRootTo(type).SelectMany(declaringClass => RoutesDeclaredIn(declaringClass, toTeventHandlers)).ToList();
      return TeventRouting<object>.FromRoutes(routes, toTeventHandlers);
   }

   ///<summary>The classes of <paramref name="type"/>'s hierarchy below Compze's own base classes, the root first.</summary>
   static IEnumerable<Type> ClassesFromTheRootTo(Type type)
   {
      var classes = new List<Type>();
      for(var current = type; current != null && current.Assembly != typeof(TeventRouting).Assembly; current = current.BaseType) classes.Add(current);
      classes.Reverse();
      return classes;
   }

   ///<summary>The routes <paramref name="declaringClass"/> itself declares, in declaration order: its methods, then the parts held in its fields.</summary>
   static IEnumerable<TeventRoute> RoutesDeclaredIn(Type declaringClass, bool toTeventHandlers)
   {
      const BindingFlags declaredInstanceMembers = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
      var methodName = toTeventHandlers ? HandlerMethodName : ApplierMethodName;

      var methodRoutes = declaringClass.GetMethods(declaredInstanceMembers)
                                       .Where(method => method.Name == methodName && method.ReturnType == typeof(void) && method.GetParameters() is [{ ParameterType: var parameterType }] && typeof(ITevent).IsAssignableFrom(parameterType))
                                       .OrderBy(method => method.MetadataToken)
                                       .Select(method => (TeventRoute)Invoke(nameof(RouteTo), [declaringClass, method.GetParameters()[0].ParameterType], method));

      var memberRoutes = declaringClass.GetFields(declaredInstanceMembers)
                                       .Select(field => (field, memberTevent: RoutedMemberTeventOf(field.FieldType)))
                                       .Where(it => it.memberTevent != null)
                                       .OrderBy(it => it.field.MetadataToken)
                                       .Select(it => (TeventRoute)Invoke(nameof(RouteToMember), [declaringClass, it.memberTevent!], it.field, toTeventHandlers));

      return methodRoutes.Concat(memberRoutes).ToList();
   }

   static Type? RoutedMemberTeventOf(Type fieldType) =>
      fieldType.GetInterfaces().Append(fieldType)
               .FirstOrDefault(it => it.IsGenericType && it.GetGenericTypeDefinition() == typeof(ITeventRoutedMember<>))
              ?.GetGenericArguments()[0];

   static object Invoke(string genericMethod, Type[] typeArguments, params object[] arguments) =>
      typeof(ConventionTeventRouting).GetMethod(genericMethod, BindingFlags.NonPublic | BindingFlags.Static)!.MakeGenericMethod(typeArguments).Invoke(null, arguments)!;

   static TeventRoute RouteTo<TDeclaringClass, TTevent>(MethodInfo method) where TTevent : ITevent
   {
      var call = method.CreateDelegate<Action<TDeclaringClass, TTevent>>();
      return new TeventRoute(typeof(IPublisherTevent<TTevent>), (instance, wrapped) => call((TDeclaringClass)instance, ((IPublisherTevent<TTevent>)wrapped).Tevent));
   }

   static TeventRoute RouteToMember<TDeclaringClass, TMemberTevent>(FieldInfo field, bool toTeventHandlers) where TMemberTevent : ITevent
   {
      var instance = Expression.Parameter(typeof(TDeclaringClass));
      var member = Expression.Lambda<Func<TDeclaringClass, ITeventRoutedMember<TMemberTevent>>>(Expression.Convert(Expression.Field(instance, field), typeof(ITeventRoutedMember<TMemberTevent>)), instance).Compile();
#pragma warning disable CS0618 // This is just the type of infrastructure code the member is for
      return new TeventRoute(typeof(IPublisherTevent<TMemberTevent>), (it, wrapped) => member((TDeclaringClass)it).DispatchInternal(wrapped, toTeventHandlers));
#pragma warning restore CS0618
   }
}
