using Compze.DependencyInjection.Abstractions;
using Compze.DependencyInjection.Specifications.Infrastructure;
using Compze.Must;

using static Compze.Must.MustActions;

// ReSharper disable InconsistentNaming

namespace Compze.DependencyInjection.Specifications.UnitOfWork_lifestyle;

public class When_resolving_a_unit_of_work_component
{
   [DependencyInjectionContainerMatrix]
   public void within_ExecuteUnitOfWork_every_resolve_returns_the_same_instance()
   {
      using var container = CreateContainerWithUnitOfWorkComponent();
      container.ExecuteUnitOfWork(unitOfWork => unitOfWork.Resolve<IMyUnitOfWorkBoundService>().Must().Be(unitOfWork.Resolve<IMyUnitOfWorkBoundService>()));
   }

   ///<summary>ToString() rather than Message in the assertion: some backend containers (Autofac) wrap the instantiation's<br/>
   /// exception in their own resolution exception, putting the assert's message on the inner exception.</summary>
   [DependencyInjectionContainerMatrix]
   public void in_a_scope_with_no_ambient_transaction_resolution_throws_naming_the_missing_ambient_transaction()
   {
      using var container = CreateContainerWithUnitOfWorkComponent();
      container.ExecuteInIsolatedScope(scope =>
         Invoking(() => scope.Resolve<IMyUnitOfWorkBoundService>())
            .Must().Throw<Exception>()
            .Which.ToString().Must().Contain("ambient transaction"));
   }

   [DependencyInjectionContainerMatrix]
   public void a_singleton_depending_on_the_component_fails_the_build_as_an_invalid_lifestyle_combination()
   {
      var builder = DependencyInjectionContainerFactory.CreateContainerBuilder();
      builder.Registrar.Register(
         UnitOfWorkParticipant.For<IMyUnitOfWorkBoundService>().CreatedBy(() => new MyUnitOfWorkBoundService()),
         Singleton.For<IMySingletonService>().CreatedBy((IMyUnitOfWorkBoundService unitOfWorkBound) => new SingletonDependingOnUnitOfWorkBound(unitOfWorkBound)));

      Invoking(() => builder.Build())
         .Must().Throw<InvalidLifeStyleCombinationException>()
         .Which.Message.Must().Contain(nameof(Lifestyle.UnitOfWork));
   }

   static IDependencyInjectionContainer CreateContainerWithUnitOfWorkComponent()
   {
      var builder = DependencyInjectionContainerFactory.CreateContainerBuilder();
      builder.Registrar.Register(UnitOfWorkParticipant.For<IMyUnitOfWorkBoundService>().CreatedBy(() => new MyUnitOfWorkBoundService()));
      return builder.Build();
   }

   interface IMyUnitOfWorkBoundService;
   class MyUnitOfWorkBoundService : IMyUnitOfWorkBoundService;

   interface IMySingletonService;
#pragma warning disable CS9113 // Parameter is unread.
   class SingletonDependingOnUnitOfWorkBound(IMyUnitOfWorkBoundService _) : IMySingletonService;
#pragma warning restore CS9113 // Parameter is unread.
}
