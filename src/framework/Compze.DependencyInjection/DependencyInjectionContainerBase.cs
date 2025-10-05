using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Compze.DependencyInjection;

/// <summary>
/// Abstract base class for dependency injection container implementations.
/// Implements the Template Method pattern to ensure validation is always performed before registration.
/// </summary>
public abstract class DependencyInjectionContainerBase : IDependencyInjectionContainer
{
   readonly List<ComponentRegistration> _registeredComponents = [];

   protected DependencyInjectionContainerBase(IRunMode runMode) => RunMode = runMode;

   public IRunMode RunMode { get; }

   /// <summary>
   /// Template method that validates and registers components.
   /// Ensures validation always happens before calling the container-specific registration logic.
   /// </summary>
   public void Register(params ComponentRegistration[] registrations)
   {
      ValidateNoDuplicateRegistrations(registrations);
      _registeredComponents.AddRange(registrations);
      RegisterInContainer(registrations);
   }

   /// <summary>
   /// Container-specific registration logic. Called after validation and tracking.
   /// </summary>
   protected abstract void RegisterInContainer(ComponentRegistration[] registrations);

   public IEnumerable<ComponentRegistration> RegisteredComponents() => _registeredComponents;

   public abstract IServiceLocator ServiceLocator { get; }

   public abstract void Dispose();

   public abstract ValueTask DisposeAsync();

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
