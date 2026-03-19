using Compze.DependencyInjection.Abstractions;
using Compze.DependencyInjection.Microsoft;
using Compze.Must;
using Compze.xUnitBDD;

namespace Compze.DependencyInjection.Specifications;

public class MicrosoftContainerInternalsTests
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
   public void ServiceProvider_is_accessible_after_Build_is_called()
   {
      using var container = new MicrosoftDependencyInjectionContainer();
      _ = ((IContainerBuilder)container).Build();
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
