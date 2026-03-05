using Compze.Utilities.DependencyInjection.SimpleInjector;
using Compze.Must;
using Compze.xUnitBDD;

namespace Compze.Internals.DependencyInjection.Specifications;

public class SimpleInjectorContainerInternalsTests
{
   [XF]
   public void Container_implements_ISimpleInjectorContainerInternals_explicitly()
   {
      using var container = new SimpleInjectorDependencyInjectionContainer();
      container.Must().BeAssignableTo<ISimpleInjectorContainerInternals>();
   }

   [XF]
   public void Container_is_accessible_via_explicit_interface_cast()
   {
      using var container = new SimpleInjectorDependencyInjectionContainer();
      var internals = (ISimpleInjectorContainerInternals)container;
      internals.Container.Must().NotBeNull();
   }

   [XF]
   public void ISimpleInjectorContainerInternals_members_are_not_accessible_without_cast()
   {
      using var container = new SimpleInjectorDependencyInjectionContainer();
      container.GetType().GetProperty(nameof(ISimpleInjectorContainerInternals.Container))
               .Must().BeNull();
   }
}
