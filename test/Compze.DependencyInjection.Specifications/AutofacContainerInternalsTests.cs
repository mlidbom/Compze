using Compze.DependencyInjection.Abstractions;
using Compze.DependencyInjection.Autofac;
using Compze.Must;
using Compze.xUnitBDD;

namespace Compze.DependencyInjection.Specifications;

public class AutofacContainerInternalsTests
{
   [XF]
   public void Container_implements_IAutofacContainerInternals_explicitly()
   {
      using var container = new AutofacDependencyInjectionContainer();
      container.Must().BeAssignableTo<IAutofacContainerInternals>();
   }

   [XF]
   public void LifetimeScope_is_accessible_after_Build_is_called()
   {
      using var container = new AutofacDependencyInjectionContainer();
      _ = ((IContainerBuilder)container).Build();
      var internals = (IAutofacContainerInternals)container;
      internals.Container.Must().NotBeNull();
   }

   [XF]
   public void IAutofacContainerInternals_members_are_not_accessible_without_cast()
   {
      using var container = new AutofacDependencyInjectionContainer();
      container.GetType().GetProperty(nameof(IAutofacContainerInternals.Container))
               .Must().BeNull();
   }
}
