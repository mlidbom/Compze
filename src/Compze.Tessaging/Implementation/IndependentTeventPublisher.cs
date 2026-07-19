using System.Transactions;
using Compze.Contracts;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Compze.Tessaging.Abstractions;
using Compze.Tessaging.Abstractions.TessageTypes;
using JetBrains.Annotations;

namespace Compze.Tessaging.Implementation;

static class IndependentTeventPublisherRegistrar
{
   public static IComponentRegistrar IndependentTeventPublisher(this IComponentRegistrar registrar)
      => registrar.Register(Implementation.IndependentTeventPublisher.RegisterWith);
}

///<summary>The <see cref="IIndependentTeventPublisher"/>: runs each publish as its own unit of work —<br/>
/// <c>ExecuteUnitOfWork</c> around the endpoint's <see cref="IUnitOfWorkTeventPublisher"/> — after asserting that the caller<br/>
/// stands outside any ambient transaction, so an independent publish can never silently join a caller's unit of work.</summary>
[UsedImplicitly] class IndependentTeventPublisher : IIndependentTeventPublisher
{
   public static void RegisterWith(IComponentRegistrar registrar)
      => registrar.Register(Singleton.For<IIndependentTeventPublisher>()
                                     .CreatedBy((IScopeFactory scopeFactory) => new IndependentTeventPublisher(scopeFactory)));

   readonly IScopeFactory _scopeFactory;

   IndependentTeventPublisher(IScopeFactory scopeFactory) => _scopeFactory = scopeFactory;

   public void Publish(ITevent tevent)
   {
      AssertStandsOutsideAnyAmbientTransaction();
      //The sync/async split (an exactly-once tevent is refused with a message pointing at PublishAsync) is enforced by the inner door.
      _scopeFactory.ExecuteUnitOfWork(unitOfWork => unitOfWork.Resolve<IUnitOfWorkTeventPublisher>().Publish(tevent));
   }

   public async Task PublishAsync(ITevent tevent)
   {
      AssertStandsOutsideAnyAmbientTransaction();
      await _scopeFactory.ExecuteUnitOfWorkAsync(async unitOfWork => await unitOfWork.Resolve<IUnitOfWorkTeventPublisher>().PublishAsync(tevent).caf()).caf();
   }

   static void AssertStandsOutsideAnyAmbientTransaction() =>
      State.Assert(Transaction.Current == null,
                   () => $"{nameof(IIndependentTeventPublisher)} was called from within an ambient transaction — inside a unit of work. An independent publish runs as its own unit of work; called here it would silently join the caller's transaction instead of standing alone. Publish through {nameof(IUnitOfWorkTeventPublisher)}, which deliberately joins the caller's unit of work.");
}
