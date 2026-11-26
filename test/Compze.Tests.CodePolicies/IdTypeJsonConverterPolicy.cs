using System.Reflection;
using System.Text.Json.Serialization;
using Compze.Abstractions;
using Compze.Internals.SystemCE.ReflectionCE;
using Compze.Must;
using Compze.Tests.CodePolicies.Infrastructure;
using Compze.xUnitBDD;

namespace Compze.Tests.CodePolicies;

///<summary>Has every id type a Compze library declares, each class derived from <see cref="EntityId{TPrimitive}"/>, carry the<br/>
/// <see cref="EntityIdJsonConverter"/> as a <see cref="JsonConverterAttribute"/> of its own.</summary>
///<remarks>System.Text.Json reads the attribute off the type it serializes alone, never off a base type. An id type without its own is<br/>
/// written as an object holding its <see cref="Abstractions.Infrastructure.ValueWrapper{TValue}.Value"/>, and read back only if its<br/>
/// constructor's parameter happens to be named after it, so a consumer's snapshots in the memory image state store would no longer<br/>
/// round-trip it as they do every other Compze id.</remarks>
public static class IdTypeJsonConverterPolicy
{
   [XF] public static void Every_id_type_declared_in_a_Compze_library_assembly_carries_the_EntityIdJsonConverter()
   {
      CompzeAssemblyLoader.EnsureAllCompzeAssembliesAreLoaded();

      var idTypes = AppDomain.CurrentDomain.AllCompzeLibraryTypes().Where(IsAnIdType).ToList();

      new
      {
         IdTypesFound = idTypes.Count,
         WithoutTheConverter = idTypes.Where(type => !CarriesTheEntityIdJsonConverter(type)).Select(type => type.FullName).Order(StringComparer.Ordinal).ToList()
      }.Must().Satisfy(it => it.IdTypesFound > 0 && it.WithoutTheConverter.Count == 0);
   }

   static bool IsAnIdType(Type type) => type.IsClass && type.ClassInheritanceChain().Any(it => it.IsGenericType && it.GetGenericTypeDefinition() == typeof(EntityId<>));

   static bool CarriesTheEntityIdJsonConverter(Type type) => type.GetCustomAttribute<JsonConverterAttribute>(inherit: false)?.ConverterType == typeof(EntityIdJsonConverter);
}
