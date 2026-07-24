using Compze.DependencyInjection.Abstractions;
using Compze.Tessaging.TessageTypes;
using Compze.TypeIdentifiers;
using Compze.TypeIdentifiers.DependencyInjection;
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

public class GreetingTuery : Remotable.NonTransactional.Tueries.Tuery<Greeting>
{
   public string Name { get; set; } = "";
}

public class GreeterRegistered
{
   public string Name { get; set; } = "";
}

public class RegisterGreeterTommand : Remotable.AtMostOnce.AtMostOnceTypermediaTommand<GreeterRegistered>
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

public class TueryBothEndpointsHandle : Remotable.NonTransactional.Tueries.Tuery<TueryAnswer>;

public class TueryOnlyTheSecondEndpointHandles : Remotable.NonTransactional.Tueries.Tuery<TueryAnswer>;

public class TueryServedByTheLateEndpoint : Remotable.NonTransactional.Tueries.Tuery<TueryAnswer>;

public class TueryNothingServes : Remotable.NonTransactional.Tueries.Tuery<TueryAnswer>;

///<summary>A number carried back from a navigation step, so that a chain composing steps has something to pass along.</summary>
public class NumberResource
{
   public int Value { get; set; }
}

public class TheAnswerTuery : Remotable.NonTransactional.Tueries.Tuery<NumberResource>;

public class AddTuery : Remotable.NonTransactional.Tueries.Tuery<NumberResource>
{
   public int Left { get; set; }
   public int Right { get; set; }
}

public class RememberNumberTommand : Remotable.AtMostOnce.AtMostOnceTypermediaTommand<NumberResource>
{
   RememberNumberTommand() {}
   public static RememberNumberTommand Create(int number) => new() { Id = new TessageId(), Number = number };

   public int Number { get; set; }
}

#pragma warning disable CA1812 // Instantiated via reflection through the [assembly: AssemblyTypeMapper(typeof(...))] attribute.
class AssemblyTypeMapper : IAssemblyTypeMapper
{
   public void Map(IAssemblyTypeMappingRegistrar map)
      => map.Map<GreetingTuery>("7e8158a9-4972-4bd3-a0a5-9b261e7e5e64")
            .Map<RegisterGreeterTommand>("8b7a82f3-9c2e-4f24-9a96-2f0d2af56a01")
            .Map<TueryBothEndpointsHandle>("c93a55b7-30a1-4a35-b7c8-01c9d61d31e6")
            .Map<TueryOnlyTheSecondEndpointHandles>("5f2f0f68-8a4e-4a2e-b7a2-38d8bd7fca10")
            .Map<TueryServedByTheLateEndpoint>("2a6de5cf-40b8-4f11-9c76-8f0e2f5f13b2")
            .Map<TueryNothingServes>("9d41c2b7-6a35-4a90-8f3e-5b7c1d20ae44")
            .Map<TheAnswerTuery>("6c15f9a3-2d70-4e81-9b4c-73a0e5182fd6")
            .Map<AddTuery>("0b83d4e6-51c9-4a27-8e60-2f9d71c34b58")
            .Map<RememberNumberTommand>("f27a061b-9e34-4c85-b1d0-58e63a2790cf");
}

// ReSharper disable once CheckNamespace
public static class TypermediaHostingSpecificationTypeMappings
{
   /// <summary>Requires the type identity of this specification assembly — its test message types.</summary>
   public static IComponentRegistrar RequireTypermediaHostingSpecificationTypeMappings(this IComponentRegistrar @this)
      => @this.RequireMappedTypesFromAssemblyContaining<AssemblyTypeMapper>();
}
