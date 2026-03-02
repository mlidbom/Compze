using System;

namespace Compze.Utilities.DependencyInjection;

public static class ComponentRegistrationExtensions
{
   public class ComponentPromise<TService> where TService : class
   {
      internal TService Resolve(IServiceLocatorKernel kernel) =>
         kernel.Resolve<TService>();
   }

   public static ComponentRegistration<TService> CreatedBy<TService, TImplementation>(
      this ComponentRegistrationWithoutInstantiationSpec<TService> @this,
      Func<TImplementation> factoryMethod) where TService : class
                                           where TImplementation : TService =>
      @this.CreatedBy(_ => factoryMethod(), []);

   public static ComponentRegistration<TService> CreatedBy<TService, TImplementation, TDependency1>(
      this ComponentRegistrationWithoutInstantiationSpec<TService> @this,
      Func<TDependency1, TImplementation> factoryMethod) where TService : class
                                                         where TDependency1 : class
                                                         where TImplementation : TService
   {
      var dependency1 = new ComponentPromise<TDependency1>();
      return @this.CreatedBy(
         kern => factoryMethod(dependency1.Resolve(kern)),
         [typeof(TDependency1)]);
   }

   public static ComponentRegistration<TService> CreatedBy<TService, TImplementation, TDependency1, TDependency2>(
      this ComponentRegistrationWithoutInstantiationSpec<TService> @this,
      Func<TDependency1, TDependency2, TImplementation> factoryMethod) where TService : class
                                                                       where TDependency1 : class
                                                                       where TDependency2 : class
                                                                       where TImplementation : TService
   {
      var dependency1 = new ComponentPromise<TDependency1>();
      var dependency2 = new ComponentPromise<TDependency2>();
      return @this.CreatedBy(
         kern => factoryMethod(dependency1.Resolve(kern), dependency2.Resolve(kern)),
         [typeof(TDependency1), typeof(TDependency2)]);
   }

   public static ComponentRegistration<TService> CreatedBy<TService, TImplementation, TDependency1, TDependency2, TDependency3>(
      this ComponentRegistrationWithoutInstantiationSpec<TService> @this,
      Func<TDependency1, TDependency2, TDependency3, TImplementation> factoryMethod) where TImplementation : TService
                                                                                     where TService : class
                                                                                     where TDependency1 : class
                                                                                     where TDependency2 : class
                                                                                     where TDependency3 : class
   {
      var dependency1 = new ComponentPromise<TDependency1>();
      var dependency2 = new ComponentPromise<TDependency2>();
      var dependency3 = new ComponentPromise<TDependency3>();
      return @this.CreatedBy(
         kern => factoryMethod(dependency1.Resolve(kern), dependency2.Resolve(kern), dependency3.Resolve(kern)),
         [typeof(TDependency1), typeof(TDependency2), typeof(TDependency3)]);
   }

   public static ComponentRegistration<TService> CreatedBy<TService, TImplementation, TDependency1, TDependency2, TDependency3, TDependency4>(
      this ComponentRegistrationWithoutInstantiationSpec<TService> @this,
      Func<TDependency1, TDependency2, TDependency3, TDependency4, TImplementation> factoryMethod) where TImplementation : TService
                                                                                                   where TService : class
                                                                                                   where TDependency1 : class
                                                                                                   where TDependency2 : class
                                                                                                   where TDependency3 : class
                                                                                                   where TDependency4 : class
   {
      var dependency1 = new ComponentPromise<TDependency1>();
      var dependency2 = new ComponentPromise<TDependency2>();
      var dependency3 = new ComponentPromise<TDependency3>();
      var dependency4 = new ComponentPromise<TDependency4>();
      return @this.CreatedBy(
         kern => factoryMethod(dependency1.Resolve(kern), dependency2.Resolve(kern), dependency3.Resolve(kern), dependency4.Resolve(kern)),
         [typeof(TDependency1), typeof(TDependency2), typeof(TDependency3), typeof(TDependency4)]);
   }

