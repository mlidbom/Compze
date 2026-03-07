using Compze.DependencyInjection.Abstractions;
using Compze.Internals.SystemCE.LinqCE;
// ReSharper disable UnusedMember.Global
namespace Compze.DependencyInjection;

public static class UntrackedTransient
{
   public static ComponentRegistrationWithoutInstantiationSpec<TService1> For<TService1, TService2, TService3, TService4, TService5, TService6, TService7, TService8, TService9>() where TService1 : class => For<TService1>(EnumerableCE.OfTypes<TService2, TService3, TService4, TService5, TService6, TService7, TService8, TService9>());
   public static ComponentRegistrationWithoutInstantiationSpec<TService1> For<TService1, TService2, TService3, TService4, TService5, TService6, TService7, TService8>() where TService1 : class => For<TService1>(EnumerableCE.OfTypes<TService2, TService3, TService4, TService5, TService6, TService7, TService8>());
   public static ComponentRegistrationWithoutInstantiationSpec<TService1> For<TService1, TService2, TService3, TService4, TService5, TService6, TService7>() where TService1 : class => For<TService1>(EnumerableCE.OfTypes<TService2, TService3, TService4, TService5, TService6, TService7>());
   public static ComponentRegistrationWithoutInstantiationSpec<TService1> For<TService1, TService2, TService3, TService4, TService5, TService6>() where TService1 : class => For<TService1>(EnumerableCE.OfTypes<TService2, TService3, TService4, TService5, TService6>());
   public static ComponentRegistrationWithoutInstantiationSpec<TService1> For<TService1, TService2, TService3, TService4, TService5>() where TService1 : class => For<TService1>(EnumerableCE.OfTypes<TService2, TService3, TService4, TService5>());
   public static ComponentRegistrationWithoutInstantiationSpec<TService1> For<TService1, TService2, TService3, TService4>() where TService1 : class => For<TService1>(EnumerableCE.OfTypes<TService2, TService3, TService4>());
   public static ComponentRegistrationWithoutInstantiationSpec<TService1> For<TService1, TService2, TService3>() where TService1 : class => For<TService1>(EnumerableCE.OfTypes<TService2, TService3>());
   public static ComponentRegistrationWithoutInstantiationSpec<TService1> For<TService1, TService2>() where TService1 : class => For<TService1>(EnumerableCE.OfTypes<TService2>());
   public static ComponentRegistrationWithoutInstantiationSpec<TService> For<TService>() where TService : class => For<TService>([]);
   static ComponentRegistrationWithoutInstantiationSpec<TService> For<TService>(IEnumerable<Type> additionalServices) where TService : class => new(Lifestyle.UntrackedTransient, additionalServices);
}
