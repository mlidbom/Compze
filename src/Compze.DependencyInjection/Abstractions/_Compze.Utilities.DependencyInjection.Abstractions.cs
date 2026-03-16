using Compze.Underscore;
using Compze.Internals.SystemCE.LinqCE;

namespace Compze.DependencyInjection.Abstractions;

//todo: rather than passing an untyped IRunMode around and using it to make decisions.
// Subtypes of IComponentRegistrar should be making the decisions, and tests should supply a different IComponentRegistrar than production code.
// The test version of the registrar would know about TestEnv, none of the production code should need any testing references or DbPool references etc.
public interface IComponentRegistrar
{
   IComponentRegistrar Register(params ComponentRegistration[] registrations);

   IComponentRegistrar Register(params Action<IComponentRegistrar>[] registrationMethods)
      => registrationMethods.ForEach(it => it(this))
                            .__(this);

   IDependencyInjectionContainer Container();

   TTestingRegistrar? TryGetTestingRegistrar<TTestingRegistrar>() where TTestingRegistrar : class;

   void SetContainer(IDependencyInjectionContainer container);
   IComponentRegistrar Clone();
}

public interface IDependencyInjectionContainer : IDisposable, IAsyncDisposable
{
   IComponentRegistrar Register();
   IDependencyInjectionContainer Register(params ComponentRegistration[] registrations);
   IEnumerable<ComponentRegistration> RegisteredComponents();
   IServiceLocator ServiceLocator { get; }
   bool IsClone { get; }
   IDependencyInjectionContainer Clone();
   bool IsRegistered<TComponent>() where TComponent : class => RegisteredComponents().Any(it => it.ServiceTypes.Contains(typeof(TComponent)));
}

public interface IServiceLocator : IDisposable, IAsyncDisposable
{
   TComponent Resolve<TComponent>() where TComponent : class;
   object Resolve(Type serviceType);
   IServiceLocatorScope BeginScope();
}

public interface IServiceLocatorScope : IDisposable
{
   TComponent Resolve<TComponent>() where TComponent : class;
   object Resolve(Type serviceType);
}

public enum Lifestyle
{
   Singleton,
   Scoped,
   TrackedTransient,
   Transient
}
