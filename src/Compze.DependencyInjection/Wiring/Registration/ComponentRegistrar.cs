using Compze.Contracts;

namespace Compze.DependencyInjection.Wiring.Registration;

public class ComponentRegistrar : IComponentRegistrar
{
   ContainerBuilder? _builder = null;

   internal void SetBuilder(ContainerBuilder builder)
   {
      Contract.State.Assert(_builder == null, () => "Builder has already been set");
      _builder = builder;
   }

   public virtual IComponentRegistrar Clone() => new ComponentRegistrar();

   public IComponentRegistrar Register(params ComponentRegistration[] registrations)
   {
      if(_builder == null) throw new InvalidOperationException("Builder has not been set yet");
      _builder.Register(registrations);
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

   public bool IsClone => _builder?.IsClone ?? false;

   public bool IsRegistered<TComponent>() where TComponent : class =>
      _builder?.RegisteredComponents().Any(it => it.ServiceTypes.Contains(typeof(TComponent))) ?? false;

   public virtual TTestingRegistrar? TryGetTestingRegistrar<TTestingRegistrar>() where TTestingRegistrar : class => null;
}
