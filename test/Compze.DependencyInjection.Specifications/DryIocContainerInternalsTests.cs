using Compze.DependencyInjection.DryIoc;
using Compze.Must;

using Compze.xUnitBDD;

namespace Compze.DependencyInjection.Specifications;

public class DryIocContainerInternalsTests
{
   [XF]
   public void Builder_implements_IDryIocBuilderInternals_explicitly()
   {
      var builder = new DryIocContainerBuilder();
      builder.Must().BeAssignableTo<IDryIocBuilderInternals>();
   }

   [XF]
   public void BuiltContainer_implements_IDryIocContainerInternals_explicitly()
   {
      using var container = new DryIocContainerBuilder().Build();
      container.Must().BeAssignableTo<IDryIocContainerInternals>();
   }

   [XF]
   public void Container_is_accessible_after_Build_is_called()
   {
      using var container = new DryIocContainerBuilder().Build();
      var internals = (IDryIocContainerInternals)container;
      internals.Container.Must().NotBeNull();
   }
}
