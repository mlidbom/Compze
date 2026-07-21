using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Compze.Tessaging.Private.HandlerAvailability;
using Compze.Tessaging.TessageTypes;

namespace Compze.Tessaging.Typermedia.Private;

static class TypermediaRoutingRegistrar
{
   internal static IComponentRegistrar TypermediaRouting(this IComponentRegistrar registrar)
      => registrar.Register(Private.TypermediaRouting.RegisterWith);
}

///<summary>An endpoint's <see cref="ITypermediaRouting"/>: typermedia tessages route through the endpoint's one router — the<br/>
/// same router, connections, and discovery every tessage kind rides — and travel on the typermedia transport to the one<br/>
/// endpoint whose advertisement handles the type. Every send is a waiting send: a type with no live, unambiguous route right<br/>
/// now waits, within the endpoint's handler-availability patience, for one to appear before failing loud<br/>
/// (see <see cref="IHandlerAvailability"/>).</summary>
class TypermediaRouting : ITypermediaRouting
{
   public static void RegisterWith(IComponentRegistrar registrar)
      => registrar.Register(Singleton.For<ITypermediaRouting>().CreatedBy(
            (IHandlerAvailability handlerAvailability, ITypermediaTransport transport) => new TypermediaRouting(handlerAvailability, transport)));

   readonly IHandlerAvailability _handlerAvailability;
   readonly ITypermediaTransport _transport;

   TypermediaRouting(IHandlerAvailability handlerAvailability, ITypermediaTransport transport)
   {
      _handlerAvailability = handlerAvailability;
      _transport = transport;
   }

   public async Task PostAsync(IAtMostOnceTypermediaTommand tommand) =>
      await _transport.PostAsync(tommand, await _handlerAvailability.AwaitAddressOfTypermediaHandlerForAsync(tommand.GetType()).caf()).caf();

   public async Task<TTommandResult> PostAsync<TTommandResult>(IAtMostOnceTypermediaTommand<TTommandResult> typermediaTommand) =>
      await _transport.PostAsync(typermediaTommand, await _handlerAvailability.AwaitAddressOfTypermediaHandlerForAsync(typermediaTommand.GetType()).caf()).caf();

   public async Task<TTueryResult> GetAsync<TTueryResult>(IRemotableTuery<TTueryResult> tuery) =>
      await _transport.GetAsync(tuery, await _handlerAvailability.AwaitAddressOfTypermediaHandlerForAsync(tuery.GetType()).caf()).caf();
}
