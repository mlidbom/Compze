using Compze.Abstractions.Tessaging.Public;
using Compze.TypeIdentifiers;

[assembly: AssemblyTypeMapper(typeof(Compze.Tests.SameMachine.EndpointHostProcess.AssemblyTypeMapper))]

namespace Compze.Tests.SameMachine.EndpointHostProcess;

///<summary>The tommand the specification process sends to the endpoint host process — the parent→child leg of the multi-process conversation.</summary>
public class TommandSentToTheEndpointHostProcess : TessageTypes.Remotable.ExactlyOnce.Tommand;

///<summary>The tommand the endpoint host process sends back to the specification process's endpoint when it handles<br/>
/// <see cref="TommandSentToTheEndpointHostProcess"/> — the child→parent leg, proving the child discovered the parent through the registry.</summary>
public class TommandSentBackToTheSpecificationProcess : TessageTypes.Remotable.ExactlyOnce.Tommand;

///<summary>Maps this assembly's tessage types. Both sides of the multi-process conversation register these — the specification's endpoint<br/>
/// and the endpoint host process — via <c>MapTypesFromAssemblyContaining</c>.</summary>
#pragma warning disable CA1812 // Instantiated via reflection through the [assembly: AssemblyTypeMapper(typeof(...))] attribute.
class AssemblyTypeMapper : IAssemblyTypeMapper
{
   public void Map(IAssemblyTypeMappingRegistrar map) =>
      map.Map<TommandSentToTheEndpointHostProcess>("6f6a2f4e-90a2-40ec-a9c9-3f5b9d0d13c8")
         .Map<TommandSentBackToTheSpecificationProcess>("2c19b90b-93cd-4a0a-a4bd-4a19a11c7d29");
}
