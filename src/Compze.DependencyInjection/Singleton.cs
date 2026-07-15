using Compze.Internals.SystemCE.LinqCE;
// ReSharper disable UnusedMember.Global todo: test coverage and remove the suppression
namespace Compze.DependencyInjection;

public static class Singleton
{
   public static SingletonRegistrationWithoutInstantiationSpec<TService1> For<TService1, TService2, TService3, TService4, TService5, TService6, TService7, TService8, TService9>() where TService1 : class => For<TService1>(EnumerableCE.OfTypes<TService2, TService3, TService4, TService5, TService6, TService7, TService8, TService9>());
   public static SingletonRegistrationWithoutInstantiationSpec<TService1> For<TService1, TService2, TService3, TService4, TService5, TService6, TService7, TService8>() where TService1 : class => For<TService1>(EnumerableCE.OfTypes<TService2, TService3, TService4, TService5, TService6, TService7, TService8>());
   public static SingletonRegistrationWithoutInstantiationSpec<TService1> For<TService1, TService2, TService3, TService4, TService5, TService6, TService7>() where TService1 : class => For<TService1>(EnumerableCE.OfTypes<TService2, TService3, TService4, TService5, TService6, TService7>());
   public static SingletonRegistrationWithoutInstantiationSpec<TService1> For<TService1, TService2, TService3, TService4, TService5, TService6>() where TService1 : class => For<TService1>(EnumerableCE.OfTypes<TService2, TService3, TService4, TService5, TService6>());
   public static SingletonRegistrationWithoutInstantiationSpec<TService1> For<TService1, TService2, TService3, TService4, TService5>() where TService1 : class => For<TService1>(EnumerableCE.OfTypes<TService2, TService3, TService4, TService5>());
   public static SingletonRegistrationWithoutInstantiationSpec<TService1> For<TService1, TService2, TService3, TService4>() where TService1 : class => For<TService1>(EnumerableCE.OfTypes<TService2, TService3, TService4>());
   public static SingletonRegistrationWithoutInstantiationSpec<TService1> For<TService1, TService2, TService3>() where TService1 : class => For<TService1>(EnumerableCE.OfTypes<TService2, TService3>());
   public static SingletonRegistrationWithoutInstantiationSpec<TService1> For<TService1, TService2>() where TService1 : class => For<TService1>(EnumerableCE.OfTypes<TService2>());
   public static SingletonRegistrationWithoutInstantiationSpec<TService> For<TService>() where TService : class => For<TService>([]);
   static SingletonRegistrationWithoutInstantiationSpec<TService> For<TService>(IEnumerable<Type> additionalServices) where TService : class => new(additionalServices);

   /// <summary>
   /// Registers this component as a member of the <typeparamref name="TService"/> component set instead of a singularly
   /// resolvable service: many components may each call <c>ForSet&lt;TService&gt;()</c>, and every one of them is returned
   /// together by <see cref="IServiceResolverCE.ResolveSet{TComponent}(Abstractions.IServiceResolver)"/>.
   /// </summary>
   /// <remarks>
   /// <typeparamref name="TService"/> becomes exclusively a component-set type: it can never also be registered as a singular
   /// service (via <see cref="For{TService}()"/>), on this or any other registration in the container, and it can only be resolved
   /// through <c>ResolveSet&lt;TService&gt;()</c> — never through <c>Resolve&lt;TService&gt;()</c>.
   /// </remarks>
   public static SingletonRegistrationWithoutInstantiationSpec<TService> ForSet<TService>() where TService : class => new([], isComponentSetMember: true);
}
