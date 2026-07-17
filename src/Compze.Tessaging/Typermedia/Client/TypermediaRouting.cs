using Compze.Abstractions.Tessaging.Public;
using Compze.Tessaging.Implementation.Transport.Client.Internal;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;

namespace Compze.Tessaging.Typermedia.Client;

static class TypermediaRoutingRegistrar
{
   internal static IComponentRegistrar TypermediaRouting(this IComponentRegistrar registrar)
      => registrar.Register(Client.TypermediaRouting.RegisterWith);
}

///<summary>An endpoint's <see cref="ITypermediaRouting"/>: typermedia tessages route through the endpoint's one router — the<br/>
/// same router, connections, and discovery every tessage kind rides — and travel on the typermedia transport to the one<br/>
/// endpoint whose advertisement handles the type (see <see cref="ITessagingRouter.AddressOfTypermediaHandlerFor"/>).</summary>
class TypermediaRouting : ITypermediaRouting
{
   public static void RegisterWith(IComponentRegistrar registrar)
      => registrar.Register(Singleton.For<ITypermediaRouting>().CreatedBy(
            (ITessagingRouter router, ITypermediaTransport transport) => new TypermediaRouting(router, transport)));

   readonly ITessagingRouter _router;
   readonly ITypermediaTransport _transport;

   TypermediaRouting(ITessagingRouter router, ITypermediaTransport transport)
   {
      _router = router;
      _transport = transport;
   }

   public async Task PostAsync(IAtMostOnceTypermediaTommand tommand) =>
      await _transport.PostAsync(tommand, _router.AddressOfTypermediaHandlerFor(tommand.GetType())).caf();

   public async Task<TTommandResult> PostAsync<TTommandResult>(IAtMostOnceTypermediaTommand<TTommandResult> typermediaTommand) =>
      await _transport.PostAsync(typermediaTommand, _router.AddressOfTypermediaHandlerFor(typermediaTommand.GetType())).caf();

   public async Task<TTueryResult> GetAsync<TTueryResult>(IRemotableTuery<TTueryResult> tuery) =>
      await _transport.GetAsync(tuery, _router.AddressOfTypermediaHandlerFor(tuery.GetType())).caf();
}
