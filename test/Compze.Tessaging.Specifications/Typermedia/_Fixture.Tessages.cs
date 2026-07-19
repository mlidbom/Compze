using Compze.Abstractions.Public;
using Compze.Abstractions.Tessaging.Public;
using Compze.TypeIdentifiers;
// ReSharper disable PropertyCanBeMadeInitOnly.Global serialization...
// ReSharper disable MemberCanBeInternal serialization...

[assembly: AssemblyTypeMapper(typeof(Compze.Tessaging.Specifications.Typermedia.AssemblyTypeMapper))]

// ReSharper disable ClassNeverInstantiated.Global
#pragma warning disable CA1724 // Type names should not match namespaces

namespace Compze.Tessaging.Specifications.Typermedia;

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

public class TueryAnswer
{
   public string Message { get; set; } = "";
}

public class TueryBothEndpointsHandle : TessageTypes.Remotable.NonTransactional.Tueries.Tuery<TueryAnswer>;

public class TueryOnlyTheSecondEndpointHandles : TessageTypes.Remotable.NonTransactional.Tueries.Tuery<TueryAnswer>;

public class TueryServedByTheLateEndpoint : TessageTypes.Remotable.NonTransactional.Tueries.Tuery<TueryAnswer>;

public class TueryNothingServes : TessageTypes.Remotable.NonTransactional.Tueries.Tuery<TueryAnswer>;

#pragma warning disable CA1812 // Instantiated via reflection through the [assembly: AssemblyTypeMapper(typeof(...))] attribute.
class AssemblyTypeMapper : IAssemblyTypeMapper
{
   public void Map(IAssemblyTypeMappingRegistrar map)
      => map.Map<GreetingTuery>("7e8158a9-4972-4bd3-a0a5-9b261e7e5e64")
            .Map<RegisterGreeterTommand>("8b7a82f3-9c2e-4f24-9a96-2f0d2af56a01")
            .Map<TueryBothEndpointsHandle>("c93a55b7-30a1-4a35-b7c8-01c9d61d31e6")
            .Map<TueryOnlyTheSecondEndpointHandles>("5f2f0f68-8a4e-4a2e-b7a2-38d8bd7fca10")
            .Map<TueryServedByTheLateEndpoint>("2a6de5cf-40b8-4f11-9c76-8f0e2f5f13b2")
            .Map<TueryNothingServes>("9d41c2b7-6a35-4a90-8f3e-5b7c1d20ae44");
}

public static class TypermediaHostingSpecificationTypeMappings
{
   /// <summary>Registers the type mappings declared by this specification assembly (its test message types).</summary>
   public static void RegisterTypermediaHostingSpecificationTypeMappings(this ITypeMapper mapper)
      => mapper.MapTypesFromAssemblyContaining<AssemblyTypeMapper>();
}
