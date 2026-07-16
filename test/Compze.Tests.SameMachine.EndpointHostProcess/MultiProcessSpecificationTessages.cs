using Compze.Abstractions.Tessaging.Public;
using Compze.TypeIdentifiers;

[assembly: AssemblyTypeMapper(typeof(Compze.Tests.SameMachine.EndpointHostProcess.AssemblyTypeMapper))]

#pragma warning disable CA1040 //The best-effort tevents are empty marker interfaces by design: their type IS the routed contract.

namespace Compze.Tests.SameMachine.EndpointHostProcess;

///<summary>The tommand the specification process sends to the endpoint host process — the parent→child leg of the multi-process conversation.</summary>
public class TommandSentToTheEndpointHostProcess : TessageTypes.Remotable.ExactlyOnce.Tommand;

///<summary>The tommand the endpoint host process sends back to the specification process's endpoint when it handles<br/>
/// <see cref="TommandSentToTheEndpointHostProcess"/> — the child→parent leg, proving the child discovered the parent through the registry.</summary>
public class TommandSentBackToTheSpecificationProcess : TessageTypes.Remotable.ExactlyOnce.Tommand;

///<summary>The best-effort tevent the specification process publishes — the parent→child leg of the guarantee-free multi-process<br/>
/// conversation: a plain <see cref="IRemotableTevent"/>, crossing the wire best-effort with no database in either process.</summary>
public interface IBestEffortTeventPublishedByTheSpecificationProcess : IRemotableTevent;

public class BestEffortTeventPublishedByTheSpecificationProcess : IBestEffortTeventPublishedByTheSpecificationProcess;

///<summary>The best-effort tevent the endpoint host process publishes when it handles<br/>
/// <see cref="IBestEffortTeventPublishedByTheSpecificationProcess"/> — the child→parent leg, proving the child discovered the parent through the registry.</summary>
public interface IBestEffortTeventPublishedByTheEndpointHostProcess : IRemotableTevent;

public class BestEffortTeventPublishedByTheEndpointHostProcess : IBestEffortTeventPublishedByTheEndpointHostProcess;

///<summary>The tuery the specification process executes against the endpoint host process — the typermedia leg of the<br/>
/// database-less multi-process conversation: the answering endpoint is found through the shared registry, never by a configured address.</summary>
public class TueryAskedByTheSpecificationProcess : TessageTypes.Remotable.NonTransactional.Tueries.Tuery<AnswerToTheTueryAskedByTheSpecificationProcess>;

///<summary>The endpoint host process's answer to <see cref="TueryAskedByTheSpecificationProcess"/> — proof the tuery crossed the<br/>
/// process boundary and was answered there.</summary>
public class AnswerToTheTueryAskedByTheSpecificationProcess(string answeredBy)
{
   public string AnsweredBy { get; private set; } = answeredBy;
}

///<summary>Maps this assembly's tessage types. Both sides of the multi-process conversation register these — the specification's endpoint<br/>
/// and the endpoint host process — via <c>MapTypesFromAssemblyContaining</c>.</summary>
#pragma warning disable CA1812 // Instantiated via reflection through the [assembly: AssemblyTypeMapper(typeof(...))] attribute.
class AssemblyTypeMapper : IAssemblyTypeMapper
{
   public void Map(IAssemblyTypeMappingRegistrar map) =>
      map.Map<TommandSentToTheEndpointHostProcess>("6f6a2f4e-90a2-40ec-a9c9-3f5b9d0d13c8")
         .Map<TommandSentBackToTheSpecificationProcess>("2c19b90b-93cd-4a0a-a4bd-4a19a11c7d29")
         .Map<IBestEffortTeventPublishedByTheSpecificationProcess>("9e4f6c1d-2a8b-4b53-8f6a-71d90c24e5b7")
         .Map<BestEffortTeventPublishedByTheSpecificationProcess>("d17b3e58-4c96-4f02-9d3b-8a65f1c40e29")
         .Map<IBestEffortTeventPublishedByTheEndpointHostProcess>("4b82d9f6-7e15-4a6c-b0d8-3c59e21a7f84")
         .Map<BestEffortTeventPublishedByTheEndpointHostProcess>("f3a61c07-95d4-4e38-a2b7-60c8d51e94f3")
         .Map<TueryAskedByTheSpecificationProcess>("8c5e02d9-46b7-4f1a-9e83-27d1b60c48f5");
}
