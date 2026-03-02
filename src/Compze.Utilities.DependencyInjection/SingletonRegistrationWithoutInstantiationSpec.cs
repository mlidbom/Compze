using System;
using System.Collections.Generic;
using Compze.Utilities.DependencyInjection.Abstractions;

namespace Compze.Utilities.DependencyInjection;

public class SingletonRegistrationWithoutInstantiationSpec<TService> : ComponentRegistrationWithoutInstantiationSpec<TService> where TService : class
{
   internal SingletonRegistrationWithoutInstantiationSpec(IEnumerable<Type> serviceTypes) : base(Lifestyle.Singleton, serviceTypes) {}

   public ComponentRegistration<TService> Instance(TService instance)
   {
      AssertImplementsAllServices(instance.GetType());
      return new ComponentRegistration<TService>(Lifestyle.Singleton, ServiceTypes, InstantiationSpec.FromInstance(instance), dependencyTypes: []);
   }
}
