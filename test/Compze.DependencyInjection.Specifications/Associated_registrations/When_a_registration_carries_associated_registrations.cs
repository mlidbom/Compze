using Compze.DependencyInjection.Specifications.Infrastructure;
using Compze.Must;
using Compze.Must.Assertions;
using static Compze.Must.MustActions;

namespace Compze.DependencyInjection.Specifications.Associated_registrations;

public class When_a_registration_carries_associated_registrations
{
   [DependencyInjectionContainerMatrix]
   public void the_associated_registrations_are_added_to_the_container()
   {
      var builder = DependencyInjectionContainerFactory.CreateContainerBuilder();
      builder.Registrar.Register(
         Singleton.For<IPrimaryService>()
                  .WithAssociatedRegistrations(Singleton.For<IAssociatedService>().CreatedBy(() => new AssociatedService()))
                  .CreatedBy(() => new PrimaryService())
      );

      using var container = builder.Build();

      container.Resolve<IPrimaryService>().Must().NotBeNull();
      container.Resolve<IAssociatedService>().Must().NotBeNull();
   }

   [DependencyInjectionContainerMatrix]
   public void the_associated_registrations_of_an_associated_registration_are_also_added()
   {
      var builder = DependencyInjectionContainerFactory.CreateContainerBuilder();
      builder.Registrar.Register(
         Singleton.For<IPrimaryService>()
                  .WithAssociatedRegistrations(
                      Singleton.For<IAssociatedService>()
                               .WithAssociatedRegistrations(Singleton.For<INestedAssociatedService>().CreatedBy(() => new NestedAssociatedService()))
                               .CreatedBy(() => new AssociatedService()))
                  .CreatedBy(() => new PrimaryService())
      );

      using var container = builder.Build();

      container.Resolve<IAssociatedService>().Must().NotBeNull();
      container.Resolve<INestedAssociatedService>().Must().NotBeNull();
   }

   [DependencyInjectionContainerMatrix]
   public void an_associated_registration_is_lifestyle_validated_like_any_other()
   {
      var builder = DependencyInjectionContainerFactory.CreateContainerBuilder();

      var exception = Invoking(() =>
      {
         builder.Registrar.Register(
            Singleton.For<IPrimaryService>()
                     .WithAssociatedRegistrations(Scoped.For<IAssociatedService>().CreatedBy(() => new AssociatedService()))
                     .CreatedBy((IAssociatedService associated) => new PrimaryServiceDependingOnAssociated(associated))
         );
         _ = builder.Build();
      }).Must().Throw<InvalidLifeStyleCombinationException>().Which;

      exception.Message.Must().Contain("Singleton");
      exception.Message.Must().Contain("Scoped");
   }

   [DependencyInjectionContainerMatrix]
   public void two_registrations_may_not_carry_associated_registrations_for_the_same_service_type()
   {
      var builder = DependencyInjectionContainerFactory.CreateContainerBuilder();
      builder.Registrar.Register(
         Singleton.For<IPrimaryService>()
                  .WithAssociatedRegistrations(Singleton.For<IAssociatedService>().CreatedBy(() => new AssociatedService()))
                  .CreatedBy(() => new PrimaryService()),
         Singleton.For<ISecondPrimaryService>()
                  .WithAssociatedRegistrations(Singleton.For<IAssociatedService>().CreatedBy(() => new AssociatedService()))
                  .CreatedBy(() => new SecondPrimaryService())
      );

      Invoking(() => builder.Build())
         .Must()
         .Throw<InvalidOperationException>()
         .Which.Message.Must()
         .Contain("IAssociatedService")
         .Contain("already registered");
   }

   interface IPrimaryService;
   class PrimaryService : IPrimaryService;
#pragma warning disable CS9113 // Parameter is unread.
   class PrimaryServiceDependingOnAssociated(IAssociatedService _) : IPrimaryService;
#pragma warning restore CS9113 // Parameter is unread.

   interface ISecondPrimaryService;
   class SecondPrimaryService : ISecondPrimaryService;

   interface IAssociatedService;
   class AssociatedService : IAssociatedService;

   interface INestedAssociatedService;
   class NestedAssociatedService : INestedAssociatedService;
}
