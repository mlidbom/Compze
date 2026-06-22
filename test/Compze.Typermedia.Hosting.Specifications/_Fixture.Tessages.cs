using Compze.Abstractions.Public;
using Compze.Abstractions.Tessaging.Public;
using Compze.TypeIdentifiers;

[assembly: AssemblyTypeMapper(typeof(Compze.Typermedia.Hosting.Specifications.AssemblyTypeMapper))]

// ReSharper disable ClassNeverInstantiated.Global
#pragma warning disable CA1724 // Type names should not match namespaces

namespace Compze.Typermedia.Hosting.Specifications;

public class Greeting
{
   public string Message { get; set; } = "";
}

public class GreetingTuery : TessageTypes.Remotable.NonTransactional.Tueries.Tuery<Greeting>
{
   public string Name { get; set; } = "";
}

public class GreeterRegistered
{
   public string Name { get; set; } = "";
}

public class RegisterGreeterTommand : TessageTypes.Remotable.AtMostOnce.AtMostOnceTypermediaTommand<GreeterRegistered>
{
   RegisterGreeterTommand() {}
   public static RegisterGreeterTommand Create(string name) => new() { Id = new TessageId(), Name = name };

   // ReSharper disable once MemberCanBeInternal — Serialized via Newtonsoft
   public string Name { get; set; } = "";
}

#pragma warning disable CA1812 // Instantiated via reflection through the [assembly: AssemblyTypeMapper(typeof(...))] attribute.
class AssemblyTypeMapper : IAssemblyTypeMapper
{
   public void Map(IAssemblyTypeMappingRegistrar map)
      => map.Map<GreetingTuery>("7e8158a9-4972-4bd3-a0a5-9b261e7e5e64")
            .Map<RegisterGreeterTommand>("8b7a82f3-9c2e-4f24-9a96-2f0d2af56a01");
}

public static class TypermediaHostingSpecificationTypeMappings
{
   /// <summary>Registers the type mappings declared by this specification assembly (its test message types).</summary>
   public static void RegisterTypermediaHostingSpecificationTypeMappings(this ITypeMapper mapper)
      => mapper.MapTypesFromAssemblyContaining<AssemblyTypeMapper>();
}
