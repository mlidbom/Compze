using Compze.DependencyInjection.Abstractions;
using Compze.Internals.Logging;
using Compze.Internals.SystemCE.LinqCE;

namespace Compze.DependencyInjection;

public abstract partial class DependencyInjectionContainer : IContainerBuilder, IDependencyInjectionContainer
{
   static readonly ILogger Log = CompzeLogger.For<DependencyInjectionContainer>();

   readonly List<ComponentRegistration> _registeredComponents = [];
   readonly IComponentRegistrar _registrar;

   public bool IsClone { get; private set; }

   protected DependencyInjectionContainer(IComponentRegistrar? registrar)
   {
      _registrar = registrar ?? new ComponentRegistrar();
      ((ComponentRegistrar)_registrar).SetContainer(this);
   }

   IComponentRegistrar IContainerBuilder.Registrar => _registrar;

   IDependencyInjectionContainer IContainerBuilder.Build()
   {
      EnsureContainerBuilt();
      return this;
   }

   IRootResolver IDependencyInjectionContainer.RootResolver
   {
      get
      {
         EnsureContainerBuilt();
         return (IRootResolver)this;
      }
   }

   IScopeFactory IDependencyInjectionContainer.ScopeFactory
   {
      get
      {
         EnsureContainerBuilt();
         return (IScopeFactory)this;
      }
   }

   public abstract void Dispose();
   public abstract ValueTask DisposeAsync();

   protected virtual IReadOnlyList<Type> ContainerFacadeServiceTypes { get; } = [];

   protected abstract DependencyInjectionContainer CreateEmptyClone();

   DependencyInjectionContainer CloneInternal()
   {
      Log.Info($"Cloning IDependencyInjectionContainer: {GetHashCode()}");
      EnsureContainerBuilt();
      IRootResolver sourceRootResolver = (IRootResolver)this;
      var cloneContainer = CreateEmptyClone();
      cloneContainer.IsClone = true;

      RegisteredComponents()
        .Where(component => ContainerFacadeServiceTypes.None(facadeType => component.ServiceTypes.Contains(facadeType)))
        .ForEach(action: registration => cloneContainer.Register(registration.CreateCloneRegistration(sourceRootResolver)));

      return cloneContainer;
   }

   IContainerBuilder IDependencyInjectionContainer.Clone() => CloneInternal();
   IContainerBuilder IContainerBuilder.Clone() => CloneInternal();

   internal void Register(params ComponentRegistration[] registrations)
   {
      ValidateNoDuplicateRegistrations(registrations);
      _registeredComponents.AddRange(registrations);
      RegisterInContainer(registrations);
   }

   public IComponentRegistrar Register() => _registrar;

   protected abstract void RegisterInContainer(ComponentRegistration[] registrations);

   public IEnumerable<ComponentRegistration> RegisteredComponents() => _registeredComponents;

   protected abstract void EnsureContainerBuilt();

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
         return dependency.Lifestyle is Lifestyle.TrackedTransient
                && !dependency.AllowScopedDependent;

      return false;
   }
}