   public static ComponentRegistration<TService> CreatedBy<TService, TImplementation, TDependency1, TDependency2, TDependency3, TDependency4, TDependency5>(
      this ComponentRegistrationWithoutInstantiationSpec<TService> @this,
      Func<TDependency1, TDependency2, TDependency3, TDependency4, TDependency5, TImplementation> factoryMethod) where TImplementation : TService
                                                                                                                 where TService : class
                                                                                                                 where TDependency1 : class
                                                                                                                 where TDependency2 : class
                                                                                                                 where TDependency3 : class
                                                                                                                 where TDependency4 : class
                                                                                                                 where TDependency5 : class
   {
      var dependency1 = new ComponentPromise<TDependency1>();
      var dependency2 = new ComponentPromise<TDependency2>();
      var dependency3 = new ComponentPromise<TDependency3>();
      var dependency4 = new ComponentPromise<TDependency4>();
      var dependency5 = new ComponentPromise<TDependency5>();
      return @this.CreatedBy(
         kern => factoryMethod(dependency1.Resolve(kern), dependency2.Resolve(kern), dependency3.Resolve(kern), dependency4.Resolve(kern), dependency5.Resolve(kern)),
         [typeof(TDependency1), typeof(TDependency2), typeof(TDependency3), typeof(TDependency4), typeof(TDependency5)]);
   }

   public static ComponentRegistration<TService> CreatedBy<TService, TImplementation, TDependency1, TDependency2, TDependency3, TDependency4, TDependency5, TDependency6>(
      this ComponentRegistrationWithoutInstantiationSpec<TService> @this,
      Func<TDependency1, TDependency2, TDependency3, TDependency4, TDependency5, TDependency6, TImplementation> factoryMethod) where TImplementation : TService
                                                                                                                               where TService : class
                                                                                                                               where TDependency1 : class
                                                                                                                               where TDependency2 : class
                                                                                                                               where TDependency3 : class
                                                                                                                               where TDependency4 : class
                                                                                                                               where TDependency5 : class
                                                                                                                               where TDependency6 : class
   {
      var dependency1 = new ComponentPromise<TDependency1>();
      var dependency2 = new ComponentPromise<TDependency2>();
      var dependency3 = new ComponentPromise<TDependency3>();
      var dependency4 = new ComponentPromise<TDependency4>();
      var dependency5 = new ComponentPromise<TDependency5>();
      var dependency6 = new ComponentPromise<TDependency6>();
      return @this.CreatedBy(
         kern => factoryMethod(dependency1.Resolve(kern), dependency2.Resolve(kern), dependency3.Resolve(kern), dependency4.Resolve(kern), dependency5.Resolve(kern), dependency6.Resolve(kern)),
         [typeof(TDependency1), typeof(TDependency2), typeof(TDependency3), typeof(TDependency4), typeof(TDependency5), typeof(TDependency6)]);
   }

   public static ComponentRegistration<TService> CreatedBy<TService, TImplementation, TDependency1, TDependency2, TDependency3, TDependency4, TDependency5, TDependency6, TDependency7>(
      this ComponentRegistrationWithoutInstantiationSpec<TService> @this,
      Func<TDependency1, TDependency2, TDependency3, TDependency4, TDependency5, TDependency6, TDependency7, TImplementation> factoryMethod) where TImplementation : TService
                                                                                                                                             where TService : class
                                                                                                                                             where TDependency1 : class
                                                                                                                                             where TDependency2 : class
                                                                                                                                             where TDependency3 : class
                                                                                                                                             where TDependency4 : class
                                                                                                                                             where TDependency5 : class
                                                                                                                                             where TDependency6 : class
                                                                                                                                             where TDependency7 : class
   {
      var dependency1 = new ComponentPromise<TDependency1>();
      var dependency2 = new ComponentPromise<TDependency2>();
      var dependency3 = new ComponentPromise<TDependency3>();
      var dependency4 = new ComponentPromise<TDependency4>();
      var dependency5 = new ComponentPromise<TDependency5>();
      var dependency6 = new ComponentPromise<TDependency6>();
      var dependency7 = new ComponentPromise<TDependency7>();
      return @this.CreatedBy(
         kern => factoryMethod(dependency1.Resolve(kern), dependency2.Resolve(kern), dependency3.Resolve(kern), dependency4.Resolve(kern), dependency5.Resolve(kern), dependency6.Resolve(kern), dependency7.Resolve(kern)),
         [typeof(TDependency1), typeof(TDependency2), typeof(TDependency3), typeof(TDependency4), typeof(TDependency5), typeof(TDependency6), typeof(TDependency7)]);
   }

