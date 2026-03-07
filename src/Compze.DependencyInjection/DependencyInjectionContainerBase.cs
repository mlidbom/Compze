using System.Diagnostics.CodeAnalysis;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.SystemCE.LinqCE;
using Compze.Threading;

namespace Compze.DependencyInjection;

public abstract class DependencyInjectionContainerBase : IDependencyInjectionContainer
{
   readonly List<ComponentRegistration> _registeredComponents = [];
   readonly Dictionary<Type, ComponentRegistration> _transientRegistrations = new();
   readonly IComponentRegistrar _registrar;
   readonly RunOnce _registerTransientInstanceTrackers = new();

   protected DependencyInjectionContainerBase(IComponentRegistrar? registrar)
   {
      _registrar = registrar ?? new ComponentRegistrar();
      _registrar.SetContainer(this);
   }

   public abstract void Dispose();
   public abstract ValueTask DisposeAsync();

   public IDependencyInjectionContainer Register(params ComponentRegistration[] registrations)
   {
      _registerTransientInstanceTrackers.RunIfFirstCall(() =>
         RegisterInContainer(
         [
            Scoped.For<ScopedTransientInstanceTracker>().CreatedBy(() => new ScopedTransientInstanceTracker()),
            Singleton.For<SingletonTransientInstanceTracker>().CreatedBy(() => new SingletonTransientInstanceTracker())
         ]));

      ValidateNoDuplicateRegistrations(registrations);
      _registeredComponents.AddRange(registrations);

      var containerRegistrations = registrations.Where(it => it.Lifestyle is not (Lifestyle.TrackedTransient or Lifestyle.Transient)).ToArray();
      if(containerRegistrations.Length > 0)
         RegisterInContainer(containerRegistrations);

      foreach(var registration in registrations.Where(it => it.Lifestyle is Lifestyle.TrackedTransient or Lifestyle.Transient))
      {
         foreach(var serviceType in registration.ServiceTypes)
            _transientRegistrations[serviceType] = registration;
      }

      return this;
   }

   protected bool TryCreateTransientInstance(Type serviceType, IServiceLocatorKernel kernel, [NotNullWhen(true)] out object? instance)
   {
      if(_transientRegistrations.TryGetValue(serviceType, out var registration))
      {
         instance = registration.InstantiationSpec.RunFactoryMethod(kernel);
         if(registration.Lifestyle == Lifestyle.TrackedTransient)
         {
            TransientInstanceTracker tracker = IsInScope()
               ? kernel.Resolve<ScopedTransientInstanceTracker>()
               : kernel.Resolve<SingletonTransientInstanceTracker>();
            tracker.Track(instance);
         }
         return true;
      }
      instance = null;
      return false;
   }

   protected abstract bool IsInScope();

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
      _registeredComponents.ForEach(consumer =>
      {
         foreach(var dependencyType in consumer.DependencyTypes)
         {
            _registeredComponents
              .Where(it => it.ProvidesService(dependencyType))
              .Where(dependency => IsInvalidLifestyleCombination(consumer, dependency))
              .ForEach(invalidDependency => throw new InvalidLifeStyleCombinationException(consumer, invalidDependency, dependencyType));
         }
      });
   }

   static bool IsInvalidLifestyleCombination(ComponentRegistration consumer, ComponentRegistration dependency)
   {
      if(consumer.Lifestyle == Lifestyle.Singleton)
         return dependency.Lifestyle switch
         {
            Lifestyle.Singleton => false,
            Lifestyle.Scoped => true,
            _ => !dependency.AllowSingletonDependent
         };

      if(consumer.Lifestyle == Lifestyle.Scoped)
         return dependency.Lifestyle is Lifestyle.TrackedTransient or Lifestyle.Transient
                && !dependency.AllowScopedDependent;

      if(consumer.Lifestyle is Lifestyle.TrackedTransient or Lifestyle.Transient)
         return dependency.Lifestyle == Lifestyle.Scoped;

      return false;
   }
}