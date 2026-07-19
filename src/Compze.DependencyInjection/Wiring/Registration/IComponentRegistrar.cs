using Compze.Internals.SystemCE.LinqCE;
using Compze.Underscore;

namespace Compze.DependencyInjection.Wiring.Registration;

public interface IComponentRegistrar
{
   IComponentRegistrar Register(params ComponentRegistration[] registrations);

   IComponentRegistrar Register(params Action<IComponentRegistrar>[] registrationMethods)
      => registrationMethods.ForEach(it => it(this))
                            .__(this);

   bool IsClone { get; }
   bool IsRegistered<TComponent>() where TComponent : class;

   TTestingRegistrar? TryGetTestingRegistrar<TTestingRegistrar>() where TTestingRegistrar : class;

   IComponentRegistrar Clone();
}