   public static ComponentRegistration<TService> CreatedBy<TService, TImplementation, TDependency1, TDependency2, TDependency3, TDependency4, TDependency5, TDependency6, TDependency7, TDependency8>(
      this ComponentRegistrationWithoutInstantiationSpec<TService> @this,
      Func<TDependency1, TDependency2, TDependency3, TDependency4, TDependency5, TDependency6, TDependency7, TDependency8, TImplementation> factoryMethod) where TImplementation : TService
                                                                                                                                                           where TService : class
                                                                                                                                                           where TDependency1 : class
                                                                                                                                                           where TDependency2 : class
                                                                                                                                                           where TDependency3 : class
                                                                                                                                                           where TDependency4 : class
                                                                                                                                                           where TDependency5 : class
                                                                                                                                                           where TDependency6 : class
                                                                                                                                                           where TDependency7 : class
                                                                                                                                                           where TDependency8 : class
   {
      var dependency1 = new ComponentPromise<TDependency1>();
      var dependency2 = new ComponentPromise<TDependency2>();
      var dependency3 = new ComponentPromise<TDependency3>();
      var dependency4 = new ComponentPromise<TDependency4>();
      var dependency5 = new ComponentPromise<TDependency5>();
      var dependency6 = new ComponentPromise<TDependency6>();
      var dependency7 = new ComponentPromise<TDependency7>();
      var dependency8 = new ComponentPromise<TDependency8>();
      return @this.CreatedBy(
         kern => factoryMethod(dependency1.Resolve(kern), dependency2.Resolve(kern), dependency3.Resolve(kern), dependency4.Resolve(kern), dependency5.Resolve(kern), dependency6.Resolve(kern), dependency7.Resolve(kern), dependency8.Resolve(kern)),
         [typeof(TDependency1), typeof(TDependency2), typeof(TDependency3), typeof(TDependency4), typeof(TDependency5), typeof(TDependency6), typeof(TDependency7), typeof(TDependency8)]);
   }

   public static ComponentRegistration<TService> CreatedBy<TService, TImplementation, TDependency1, TDependency2, TDependency3, TDependency4, TDependency5, TDependency6, TDependency7, TDependency8, TDependency9>(
      this ComponentRegistrationWithoutInstantiationSpec<TService> @this,
      Func<TDependency1, TDependency2, TDependency3, TDependency4, TDependency5, TDependency6, TDependency7, TDependency8, TDependency9, TImplementation> factoryMethod) where TImplementation : TService
                                                                                                                                                                         where TService : class
                                                                                                                                                                         where TDependency1 : class
                                                                                                                                                                         where TDependency2 : class
                                                                                                                                                                         where TDependency3 : class
                                                                                                                                                                         where TDependency4 : class
                                                                                                                                                                         where TDependency5 : class
                                                                                                                                                                         where TDependency6 : class
                                                                                                                                                                         where TDependency7 : class
                                                                                                                                                                         where TDependency8 : class
                                                                                                                                                                         where TDependency9 : class
   {
      var dependency1 = new ComponentPromise<TDependency1>();
      var dependency2 = new ComponentPromise<TDependency2>();
      var dependency3 = new ComponentPromise<TDependency3>();
      var dependency4 = new ComponentPromise<TDependency4>();
      var dependency5 = new ComponentPromise<TDependency5>();
      var dependency6 = new ComponentPromise<TDependency6>();
      var dependency7 = new ComponentPromise<TDependency7>();
      var dependency8 = new ComponentPromise<TDependency8>();
      var dependency9 = new ComponentPromise<TDependency9>();
      return @this.CreatedBy(
         kern => factoryMethod(dependency1.Resolve(kern), dependency2.Resolve(kern), dependency3.Resolve(kern), dependency4.Resolve(kern), dependency5.Resolve(kern), dependency6.Resolve(kern), dependency7.Resolve(kern), dependency8.Resolve(kern), dependency9.Resolve(kern)),
         [typeof(TDependency1), typeof(TDependency2), typeof(TDependency3), typeof(TDependency4), typeof(TDependency5), typeof(TDependency6), typeof(TDependency7), typeof(TDependency8), typeof(TDependency9)]);
   }

