using Compze.Abstractions.Tessaging.Public;
using Compze.Contracts;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.SystemCE.TransactionsCE;
using JetBrains.Annotations;

namespace Compze.Tessaging.Implementation;

static class TransactionIgnoringTeventPublisherRegistrar
{
   public static IComponentRegistrar TransactionIgnoringTeventPublisher(this IComponentRegistrar registrar)
      => registrar.Register(Implementation.UnitOfWorkIgnoringTeventPublisher.RegisterWith);
}

///<summary>The <see cref="IUnitOfWorkIgnoringTeventPublisher"/>: the <see cref="IUnitOfWorkTeventPublisher"/> with the ambient transaction<br/>
/// suppressed. Under suppression the ordinary publisher's honor-the-transaction behavior degenerates on every leg into exactly the<br/>
/// escape hatch's contract — participation dispatches detached from the caller's transaction, observation fires, and the transient<br/>
/// leg sees no transaction to defer to, so it hands the tevent to the subscribers' connections right away.</summary>
[UsedImplicitly] class UnitOfWorkIgnoringTeventPublisher : IUnitOfWorkIgnoringTeventPublisher
{
   public static void RegisterWith(IComponentRegistrar registrar)
      => registrar.Register(Scoped.For<IUnitOfWorkIgnoringTeventPublisher>()
                                  .CreatedBy((IUnitOfWorkTeventPublisher teventPublisher) => new UnitOfWorkIgnoringTeventPublisher(teventPublisher)));

   readonly IUnitOfWorkTeventPublisher _teventPublisher;

   UnitOfWorkIgnoringTeventPublisher(IUnitOfWorkTeventPublisher teventPublisher) => _teventPublisher = teventPublisher;

   public void Publish(ITevent tevent)
   {
      State.Assert(tevent is not IMustBeSentTransactionally,
                   () => $"{tevent.GetType().FullName} implements {typeof(IMustBeSentTransactionally).FullName}, and immediate, unconditional delivery structurally cannot back a transactional send. Publish it through {typeof(IUnitOfWorkTeventPublisher).FullName}, which honors the transaction its send must join — the transaction-ignoring publisher is only for tevents whose types demand no transactional send.");
      TransactionScopeCe.SuppressAmbient(() => _teventPublisher.Publish(tevent));
   }
}
