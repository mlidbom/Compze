using Compze.DependencyInjection.LightInject;
using Compze.Must;

using Compze.xUnitBDD;

namespace Compze.DependencyInjection.Specifications;

public class LightInjectContainerInternalsTests
{
   [XF]
   public void Builder_implements_ILightInjectBuilderInternals_explicitly()
   {
      var builder = new LightInjectContainerBuilder();
      builder.Must().BeAssignableTo<ILightInjectBuilderInternals>();
   }

   [XF]
   public void BuiltContainer_implements_ILightInjectContainerInternals_explicitly()
   {
      using var container = new LightInjectContainerBuilder().Build();
      container.Must().BeAssignableTo<ILightInjectContainerInternals>();
   }

   [XF]
   public void ServiceContainer_is_accessible_after_Build_is_called()
   {
      using var container = new LightInjectContainerBuilder().Build();
      var internals = (ILightInjectContainerInternals)container;
      internals.ServiceContainer.Must().NotBeNull();
   }
}