   public static ComponentRegistration<TService> CreatedBy<TService, TImplementation, TDependency1, TDependency2, TDependency3, TDependency4, TDependency5, TDependency6, TDependency7, TDependency8, TDependency9, TDependency10>(
      this ComponentRegistrationWithoutInstantiationSpec<TService> @this,
      Func<TDependency1, TDependency2, TDependency3, TDependency4, TDependency5, TDependency6, TDependency7, TDependency8, TDependency9, TDependency10, TImplementation> factoryMethod) where TImplementation : TService
                                                                                                                                                                                       where TService : class
                                                                                                                                                                                       where TDependency1 : class
                                                                                                                                                                                       where TDependency2 : class
                                                                                                                                                                                       where TDependency3 : class
                                                                                                                                                                                       where TDependency4 : class
                                                                                                                                                                                       where TDependency5 : class
                                                                                                                                                                                       where TDependency6 : class
                                                                                                                                                                                       where TDependency7 : class
                                                                                                                                                                                       where TDependency8 : class
                                                                                                                                                                                       where TDependency9 : class
                                                                                                                                                                                       where TDependency10 : class
   {
      var dependency1 = new ComponentPromise<TDependency1>();
      var dependency2 = new ComponentPromise<TDependency2>();
      var dependency3 = new ComponentPromise<TDependency3>();
      var dependency4 = new ComponentPromise<TDependency4>();
      var dependency5 = new ComponentPromise<TDependency5>();
      var dependency6 = new ComponentPromise<TDependency6>();
      var dependency7 = new ComponentPromise<TDependency7>();
      var dependency8 = new ComponentPromise<TDependency8>();
      var dependency9 = new ComponentPromise<TDependency9>();
      var dependency10 = new ComponentPromise<TDependency10>();
      return @this.CreatedBy(
         kern => factoryMethod(dependency1.Resolve(kern), dependency2.Resolve(kern), dependency3.Resolve(kern), dependency4.Resolve(kern), dependency5.Resolve(kern), dependency6.Resolve(kern), dependency7.Resolve(kern), dependency8.Resolve(kern), dependency9.Resolve(kern), dependency10.Resolve(kern)),
         [typeof(TDependency1), typeof(TDependency2), typeof(TDependency3), typeof(TDependency4), typeof(TDependency5), typeof(TDependency6), typeof(TDependency7), typeof(TDependency8), typeof(TDependency9), typeof(TDependency10)]);
   }

   public static ComponentRegistration<TService> CreatedBy<TService, TImplementation, TDependency1, TDependency2, TDependency3, TDependency4, TDependency5, TDependency6, TDependency7, TDependency8, TDependency9, TDependency10, TDependency11>(
      this ComponentRegistrationWithoutInstantiationSpec<TService> @this,
      Func<TDependency1, TDependency2, TDependency3, TDependency4, TDependency5, TDependency6, TDependency7, TDependency8, TDependency9, TDependency10, TDependency11, TImplementation> factoryMethod) where TImplementation : TService
                                                                                                                                                                                                       where TService : class
                                                                                                                                                                                                       where TDependency1 : class
                                                                                                                                                                                                       where TDependency2 : class
                                                                                                                                                                                                       where TDependency3 : class
                                                                                                                                                                                                       where TDependency4 : class
                                                                                                                                                                                                       where TDependency5 : class
                                                                                                                                                                                                       where TDependency6 : class
                                                                                                                                                                                                       where TDependency7 : class
                                                                                                                                                                                                       where TDependency8 : class
                                                                                                                                                                                                       where TDependency9 : class
                                                                                                                                                                                                       where TDependency10 : class
                                                                                                                                                                                                       where TDependency11 : class
   {
      var dependency1 = new ComponentPromise<TDependency1>();
      var dependency2 = new ComponentPromise<TDependency2>();
      var dependency3 = new ComponentPromise<TDependency3>();
      var dependency4 = new ComponentPromise<TDependency4>();
      var dependency5 = new ComponentPromise<TDependency5>();
      var dependency6 = new ComponentPromise<TDependency6>();
      var dependency7 = new ComponentPromise<TDependency7>();
      var dependency8 = new ComponentPromise<TDependency8>();
      var dependency9 = new ComponentPromise<TDependency9>();
      var dependency10 = new ComponentPromise<TDependency10>();
      var dependency11 = new ComponentPromise<TDependency11>();
      return @this.CreatedBy(
         kern => factoryMethod(dependency1.Resolve(kern), dependency2.Resolve(kern), dependency3.Resolve(kern), dependency4.Resolve(kern), dependency5.Resolve(kern), dependency6.Resolve(kern), dependency7.Resolve(kern), dependency8.Resolve(kern), dependency9.Resolve(kern), dependency10.Resolve(kern), dependency11.Resolve(kern)),
         [typeof(TDependency1), typeof(TDependency2), typeof(TDependency3), typeof(TDependency4), typeof(TDependency5), typeof(TDependency6), typeof(TDependency7), typeof(TDependency8), typeof(TDependency9), typeof(TDependency10), typeof(TDependency11)]);
   }

