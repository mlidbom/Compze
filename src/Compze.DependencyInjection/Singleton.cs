using Compze.Utilities.SystemCE.LinqCE;
//todo: test coverage and remove the suppression
// ReSharper disable UnusedMember.Global
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
}