using Compze.Contracts;
using Compze.DependencyInjection.Abstractions;

namespace Compze.DependencyInjection;

public class ComponentRegistrar : IComponentRegistrar
{
   DependencyInjectionContainer? _container = null;

   internal void SetContainer(DependencyInjectionContainer container)
   {
      Contract.State.Assert(_container == null, () => "Container has already been set");
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

   public bool IsClone => _container?.IsClone ?? false;

   public bool IsRegistered<TComponent>() where TComponent : class =>
      _container?.RegisteredComponents().Any(it => it.ServiceTypes.Contains(typeof(TComponent))) ?? false;

   public virtual TTestingRegistrar? TryGetTestingRegistrar<TTestingRegistrar>() where TTestingRegistrar : class => null;
}