   public static ComponentRegistration<TService> CreatedBy<TService, TImplementation, TDependency1, TDependency2, TDependency3, TDependency4, TDependency5, TDependency6, TDependency7, TDependency8, TDependency9, TDependency10, TDependency11, TDependency12>(
      this ComponentRegistrationWithoutInstantiationSpec<TService> @this,
      Func<TDependency1, TDependency2, TDependency3, TDependency4, TDependency5, TDependency6, TDependency7, TDependency8, TDependency9, TDependency10, TDependency11, TDependency12, TImplementation> factoryMethod) where TImplementation : TService
                                                                                                                                                                                                                       where TService : class
                                                                                                                                                                                                                       where TDependency1 : class
                                                                                                                                                                                                                       where TDependency2 : class
                                                                                                                                                                                                                       where TDependency3 : class
                                                                                                                                                                                                                       where TDependency4 : class
                                                                                                                                                                                                                       where TDependency5 : class
                                                                                                                                                                                                                       where TDependency6 : class
                                                                                                                                                                                                                       where TDependency7 : class
                                                                                                                                                                                                                       where TDependency8 : class
                                                                                                                                                                                                                       where TDependency9 : class
                                                                                                                                                                                                                       where TDependency10 : class
                                                                                                                                                                                                                       where TDependency11 : class
                                                                                                                                                                                                                       where TDependency12 : class
   {
      var dependency1 = new ComponentPromise<TDependency1>();
      var dependency2 = new ComponentPromise<TDependency2>();
      var dependency3 = new ComponentPromise<TDependency3>();
      var dependency4 = new ComponentPromise<TDependency4>();
      var dependency5 = new ComponentPromise<TDependency5>();
      var dependency6 = new ComponentPromise<TDependency6>();
      var dependency7 = new ComponentPromise<TDependency7>();
      var dependency8 = new ComponentPromise<TDependency8>();
      var dependency9 = new ComponentPromise<TDependency9>();
      var dependency10 = new ComponentPromise<TDependency10>();
      var dependency11 = new ComponentPromise<TDependency11>();
      var dependency12 = new ComponentPromise<TDependency12>();
      return @this.CreatedBy(
         kern => factoryMethod(dependency1.Resolve(kern), dependency2.Resolve(kern), dependency3.Resolve(kern), dependency4.Resolve(kern), dependency5.Resolve(kern), dependency6.Resolve(kern), dependency7.Resolve(kern), dependency8.Resolve(kern), dependency9.Resolve(kern), dependency10.Resolve(kern), dependency11.Resolve(kern), dependency12.Resolve(kern)),
         [typeof(TDependency1), typeof(TDependency2), typeof(TDependency3), typeof(TDependency4), typeof(TDependency5), typeof(TDependency6), typeof(TDependency7), typeof(TDependency8), typeof(TDependency9), typeof(TDependency10), typeof(TDependency11), typeof(TDependency12)]);
   }

