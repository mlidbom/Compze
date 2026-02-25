using System;
using Compze.Contracts;
using Compze.Utilities.DependencyInjection.Abstractions;

namespace Compze.Utilities.DependencyInjection;

public class ComponentRegistrar : IComponentRegistrar
{
   IDependencyInjectionContainer? _container = null;

   public void SetContainer(IDependencyInjectionContainer container)
   {
      Contract.State.Fulfills(_container == null, () => "Container has already been set");
      _container = container;
   }

   public virtual IComponentRegistrar Clone() => new ComponentRegistrar();

   public IComponentRegistrar Register(params ComponentRegistration[] registrations)
   {
      if(_container == null) throw new InvalidOperationException("Container has not been set yet");
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

   public IDependencyInjectionContainer Container()
   {
      if(_container == null) throw new InvalidOperationException("Container has not been set yet");
      return _container;
   }

   public virtual TTestingRegistrar? TryGetTestingRegistrar<TTestingRegistrar>() where TTestingRegistrar : class => null;
}
