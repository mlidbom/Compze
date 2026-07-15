using Compze.DependencyInjection.Abstractions;
using Compze.DependencyInjection.Microsoft;
using Compze.Must;

using Compze.xUnitBDD;

namespace Compze.DependencyInjection.Specifications;

public class MicrosoftContainerInternalsTests
{
   [XF]
   public void Builder_implements_IMicrosoftBuilderInternals_explicitly()
   {
      var builder = new MicrosoftContainerBuilder();
      builder.Must().BeAssignableTo<IMicrosoftBuilderInternals>();
   }

   [XF]
   public void ServiceCollection_is_accessible_via_explicit_interface_cast()
   {
      var builder = new MicrosoftContainerBuilder();
      var internals = (IMicrosoftBuilderInternals)builder;
      internals.ServiceCollection.Must().NotBeNull();
   }

   [XF]
   public void BuiltContainer_implements_IMicrosoftContainerInternals_explicitly()
   {
      using var container = new MicrosoftContainerBuilder().Build();
      container.Must().BeAssignableTo<IMicrosoftContainerInternals>();
   }

   [XF]
   public void ServiceProvider_is_accessible_after_Build_is_called()
   {
      using var container = new MicrosoftContainerBuilder().Build();
      var internals = (IMicrosoftContainerInternals)container;
      internals.ServiceProvider.Must().NotBeNull();
   }

   [XF]
   public void IMicrosoftBuilderInternals_members_are_not_accessible_without_cast()
   {
      var builder = new MicrosoftContainerBuilder();
      builder.GetType().GetProperty(nameof(IMicrosoftBuilderInternals.ServiceCollection))
             .Must().BeNull();
   }

   [XF]
   public void IMicrosoftContainerInternals_members_are_not_accessible_without_cast()
   {
      using var container = new MicrosoftContainerBuilder().Build();
      container.GetType().GetProperty(nameof(IMicrosoftContainerInternals.ServiceProvider))
               .Must().BeNull();
   }
}