   public static ComponentRegistration<TService> CreatedBy<TService, TImplementation, TDependency1, TDependency2, TDependency3, TDependency4, TDependency5, TDependency6, TDependency7, TDependency8, TDependency9, TDependency10, TDependency11, TDependency12, TDependency13>(
      this ComponentRegistrationWithoutInstantiationSpec<TService> @this,
      Func<TDependency1, TDependency2, TDependency3, TDependency4, TDependency5, TDependency6, TDependency7, TDependency8, TDependency9, TDependency10, TDependency11, TDependency12, TDependency13, TImplementation> factoryMethod) where TImplementation : TService
                                                                                                                                                                                                                                       where TService : class
                                                                                                                                                                                                                                       where TDependency1 : class
                                                                                                                                                                                                                                       where TDependency2 : class
                                                                                                                                                                                                                                       where TDependency3 : class
                                                                                                                                                                                                                                       where TDependency4 : class
                                                                                                                                                                                                                                       where TDependency5 : class
                                                                                                                                                                                                                                       where TDependency6 : class
                                                                                                                                                                                                                                       where TDependency7 : class
                                                                                                                                                                                                                                       where TDependency8 : class
                                                                                                                                                                                                                                       where TDependency9 : class
                                                                                                                                                                                                                                       where TDependency10 : class
                                                                                                                                                                                                                                       where TDependency11 : class
                                                                                                                                                                                                                                       where TDependency12 : class
                                                                                                                                                                                                                                       where TDependency13 : class
   {
      var dependency1 = new ComponentPromise<TDependency1>();
      var dependency2 = new ComponentPromise<TDependency2>();
      var dependency3 = new ComponentPromise<TDependency3>();
      var dependency4 = new ComponentPromise<TDependency4>();
      var dependency5 = new ComponentPromise<TDependency5>();
      var dependency6 = new ComponentPromise<TDependency6>();
      var dependency7 = new ComponentPromise<TDependency7>();
      var dependency8 = new ComponentPromise<TDependency8>();
      var dependency9 = new ComponentPromise<TDependency9>();
      var dependency10 = new ComponentPromise<TDependency10>();
      var dependency11 = new ComponentPromise<TDependency11>();
      var dependency12 = new ComponentPromise<TDependency12>();
      var dependency13 = new ComponentPromise<TDependency13>();
      return @this.CreatedBy(
         kern => factoryMethod(dependency1.Resolve(kern), dependency2.Resolve(kern), dependency3.Resolve(kern), dependency4.Resolve(kern), dependency5.Resolve(kern), dependency6.Resolve(kern), dependency7.Resolve(kern), dependency8.Resolve(kern), dependency9.Resolve(kern), dependency10.Resolve(kern), dependency11.Resolve(kern), dependency12.Resolve(kern), dependency13.Resolve(kern)),
         [typeof(TDependency1), typeof(TDependency2), typeof(TDependency3), typeof(TDependency4), typeof(TDependency5), typeof(TDependency6), typeof(TDependency7), typeof(TDependency8), typeof(TDependency9), typeof(TDependency10), typeof(TDependency11), typeof(TDependency12), typeof(TDependency13)]);
   }

   public static ComponentRegistration<TService> CreatedBy<TService, TImplementation, TDependency1, TDependency2, TDependency3, TDependency4, TDependency5, TDependency6, TDependency7, TDependency8, TDependency9, TDependency10, TDependency11, TDependency12, TDependency13, TDependency14>(
      this ComponentRegistrationWithoutInstantiationSpec<TService> @this,
      Func<TDependency1, TDependency2, TDependency3, TDependency4, TDependency5, TDependency6, TDependency7, TDependency8, TDependency9, TDependency10, TDependency11, TDependency12, TDependency13, TDependency14, TImplementation> factoryMethod) where TImplementation : TService
                                                                                                                                                                                                                                                       where TService : class
                                                                                                                                                                                                                                                       where TDependency1 : class
                                                                                                                                                                                                                                                       where TDependency2 : class
                                                                                                                                                                                                                                                       where TDependency3 : class
                                                                                                                                                                                                                                                       where TDependency4 : class
                                                                                                                                                                                                                                                       where TDependency5 : class
                                                                                                                                                                                                                                                       where TDependency6 : class
                                                                                                                                                                                                                                                       where TDependency7 : class
                                                                                                                                                                                                                                                       where TDependency8 : class
                                                                                                                                                                                                                                                       where TDependency9 : class
                                                                                                                                                                                                                                                       where TDependency10 : class
                                                                                                                                                                                                                                                       where TDependency11 : class
                                                                                                                                                                                                                                                       where TDependency12 : class
                                                                                                                                                                                                                                                       where TDependency13 : class
                                                                                                                                                                                                                                                       where TDependency14 : class
   {
      var dependency1 = new ComponentPromise<TDependency1>();
      var dependency2 = new ComponentPromise<TDependency2>();
      var dependency3 = new ComponentPromise<TDependency3>();
      var dependency4 = new ComponentPromise<TDependency4>();
      var dependency5 = new ComponentPromise<TDependency5>();
      var dependency6 = new ComponentPromise<TDependency6>();
      var dependency7 = new ComponentPromise<TDependency7>();
      var dependency8 = new ComponentPromise<TDependency8>();
      var dependency9 = new ComponentPromise<TDependency9>();
      var dependency10 = new ComponentPromise<TDependency10>();
      var dependency11 = new ComponentPromise<TDependency11>();
      var dependency12 = new ComponentPromise<TDependency12>();
      var dependency13 = new ComponentPromise<TDependency13>();
      var dependency14 = new ComponentPromise<TDependency14>();
      return @this.CreatedBy(
         kern => factoryMethod(dependency1.Resolve(kern), dependency2.Resolve(kern), dependency3.Resolve(kern), dependency4.Resolve(kern), dependency5.Resolve(kern), dependency6.Resolve(kern), dependency7.Resolve(kern), dependency8.Resolve(kern), dependency9.Resolve(kern), dependency10.Resolve(kern), dependency11.Resolve(kern), dependency12.Resolve(kern), dependency13.Resolve(kern), dependency14.Resolve(kern)),
         [typeof(TDependency1), typeof(TDependency2), typeof(TDependency3), typeof(TDependency4), typeof(TDependency5), typeof(TDependency6), typeof(TDependency7), typeof(TDependency8), typeof(TDependency9), typeof(TDependency10), typeof(TDependency11), typeof(TDependency12), typeof(TDependency13), typeof(TDependency14)]);
   }

