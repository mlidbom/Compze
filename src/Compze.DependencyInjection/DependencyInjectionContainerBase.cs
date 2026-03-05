using Compze.DependencyInjection.Abstractions;
using Compze.Utilities.SystemCE.LinqCE;

namespace Compze.DependencyInjection;

public abstract class DependencyInjectionContainerBase : IDependencyInjectionContainer
{
   readonly List<ComponentRegistration> _registeredComponents = [];
   readonly IComponentRegistrar _registrar;

   protected DependencyInjectionContainerBase(IComponentRegistrar? registrar)
   {
      _registrar = registrar ?? new ComponentRegistrar();
      _registrar.SetContainer(this);
   }

   public abstract void Dispose();
   public abstract ValueTask DisposeAsync();

   public IDependencyInjectionContainer Register(params ComponentRegistration[] registrations)
   {
      ValidateNoDuplicateRegistrations(registrations);
      _registeredComponents.AddRange(registrations);
      return RegisterInContainer(registrations);
   }

   public IComponentRegistrar Register() => _registrar;

   protected abstract IDependencyInjectionContainer RegisterInContainer(ComponentRegistration[] registrations);

   public IEnumerable<ComponentRegistration> RegisteredComponents() => _registeredComponents;

   public abstract IServiceLocator ServiceLocator { get; }

   void ValidateNoDuplicateRegistrations(ComponentRegistration[] newRegistrations)
   {
      foreach(var serviceType in newRegistrations.SelectMany(it => it.ServiceTypes))
      {
         _registeredComponents
           .Where(it => it.ServiceTypes.Contains(serviceType))
           .ForEach(_ => throw new InvalidOperationException($"Service type '{serviceType.FullName}' is already registered."));
      }
   }

   protected void AssertLifeStyleCombinationsAreValid()
   {
      _registeredComponents.Where(it => it.Lifestyle == Lifestyle.Singleton)
                           .ForEach(singleton =>
                            {
                               foreach(var dependencyType in singleton.DependencyTypes)
                               {
                                  _registeredComponents
                                    .Where(it => it.ProvidesService(dependencyType) && it.Lifestyle != Lifestyle.Singleton)
                                    .ForEach(invalidDependency => throw new InvalidLifeStyleCombinationException(singleton, invalidDependency, dependencyType));
                               }
                            });
   }
}