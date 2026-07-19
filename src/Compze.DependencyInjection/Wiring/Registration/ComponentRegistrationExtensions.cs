using Compze.DependencyInjection.Runtime.Resolution;

namespace Compze.DependencyInjection.Wiring.Registration;

public static class ComponentRegistrationExtensions
{
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
      return @this.CreatedBy(
         kern => factoryMethod(kern.Resolve<TDependency1>()),
         [typeof(TDependency1)]);
   }

   public static ComponentRegistration<TService> CreatedBy<TService, TImplementation, TDependency1, TDependency2>(
      this ComponentRegistrationWithoutInstantiationSpec<TService> @this,
      Func<TDependency1, TDependency2, TImplementation> factoryMethod) where TService : class
                                                                       where TDependency1 : class
                                                                       where TDependency2 : class
                                                                       where TImplementation : TService
   {
      return @this.CreatedBy(
         kern => factoryMethod(kern.Resolve<TDependency1>(), kern.Resolve<TDependency2>()),
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
      return @this.CreatedBy(
         kern => factoryMethod(kern.Resolve<TDependency1>(), kern.Resolve<TDependency2>(), kern.Resolve<TDependency3>()),
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
      return @this.CreatedBy(
         kern => factoryMethod(kern.Resolve<TDependency1>(), kern.Resolve<TDependency2>(), kern.Resolve<TDependency3>(), kern.Resolve<TDependency4>()),
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
      return @this.CreatedBy(
         kern => factoryMethod(kern.Resolve<TDependency1>(), kern.Resolve<TDependency2>(), kern.Resolve<TDependency3>(), kern.Resolve<TDependency4>(), kern.Resolve<TDependency5>()),
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
      return @this.CreatedBy(
         kern => factoryMethod(kern.Resolve<TDependency1>(), kern.Resolve<TDependency2>(), kern.Resolve<TDependency3>(), kern.Resolve<TDependency4>(), kern.Resolve<TDependency5>(), kern.Resolve<TDependency6>()),
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
      return @this.CreatedBy(
         kern => factoryMethod(kern.Resolve<TDependency1>(), kern.Resolve<TDependency2>(), kern.Resolve<TDependency3>(), kern.Resolve<TDependency4>(), kern.Resolve<TDependency5>(), kern.Resolve<TDependency6>(), kern.Resolve<TDependency7>()),
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
      return @this.CreatedBy(
         kern => factoryMethod(kern.Resolve<TDependency1>(), kern.Resolve<TDependency2>(), kern.Resolve<TDependency3>(), kern.Resolve<TDependency4>(), kern.Resolve<TDependency5>(), kern.Resolve<TDependency6>(), kern.Resolve<TDependency7>(), kern.Resolve<TDependency8>()),
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
      return @this.CreatedBy(
         kern => factoryMethod(kern.Resolve<TDependency1>(), kern.Resolve<TDependency2>(), kern.Resolve<TDependency3>(), kern.Resolve<TDependency4>(), kern.Resolve<TDependency5>(), kern.Resolve<TDependency6>(), kern.Resolve<TDependency7>(), kern.Resolve<TDependency8>(), kern.Resolve<TDependency9>()),
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
      return @this.CreatedBy(
         kern => factoryMethod(kern.Resolve<TDependency1>(), kern.Resolve<TDependency2>(), kern.Resolve<TDependency3>(), kern.Resolve<TDependency4>(), kern.Resolve<TDependency5>(), kern.Resolve<TDependency6>(), kern.Resolve<TDependency7>(), kern.Resolve<TDependency8>(), kern.Resolve<TDependency9>(), kern.Resolve<TDependency10>()),
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
      return @this.CreatedBy(
         kern => factoryMethod(kern.Resolve<TDependency1>(), kern.Resolve<TDependency2>(), kern.Resolve<TDependency3>(), kern.Resolve<TDependency4>(), kern.Resolve<TDependency5>(), kern.Resolve<TDependency6>(), kern.Resolve<TDependency7>(), kern.Resolve<TDependency8>(), kern.Resolve<TDependency9>(), kern.Resolve<TDependency10>(), kern.Resolve<TDependency11>()),
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
      return @this.CreatedBy(
         kern => factoryMethod(kern.Resolve<TDependency1>(), kern.Resolve<TDependency2>(), kern.Resolve<TDependency3>(), kern.Resolve<TDependency4>(), kern.Resolve<TDependency5>(), kern.Resolve<TDependency6>(), kern.Resolve<TDependency7>(), kern.Resolve<TDependency8>(), kern.Resolve<TDependency9>(), kern.Resolve<TDependency10>(), kern.Resolve<TDependency11>(), kern.Resolve<TDependency12>()),
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
      return @this.CreatedBy(
         kern => factoryMethod(kern.Resolve<TDependency1>(), kern.Resolve<TDependency2>(), kern.Resolve<TDependency3>(), kern.Resolve<TDependency4>(), kern.Resolve<TDependency5>(), kern.Resolve<TDependency6>(), kern.Resolve<TDependency7>(), kern.Resolve<TDependency8>(), kern.Resolve<TDependency9>(), kern.Resolve<TDependency10>(), kern.Resolve<TDependency11>(), kern.Resolve<TDependency12>(), kern.Resolve<TDependency13>()),
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
      return @this.CreatedBy(
         kern => factoryMethod(kern.Resolve<TDependency1>(), kern.Resolve<TDependency2>(), kern.Resolve<TDependency3>(), kern.Resolve<TDependency4>(), kern.Resolve<TDependency5>(), kern.Resolve<TDependency6>(), kern.Resolve<TDependency7>(), kern.Resolve<TDependency8>(), kern.Resolve<TDependency9>(), kern.Resolve<TDependency10>(), kern.Resolve<TDependency11>(), kern.Resolve<TDependency12>(), kern.Resolve<TDependency13>(), kern.Resolve<TDependency14>()),
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
      return @this.CreatedBy(
         kern => factoryMethod(kern.Resolve<TDependency1>(), kern.Resolve<TDependency2>(), kern.Resolve<TDependency3>(), kern.Resolve<TDependency4>(), kern.Resolve<TDependency5>(), kern.Resolve<TDependency6>(), kern.Resolve<TDependency7>(), kern.Resolve<TDependency8>(), kern.Resolve<TDependency9>(), kern.Resolve<TDependency10>(), kern.Resolve<TDependency11>(), kern.Resolve<TDependency12>(), kern.Resolve<TDependency13>(), kern.Resolve<TDependency14>(), kern.Resolve<TDependency15>()),
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
      return @this.CreatedBy(
         kern => factoryMethod(kern.Resolve<TDependency1>(), kern.Resolve<TDependency2>(), kern.Resolve<TDependency3>(), kern.Resolve<TDependency4>(), kern.Resolve<TDependency5>(), kern.Resolve<TDependency6>(), kern.Resolve<TDependency7>(), kern.Resolve<TDependency8>(), kern.Resolve<TDependency9>(), kern.Resolve<TDependency10>(), kern.Resolve<TDependency11>(), kern.Resolve<TDependency12>(), kern.Resolve<TDependency13>(), kern.Resolve<TDependency14>(), kern.Resolve<TDependency15>(), kern.Resolve<TDependency16>()),
         [typeof(TDependency1), typeof(TDependency2), typeof(TDependency3), typeof(TDependency4), typeof(TDependency5), typeof(TDependency6), typeof(TDependency7), typeof(TDependency8), typeof(TDependency9), typeof(TDependency10), typeof(TDependency11), typeof(TDependency12), typeof(TDependency13), typeof(TDependency14), typeof(TDependency15), typeof(TDependency16)]);
   }
}
