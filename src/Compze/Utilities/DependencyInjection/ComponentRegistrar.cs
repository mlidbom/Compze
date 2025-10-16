using System;
using Compze.Utilities.DependencyInjection.Abstractions;

namespace Compze.Utilities.DependencyInjection;

class ComponentRegistrar(IDependencyInjectionContainer container) : IComponentRegistrar
{
   readonly IDependencyInjectionContainer _container = container;

   public IComponentRegistrar Register(params ComponentRegistration[] registrations)
   {
      _container.Register(registrations);
      return this;
   }

   public IComponentRegistrar Register(params Action<IComponentRegistrar>[] registrationMethods)
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
