using Compze.DependencyInjection.Specifications.Infrastructure;
using Compze.Must;
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
                  .CreatedBy(() => new PrimaryService())
                  .WithAssociatedRegistrations(Singleton.For<IAssociatedService>().CreatedBy(() => new AssociatedService()))
      );

      using var container = builder.Build();

      container.Resolve<IPrimaryService>().Must().NotBeNull();
      container.Resolve<IAssociatedService>().Must().NotBeNull();
   }

   [DependencyInjectionContainerMatrix]
   public void an_associated_registration_is_lifestyle_validated_like_any_other()
   {
      var builder = DependencyInjectionContainerFactory.CreateContainerBuilder();

      var exception = Invoking(() =>
      {
         // ReSharper disable once AccessToDisposedClosure
         builder.Registrar.Register(
            Singleton.For<IPrimaryService>()
                     .CreatedBy((IAssociatedService associated) => new PrimaryServiceDependingOnAssociated(associated))
                     .WithAssociatedRegistrations(Scoped.For<IAssociatedService>().CreatedBy(() => new AssociatedService()))
         );
         _ = builder.Build();
      }).Must().Throw<InvalidLifeStyleCombinationException>().Which;

      exception.Message.Must().Contain("Singleton");
      exception.Message.Must().Contain("Scoped");
   }

   interface IPrimaryService;
   class PrimaryService : IPrimaryService;
#pragma warning disable CS9113 // Parameter is unread.
   class PrimaryServiceDependingOnAssociated(IAssociatedService _) : IPrimaryService;
#pragma warning restore CS9113 // Parameter is unread.

   interface IAssociatedService;
   class AssociatedService : IAssociatedService;
}
