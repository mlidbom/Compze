using Compze.DependencyInjection.Abstractions;
using Compze.DependencyInjection.Autofac;
using Compze.Must;

using Compze.xUnitBDD;

namespace Compze.DependencyInjection.Specifications;

public class AutofacContainerInternalsTests
{
   [XF]
   public void Builder_implements_IAutofacBuilderInternals_explicitly()
   {
      var builder = new AutofacContainerBuilder();
      builder.Must().BeAssignableTo<IAutofacBuilderInternals>();
   }

   [XF]
   public void BuiltContainer_implements_IAutofacContainerInternals_explicitly()
   {
      using var container = new AutofacContainerBuilder().Build();
      container.Must().BeAssignableTo<IAutofacContainerInternals>();
   }

   [XF]
   public void Container_is_accessible_after_Build_is_called()
   {
      using var container = new AutofacContainerBuilder().Build();
      var internals = (IAutofacContainerInternals)container;
      internals.Container.Must().NotBeNull();
   }

   [XF]
   public void IAutofacBuilderInternals_members_are_not_accessible_without_cast()
   {
      var builder = new AutofacContainerBuilder();
      builder.GetType().GetProperty(nameof(IAutofacBuilderInternals.ContainerBuilder))
             .Must().BeNull();
   }

   [XF]
   public void IAutofacContainerInternals_members_are_not_accessible_without_cast()
   {
      using var container = new AutofacContainerBuilder().Build();
      container.GetType().GetProperty(nameof(IAutofacContainerInternals.Container))
               .Must().BeNull();
   }
}
