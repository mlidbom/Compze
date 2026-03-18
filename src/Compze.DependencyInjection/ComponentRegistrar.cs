using Compze.Contracts;
using Compze.DependencyInjection.Abstractions;

namespace Compze.DependencyInjection;

public class ComponentRegistrar : IComponentRegistrar
{
   ILegacyContainer? _container = null;

   public void SetContainer(ILegacyContainer container)
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

   public ILegacyContainer Container()
   {
      if(_container == null) throw new InvalidOperationException("Container has not been set yet");
      return _container;
   }

   public virtual TTestingRegistrar? TryGetTestingRegistrar<TTestingRegistrar>() where TTestingRegistrar : class => null;
}