   public static ComponentRegistration<TService> CreatedBy<TService, TImplementation, TDependency1, TDependency2, TDependency3, TDependency4, TDependency5, TDependency6, TDependency7, TDependency8, TDependency9, TDependency10, TDependency11, TDependency12, TDependency13, TDependency14, TDependency15>(
      this ComponentRegistrationWithoutInstantiationSpec<TService> @this,
      Func<TDependency1, TDependency2, TDependency3, TDependency4, TDependency5, TDependency6, TDependency7, TDependency8, TDependency9, TDependency10, TDependency11, TDependency12, TDependency13, TDependency14, TDependency15, TImplementation> factoryMethod) where TImplementation : TService
                                                                                                                                                                                                                                                                       where TService : class
                                                                                                                                                                                                                                                                       where TDependency1 : class
                                                                                                                                                                                                                                                                       where TDependency2 : class
                                                                                                                                                                                                                                                                       where TDependency3 : class
                                                                                                                                                                                                                                                                       where TDependency4 : class
                                                                                                                                                                                                                                                                       where TDependency5 : class
                                                                                                                                                                                                                                                                       where TDependency6 : class
                                                                                                                                                                                                                                                                       where TDependency7 : class
                                                                                                                                                                                                                                                                       where TDependency8 : class
                                                                                                                                                                                                                                                                       where TDependency9 : class
                                                                                                                                                                                                                                                                       where TDependency10 : class
                                                                                                                                                                                                                                                                       where TDependency11 : class
                                                                                                                                                                                                                                                                       where TDependency12 : class
                                                                                                                                                                                                                                                                       where TDependency13 : class
                                                                                                                                                                                                                                                                       where TDependency14 : class
                                                                                                                                                                                                                                                                       where TDependency15 : class
   {
      var dependency1 = new ComponentPromise<TDependency1>();
      var dependency2 = new ComponentPromise<TDependency2>();
      var dependency3 = new ComponentPromise<TDependency3>();
      var dependency4 = new ComponentPromise<TDependency4>();
      var dependency5 = new ComponentPromise<TDependency5>();
      var dependency6 = new ComponentPromise<TDependency6>();
      var dependency7 = new ComponentPromise<TDependency7>();
      var dependency8 = new ComponentPromise<TDependency8>();
      var dependency9 = new ComponentPromise<TDependency9>();
      var dependency10 = new ComponentPromise<TDependency10>();
      var dependency11 = new ComponentPromise<TDependency11>();
      var dependency12 = new ComponentPromise<TDependency12>();
      var dependency13 = new ComponentPromise<TDependency13>();
      var dependency14 = new ComponentPromise<TDependency14>();
      var dependency15 = new ComponentPromise<TDependency15>();
      return @this.CreatedBy(
         kern => factoryMethod(dependency1.Resolve(kern), dependency2.Resolve(kern), dependency3.Resolve(kern), dependency4.Resolve(kern), dependency5.Resolve(kern), dependency6.Resolve(kern), dependency7.Resolve(kern), dependency8.Resolve(kern), dependency9.Resolve(kern), dependency10.Resolve(kern), dependency11.Resolve(kern), dependency12.Resolve(kern), dependency13.Resolve(kern), dependency14.Resolve(kern), dependency15.Resolve(kern)),
         [typeof(TDependency1), typeof(TDependency2), typeof(TDependency3), typeof(TDependency4), typeof(TDependency5), typeof(TDependency6), typeof(TDependency7), typeof(TDependency8), typeof(TDependency9), typeof(TDependency10), typeof(TDependency11), typeof(TDependency12), typeof(TDependency13), typeof(TDependency14), typeof(TDependency15)]);
   }

