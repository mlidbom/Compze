using Compze.Tests.Infrastructure;
using Compze.Utilities.DependencyInjection.Microsoft;
using Compze.Utilities.Testing.Must;
using Compze.Utilities.Testing.XUnit.BDD;

namespace Compze.Tests.Integration.DependencyInjection;

public class MicrosoftContainerInternalsTests : UniversalTestBase
{
   [XF]
   public void Container_implements_IMicrosoftContainerInternals_explicitly()
   {
      using var container = new MicrosoftDependencyInjectionContainer();
      container.Must().BeAssignableTo<IMicrosoftContainerInternals>();
   }

   [XF]
   public void ServiceCollection_is_accessible_via_explicit_interface_cast()
   {
      using var container = new MicrosoftDependencyInjectionContainer();
      var internals = (IMicrosoftContainerInternals)container;
      internals.ServiceCollection.Must().NotBeNull();
   }

   [XF]
   public void ServiceProvider_is_accessible_after_ServiceLocator_is_accessed()
   {
      using var container = new MicrosoftDependencyInjectionContainer();
      _ = container.ServiceLocator;
      var internals = (IMicrosoftContainerInternals)container;
      internals.ServiceProvider.Must().NotBeNull();
   }

   [XF]
   public void IMicrosoftContainerInternals_members_are_not_accessible_without_cast()
   {
      using var container = new MicrosoftDependencyInjectionContainer();
      container.GetType().GetProperty(nameof(IMicrosoftContainerInternals.ServiceCollection))
               .Must().BeNull();
      container.GetType().GetProperty(nameof(IMicrosoftContainerInternals.ServiceProvider))
               .Must().BeNull();
   }
}
