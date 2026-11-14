using Compze.Tessaging.Endpoints;
using Compze.Tessaging.TessageTypes;
using Compze.TypeIdentifiers;

[assembly: AssemblyTypeMapper(typeof(Compze.Tests.AcrossMachines.Lab.AssemblyTypeMapper))]

namespace Compze.Tests.AcrossMachines.Lab;

///<summary>The identity of the lab endpoint: the endpoint the lab's program hosts over TCP in one machine<br/>
/// (<see cref="LabEndpoint"/>), for a <see cref="Conversation"/> in another machine to reach.</summary>
public sealed class LabEndpointIdentity : IEndpointIdentity
{
   public static string Name => "AcrossMachinesLabEndpoint";
   public static EndpointId Id { get; } = new(Guid.Parse("0a244dfa-1bd7-47a4-ba2a-7681427d47d4"));
}

///<summary>The identity of the conversing endpoint: the endpoint that holds a <see cref="Conversation"/> with the lab<br/>
/// endpoint after finding it through the lab registry (<see cref="ConversingEndpoint"/>).</summary>
public sealed class ConversingEndpointIdentity : IEndpointIdentity
{
   public static string Name => "AcrossMachinesLabConversingEndpoint";
   public static EndpointId Id { get; } = new(Guid.Parse("c5b7e0a3-4d19-4f6e-8a02-9e3b1d6f7c84"));
}

///<summary>The tuery a <see cref="Conversation"/> asks the lab endpoint. It orders its own answer: how large, so that one<br/>
/// exchange can be made to span many network packets, and after how long, so that the endpoint's machine can be made to<br/>
/// vanish while the tuery is in flight.</summary>
public class TueryAskedFromAnotherMachine : Remotable.NonTransactional.Tueries.Tuery<AnswerFromTheLabEndpoint>
{
   public TueryAskedFromAnotherMachine(int answerPayloadCharacters, TimeSpan answerAfter)
   {
      AnswerPayloadCharacters = answerPayloadCharacters;
      AnswerAfter = answerAfter;
   }

   ///<summary>How many characters of payload the answer is to carry.</summary>
   public int AnswerPayloadCharacters { get; private set; }

   ///<summary>How long the lab endpoint is to wait before it answers.</summary>
   public TimeSpan AnswerAfter { get; private set; }
}

///<summary>The lab endpoint's answer to <see cref="TueryAskedFromAnotherMachine"/>: proof that the tuery reached the endpoint's<br/>
/// machine, which the answer names, and the payload the tuery ordered.</summary>
public class AnswerFromTheLabEndpoint
{
   public AnswerFromTheLabEndpoint(string answeredByMachine, string payload)
   {
      AnsweredByMachine = answeredByMachine;
      Payload = payload;
   }

   ///<summary>The name of the machine the lab endpoint runs in.</summary>
   public string AnsweredByMachine { get; private set; }

   public string Payload { get; private set; }
}

///<summary>Maps this assembly's tessage types. Both sides map them: the lab endpoint and the pure client conversing with it.</summary>
#pragma warning disable CA1812 // Instantiated via reflection through the [assembly: AssemblyTypeMapper(typeof(...))] attribute.
class AssemblyTypeMapper : IAssemblyTypeMapper
{
   public void Map(IAssemblyTypeMappingRegistrar map) =>
      map.Map<TueryAskedFromAnotherMachine>("69fea2cf-ce17-4e36-a460-275c17f38af1");
}
