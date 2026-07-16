using System.Transactions;
using Compze.Abstractions.Tessaging.Public;
using Compze.Contracts;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
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
      State.Assert(Transaction.Current == null,
                   () => $"{nameof(IIndependentTeventPublisher)} was called from within an ambient transaction — inside a unit of work. An independent publish runs as its own unit of work; called here it would silently join the caller's transaction instead of standing alone. Publish through {nameof(IUnitOfWorkTeventPublisher)}, which deliberately joins the caller's unit of work.");
      _scopeFactory.ExecuteUnitOfWork(unitOfWork => unitOfWork.Resolve<IUnitOfWorkTeventPublisher>().Publish(tevent));
   }
}
