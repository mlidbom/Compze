using Compze.Utilities.DependencyInjection.Abstractions;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Compze.Utilities.SystemCE.LinqCE;

namespace Compze.Utilities.DependencyInjection;

public abstract class DependencyInjectionContainerBase : IDependencyInjectionContainer
{
   readonly List<ComponentRegistration> _registeredComponents = [];

   protected DependencyInjectionContainerBase(IRunMode runMode) => RunMode = runMode;

   public IRunMode RunMode { get; }

   public abstract void Dispose();
   public abstract ValueTask DisposeAsync();

   public IDependencyInjectionContainer Register(params ComponentRegistration[] registrations)
   {
      ValidateNoDuplicateRegistrations(registrations);
      _registeredComponents.AddRange(registrations);
      AssertLifeStyleCombinationsAreValid();
      return RegisterInContainer(registrations);
   }

   public IDependencyRegistrar Register() => new DependencyRegistrar(this);

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

   void AssertLifeStyleCombinationsAreValid()
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

class DependencyRegistrar(IDependencyInjectionContainer container) : IDependencyRegistrar
{
   readonly IDependencyInjectionContainer _container = container;

   public IDependencyRegistrar Register(params ComponentRegistration[] registrations)
   {
      _container.Register(registrations);
      return this;
   }

   public IDependencyRegistrar Register(params Action<IDependencyRegistrar>[] registrationMethods)
   {
      foreach(var registrationMethod in registrationMethods)
      {
         registrationMethod(this);
      }

      return this;
   }

   public IDependencyInjectionContainer Container() => _container;

   public IRunMode RunMode => _container.RunMode;
}
