using Compze.Abstractions.Hosting.Public;
using Compze.Internals.Transport.NamedPipes;
using Compze.Tessaging.Implementation.Transport.Abstractions;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Compze.Internals.Transport;

namespace Compze.Tessaging.Implementation.Transport.Client.Implementation.NamedPipes;

///<summary>The named-pipe implementation of the Tessaging transport's client side: posts a tessage to the receiving endpoint's<br/>
/// inbox pipe server and awaits the acknowledgement it writes after registering the tessage — the same-machine counterpart of<br/>
/// <see cref="Http.HttpTransportMessagePoster"/>.</summary>
class NamedPipeTransportMessagePoster : ITransportMessagePoster
{
   public static void RegisterWith(IComponentRegistrar registrar)
      => registrar.Register(Singleton.For<ITransportMessagePoster>()
                                     .CreatedBy(() => new NamedPipeTransportMessagePoster()));

   static TransportRequestKind RequestKindFor(TransportTessage.OutGoing tessage) =>
      tessage.TessageTypeEnum switch
      {
         TransportTessageType.ExactlyOnceTevent => TransportRequestKind.ExactlyOnceTevent,
         TransportTessageType.ExactlyOnceTommand => TransportRequestKind.ExactlyOnceTommand,
         _ => throw new ArgumentOutOfRangeException()
      };

   public async Task PostAsync(TransportTessage.OutGoing tessage, EndpointAddress endPointAddress) =>
      await NamedPipeTransportClient.SendAsync(new TransportRequest(RequestKindFor(tessage), tessage.TessageId, tessage.Type.CanonicalString, tessage.Body),
                                               endPointAddress).caf();
}
