using Compze.Utilities.DependencyInjection.Abstractions;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;

namespace Compze.Utilities.DependencyInjection;

/// <summary>
/// Abstract base class for dependency injection container implementations.
/// Implements the Template Method pattern to ensure validation is always performed before registration.
/// </summary>
[SuppressMessage("Design", "CA1063:Implement IDisposable Correctly", Justification = "CA1063 analyzer does not recognize the pattern when both IDisposable and IAsyncDisposable are implemented. It expects only Dispose(bool) but fails to account for DisposeAsyncCore() being the async equivalent. Pattern follows https://learn.microsoft.com/en-us/dotnet/standard/garbage-collection/implementing-disposeasync")]
public abstract class DependencyInjectionContainerBase : IDependencyInjectionContainer
{
   readonly List<ComponentRegistration> _registeredComponents = [];

   protected DependencyInjectionContainerBase(IRunMode runMode) => RunMode = runMode;

   public IRunMode RunMode { get; }

   /// <summary>
   /// Template method that validates and registers components.
   /// Ensures validation always happens before calling the container-specific registration logic.
   /// </summary>
   public IDependencyInjectionContainer Register(params ComponentRegistration[] registrations)
   {
      ValidateNoDuplicateRegistrations(registrations);
      _registeredComponents.AddRange(registrations);
      return RegisterInContainer(registrations);
   }

   public IDependencyRegistrar Register() => new DependencyRegistrar(this);

    /// <summary>
    /// Container-specific registration logic. Called after validation and tracking.
    /// </summary>
    protected abstract IDependencyInjectionContainer RegisterInContainer(ComponentRegistration[] registrations);

   public IEnumerable<ComponentRegistration> RegisteredComponents() => _registeredComponents;

   public abstract IServiceLocator ServiceLocator { get; }

   [SuppressMessage("Design", "CA1063:Implement IDisposable Correctly", Justification = "CA1063 expects Dispose() to be sealed in non-sealed classes, but this is incompatible with the abstract base class pattern where derived classes override Dispose(bool), not Dispose(). The analyzer doesn't distinguish between these scenarios.")]
   public void Dispose()
   {
      Dispose(disposing: true);
      GC.SuppressFinalize(this);
   }

   public async ValueTask DisposeAsync()
   {
      await DisposeAsyncCore().ConfigureAwait(false);
      Dispose(disposing: false);
      GC.SuppressFinalize(this);
   }

   /// <summary>
   /// Releases the unmanaged resources used by the container and optionally releases the managed resources.
   /// </summary>
   /// <param name="disposing">true to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
   protected virtual void Dispose(bool disposing)
   {
      // Base implementation - inheritors should override to dispose their resources
   }

   /// <summary>
   /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources asynchronously.
   /// </summary>
   protected virtual ValueTask DisposeAsyncCore()
   {
      // Base implementation - inheritors should override to dispose their resources asynchronously
      return default;
   }

   void ValidateNoDuplicateRegistrations(ComponentRegistration[] newRegistrations)
   {
      foreach(var newRegistration in newRegistrations)
      {
         foreach(var serviceType in newRegistration.ServiceTypes)
         {
            var existingRegistration = _registeredComponents
               .FirstOrDefault(existing => existing.ServiceTypes.Contains(serviceType));

            if(existingRegistration != null)
            {
               throw new InvalidOperationException(
                  $"Service type '{serviceType.FullName}' is already registered. " +
                  $"Existing registration includes service types: [{string.Join(", ", existingRegistration.ServiceTypes.Select(t => t.Name))}]. " +
                  $"Attempted duplicate registration includes service types: [{string.Join(", ", newRegistration.ServiceTypes.Select(t => t.Name))}].");
            }
         }
      }
   }
}

internal class DependencyRegistrar(IDependencyInjectionContainer container) : IDependencyRegistrar
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
