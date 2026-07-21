using Compze.DependencyInjection.Abstractions;
using Compze.DependencyInjection.Specifications.Infrastructure;
using Compze.Must;

namespace Compze.DependencyInjection.Specifications.Associated_registrations;

interface IFeatureDecoratedService;
class FeatureDecoratedService : IFeatureDecoratedService;

///<summary>What the feature extension's companion learned about the registration it decorated — proof the deferred hook
/// received the finished <see cref="ComponentRegistration"/>.</summary>
class CompanionRecordingTheDecoratedRegistration(IReadOnlySet<Type> serviceTypes, Lifestyle lifestyle)
{
   public IReadOnlySet<Type> DecoratedServiceTypes { get; } = serviceTypes;
   public Lifestyle DecoratedLifestyle { get; } = lifestyle;
}

///<summary>A consumer-written feature extension built on the deferred <c>WithAssociatedRegistrations</c> overload — the
/// extension point its documentation promises: derive companion registrations from the finished registration without the
/// core knowing the feature exists.</summary>
static class WithCompanionFeatureExtension
{
   extension<TSpec>(TSpec @this) where TSpec : ComponentRegistrationWithoutInstantiationSpec
   {
      public TSpec WithCompanionRecordingTheDecoratedRegistration() =>
         @this.WithAssociatedRegistrations(builtRegistration =>
            [Singleton.For<CompanionRecordingTheDecoratedRegistration>().CreatedBy(() => new CompanionRecordingTheDecoratedRegistration(builtRegistration.ServiceTypes, builtRegistration.Lifestyle))]);
   }
}

public class When_a_consumer_written_feature_extension_attaches_registrations_through_the_deferred_hook
{
   [DependencyInjectionContainerMatrix]
   public void the_companion_registration_joins_the_container_and_received_the_finished_registration_it_decorated()
   {
      var builder = DependencyInjectionContainerFactory.CreateContainerBuilder();
      builder.Registrar.Register(
         Singleton.For<IFeatureDecoratedService>()
                  .WithCompanionRecordingTheDecoratedRegistration()
                  .CreatedBy(() => new FeatureDecoratedService()));

      using var container = builder.Build();

      var companion = container.Resolve<CompanionRecordingTheDecoratedRegistration>();
      companion.DecoratedServiceTypes.Contains(typeof(IFeatureDecoratedService)).Must().BeTrue();
      companion.DecoratedLifestyle.Must().Be(Lifestyle.Singleton);
   }
}