   public static ComponentRegistration<TService> CreatedBy<TService, TImplementation, TDependency1, TDependency2, TDependency3, TDependency4, TDependency5, TDependency6, TDependency7, TDependency8, TDependency9, TDependency10, TDependency11, TDependency12, TDependency13, TDependency14, TDependency15, TDependency16>(
      this ComponentRegistrationWithoutInstantiationSpec<TService> @this,
      Func<TDependency1, TDependency2, TDependency3, TDependency4, TDependency5, TDependency6, TDependency7, TDependency8, TDependency9, TDependency10, TDependency11, TDependency12, TDependency13, TDependency14, TDependency15, TDependency16, TImplementation> factoryMethod) where TImplementation : TService
                                                                                                                                                                                                                                                                                       where TService : class
                                                                                                                                                                                                                                                                                       where TDependency1 : class
                                                                                                                                                                                                                                                                                       where TDependency2 : class
                                                                                                                                                                                                                                                                                       where TDependency3 : class
                                                                                                                                                                                                                                                                                       where TDependency4 : class
                                                                                                                                                                                                                                                                                       where TDependency5 : class
                                                                                                                                                                                                                                                                                       where TDependency6 : class
                                                                                                                                                                                                                                                                                       where TDependency7 : class
                                                                                                                                                                                                                                                                                       where TDependency8 : class
                                                                                                                                                                                                                                                                                       where TDependency9 : class
                                                                                                                                                                                                                                                                                       where TDependency10 : class
                                                                                                                                                                                                                                                                                       where TDependency11 : class
                                                                                                                                                                                                                                                                                       where TDependency12 : class
                                                                                                                                                                                                                                                                                       where TDependency13 : class
                                                                                                                                                                                                                                                                                       where TDependency14 : class
                                                                                                                                                                                                                                                                                       where TDependency15 : class
                                                                                                                                                                                                                                                                                       where TDependency16 : class
   {
      var dependency1 = new ComponentPromise<TDependency1>();
      var dependency2 = new ComponentPromise<TDependency2>();
      var dependency3 = new ComponentPromise<TDependency3>();
      var dependency4 = new ComponentPromise<TDependency4>();
      var dependency5 = new ComponentPromise<TDependency5>();
      var dependency6 = new ComponentPromise<TDependency6>();
      var dependency7 = new ComponentPromise<TDependency7>();
      var dependency8 = new ComponentPromise<TDependency8>();
      var dependency9 = new ComponentPromise<TDependency9>();
      var dependency10 = new ComponentPromise<TDependency10>();
      var dependency11 = new ComponentPromise<TDependency11>();
      var dependency12 = new ComponentPromise<TDependency12>();
      var dependency13 = new ComponentPromise<TDependency13>();
      var dependency14 = new ComponentPromise<TDependency14>();
      var dependency15 = new ComponentPromise<TDependency15>();
      var dependency16 = new ComponentPromise<TDependency16>();
      return @this.CreatedBy(
         kern => factoryMethod(dependency1.Resolve(kern), dependency2.Resolve(kern), dependency3.Resolve(kern), dependency4.Resolve(kern), dependency5.Resolve(kern), dependency6.Resolve(kern), dependency7.Resolve(kern), dependency8.Resolve(kern), dependency9.Resolve(kern), dependency10.Resolve(kern), dependency11.Resolve(kern), dependency12.Resolve(kern), dependency13.Resolve(kern), dependency14.Resolve(kern), dependency15.Resolve(kern), dependency16.Resolve(kern)),
         [typeof(TDependency1), typeof(TDependency2), typeof(TDependency3), typeof(TDependency4), typeof(TDependency5), typeof(TDependency6), typeof(TDependency7), typeof(TDependency8), typeof(TDependency9), typeof(TDependency10), typeof(TDependency11), typeof(TDependency12), typeof(TDependency13), typeof(TDependency14), typeof(TDependency15), typeof(TDependency16)]);
   }
}
