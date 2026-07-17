using Compze.Abstractions.Hosting.Public;
using Compze.Tessaging.Internals.Transport;
using Compze.Tessaging.Implementation.Transport.Abstractions;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;

namespace Compze.Tessaging.Implementation.Transport.Client.Implementation;

public static class TransportMessagePosterRegistrar
{
   ///<summary>Registers the client side of the Tessaging transport (<see cref="TransportMessagePoster"/>), which runs on the<br/>
   /// endpoint transport client (<see cref="IEndpointTransportClient"/>) a protocol registration supplies.</summary>
   public static IComponentRegistrar TessagingTransportMessagePoster(this IComponentRegistrar registrar)
      => registrar.Register(TransportMessagePoster.RegisterWith);
}

///<summary>The client side of the Tessaging transport: posts a tessage to the receiving endpoint's inbox through the endpoint<br/>
/// transport client (<see cref="IEndpointTransportClient"/>) and awaits the acknowledgement written after the inbox has<br/>
/// registered the tessage — one implementation for every protocol, since the protocol difference lives entirely in the<br/>
/// transport client.</summary>
class TransportMessagePoster : ITransportMessagePoster
{
   public static void RegisterWith(IComponentRegistrar registrar)
      => registrar.Register(Singleton.For<ITransportMessagePoster>()
                                     .CreatedBy((IEndpointTransportClient transportClient) => new TransportMessagePoster(transportClient)));

   readonly IEndpointTransportClient _transportClient;

   TransportMessagePoster(IEndpointTransportClient transportClient) => _transportClient = transportClient;

   public async Task PostAsync(TransportTessage.OutGoing tessage, EndpointAddress endPointAddress) =>
      await _transportClient.SendAsync(new TransportRequest(RequestKindFor(tessage), tessage.TessageId, tessage.Type.CanonicalString, tessage.Body),
                                       endPointAddress).caf();

   static TransportRequestKind RequestKindFor(TransportTessage.OutGoing tessage) =>
      tessage.TessageTypeEnum switch
      {
         TransportTessageType.ExactlyOnceTevent => TransportRequestKind.ExactlyOnceTevent,
         TransportTessageType.ExactlyOnceTommand => TransportRequestKind.ExactlyOnceTommand,
         TransportTessageType.BestEffortTevent => TransportRequestKind.BestEffortTevent,
         _ => throw new ArgumentOutOfRangeException()
      };
}
