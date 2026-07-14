using Compze.Abstractions.Hosting.Public;
using Compze.Internals.Transport.NamedPipes;
using Compze.Tessaging.Implementation.Transport.Abstractions;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;

namespace Compze.Tessaging.Implementation.Transport.Client.Implementation.NamedPipes;

///<summary>The named-pipe implementation of the Tessaging transport's client side: posts a tessage to the receiving endpoint's<br/>
/// inbox pipe server and awaits the acknowledgement it writes after registering the tessage — the same-machine counterpart of<br/>
/// <see cref="Http.HttpTransportMessagePoster"/>.</summary>
class NamedPipeTransportMessagePoster : ITransportMessagePoster
{
   public static void RegisterWith(IComponentRegistrar registrar)
      => registrar.Register(Singleton.For<ITransportMessagePoster>()
                                     .CreatedBy(() => new NamedPipeTransportMessagePoster()));

   static NamedPipeTransportRequestKind RequestKindFor(TransportTessage.OutGoing tessage) =>
      tessage.TessageTypeEnum switch
      {
         TransportTessageType.ExactlyOnceTevent => NamedPipeTransportRequestKind.ExactlyOnceTevent,
         TransportTessageType.ExactlyOnceTommand => NamedPipeTransportRequestKind.ExactlyOnceTommand,
         _ => throw new ArgumentOutOfRangeException()
      };

   public async Task PostAsync(TransportTessage.OutGoing tessage, EndpointAddress endPointAddress) =>
      await NamedPipeTransportClient.SendAsync(new NamedPipeTransportRequest(RequestKindFor(tessage), tessage.TessageId, tessage.Type.CanonicalString, tessage.Body),
                                               endPointAddress).caf();
}
