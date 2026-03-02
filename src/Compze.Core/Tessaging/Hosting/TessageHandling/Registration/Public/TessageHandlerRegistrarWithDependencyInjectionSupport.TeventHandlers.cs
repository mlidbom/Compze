using System;
using Compze.Core.Tessaging.Public;

namespace Compze.Core.Tessaging.Hosting.TessageHandling.Registration.Public;

public static partial class TessageHandlerRegistrarWithDependencyInjectionSupportExtensions
{
   public static TessageHandlerRegistrarWithDependencyInjectionSupport ForTevent<TTevent>(
      this TessageHandlerRegistrarWithDependencyInjectionSupport @this,
      Action<TTevent> handler) where TTevent : ITevent
   {
      @this.Register.ForTevent(handler);
      return @this;
   }

   public static TessageHandlerRegistrarWithDependencyInjectionSupport ForTevent<TTevent, TDependency1>(
      this TessageHandlerRegistrarWithDependencyInjectionSupport @this,
      Action<TTevent, TDependency1> handler) where TTevent : ITevent
                                             where TDependency1 : class
   {
      @this.Register.ForTevent<TTevent>(tevent => handler(tevent, @this.Resolve<TDependency1>()));
      return @this;
   }

   public static TessageHandlerRegistrarWithDependencyInjectionSupport ForTevent<TTevent, TDependency1, TDependency2>(
      this TessageHandlerRegistrarWithDependencyInjectionSupport @this,
      Action<TTevent, TDependency1, TDependency2> handler) where TTevent : ITevent
                                                           where TDependency1 : class
                                                           where TDependency2 : class
   {
      @this.Register.ForTevent<TTevent>(tevent => handler(tevent, @this.Resolve<TDependency1>(), @this.Resolve<TDependency2>()));
      return @this;
   }

   public static TessageHandlerRegistrarWithDependencyInjectionSupport ForTevent<TTevent, TDependency1, TDependency2, TDependency3>(
      this TessageHandlerRegistrarWithDependencyInjectionSupport @this,
      Action<TTevent, TDependency1, TDependency2, TDependency3> handler) where TTevent : ITevent
                                                                         where TDependency1 : class
                                                                         where TDependency2 : class
                                                                         where TDependency3 : class
   {
      @this.Register.ForTevent<TTevent>(tevent => handler(tevent, @this.Resolve<TDependency1>(), @this.Resolve<TDependency2>(), @this.Resolve<TDependency3>()));
      return @this;
   }

   public static TessageHandlerRegistrarWithDependencyInjectionSupport ForTevent<TTevent, TDependency1, TDependency2, TDependency3, TDependency4>(
      this TessageHandlerRegistrarWithDependencyInjectionSupport @this,
      Action<TTevent, TDependency1, TDependency2, TDependency3, TDependency4> handler) where TTevent : ITevent
                                                                                       where TDependency1 : class
                                                                                       where TDependency2 : class
                                                                                       where TDependency3 : class
                                                                                       where TDependency4 : class
   {
      @this.Register.ForTevent<TTevent>(tevent => handler(tevent, @this.Resolve<TDependency1>(), @this.Resolve<TDependency2>(), @this.Resolve<TDependency3>(), @this.Resolve<TDependency4>()));
      return @this;
   }

   public static TessageHandlerRegistrarWithDependencyInjectionSupport ForTevent<TTevent, TDependency1, TDependency2, TDependency3, TDependency4, TDependency5>(
      this TessageHandlerRegistrarWithDependencyInjectionSupport @this,
      Action<TTevent, TDependency1, TDependency2, TDependency3, TDependency4, TDependency5> handler) where TTevent : ITevent
                                                                                                     where TDependency1 : class
                                                                                                     where TDependency2 : class
                                                                                                     where TDependency3 : class
                                                                                                     where TDependency4 : class
                                                                                                     where TDependency5 : class
   {
      @this.Register.ForTevent<TTevent>(tevent => handler(tevent, @this.Resolve<TDependency1>(), @this.Resolve<TDependency2>(), @this.Resolve<TDependency3>(), @this.Resolve<TDependency4>(), @this.Resolve<TDependency5>()));
      return @this;
   }

   public static TessageHandlerRegistrarWithDependencyInjectionSupport ForTevent<TTevent, TDependency1, TDependency2, TDependency3, TDependency4, TDependency5, TDependency6>(
      this TessageHandlerRegistrarWithDependencyInjectionSupport @this,
      Action<TTevent, TDependency1, TDependency2, TDependency3, TDependency4, TDependency5, TDependency6> handler) where TTevent : ITevent
                                                                                                                   where TDependency1 : class
                                                                                                                   where TDependency2 : class
                                                                                                                   where TDependency3 : class
                                                                                                                   where TDependency4 : class
                                                                                                                   where TDependency5 : class
                                                                                                                   where TDependency6 : class
   {
      @this.Register.ForTevent<TTevent>(tevent => handler(tevent, @this.Resolve<TDependency1>(), @this.Resolve<TDependency2>(), @this.Resolve<TDependency3>(), @this.Resolve<TDependency4>(), @this.Resolve<TDependency5>(), @this.Resolve<TDependency6>()));
      return @this;
   }

   public static TessageHandlerRegistrarWithDependencyInjectionSupport ForTevent<TTevent, TDependency1, TDependency2, TDependency3, TDependency4, TDependency5, TDependency6, TDependency7>(
      this TessageHandlerRegistrarWithDependencyInjectionSupport @this,
      Action<TTevent, TDependency1, TDependency2, TDependency3, TDependency4, TDependency5, TDependency6, TDependency7> handler) where TTevent : ITevent
                                                                                                                                 where TDependency1 : class
                                                                                                                                 where TDependency2 : class
                                                                                                                                 where TDependency3 : class
                                                                                                                                 where TDependency4 : class
                                                                                                                                 where TDependency5 : class
                                                                                                                                 where TDependency6 : class
                                                                                                                                 where TDependency7 : class
   {
      @this.Register.ForTevent<TTevent>(tevent => handler(tevent, @this.Resolve<TDependency1>(), @this.Resolve<TDependency2>(), @this.Resolve<TDependency3>(), @this.Resolve<TDependency4>(), @this.Resolve<TDependency5>(), @this.Resolve<TDependency6>(), @this.Resolve<TDependency7>()));
      return @this;
   }

   public static TessageHandlerRegistrarWithDependencyInjectionSupport ForTevent<TTevent, TDependency1, TDependency2, TDependency3, TDependency4, TDependency5, TDependency6, TDependency7, TDependency8>(
      this TessageHandlerRegistrarWithDependencyInjectionSupport @this,
      Action<TTevent, TDependency1, TDependency2, TDependency3, TDependency4, TDependency5, TDependency6, TDependency7, TDependency8> handler) where TTevent : ITevent
                                                                                                                                               where TDependency1 : class
                                                                                                                                               where TDependency2 : class
                                                                                                                                               where TDependency3 : class
                                                                                                                                               where TDependency4 : class
                                                                                                                                               where TDependency5 : class
                                                                                                                                               where TDependency6 : class
                                                                                                                                               where TDependency7 : class
                                                                                                                                               where TDependency8 : class
   {
      @this.Register.ForTevent<TTevent>(tevent => handler(tevent, @this.Resolve<TDependency1>(), @this.Resolve<TDependency2>(), @this.Resolve<TDependency3>(), @this.Resolve<TDependency4>(), @this.Resolve<TDependency5>(), @this.Resolve<TDependency6>(), @this.Resolve<TDependency7>(), @this.Resolve<TDependency8>()));
      return @this;
   }

   public static TessageHandlerRegistrarWithDependencyInjectionSupport ForTevent<TTevent, TDependency1, TDependency2, TDependency3, TDependency4, TDependency5, TDependency6, TDependency7, TDependency8, TDependency9>(
      this TessageHandlerRegistrarWithDependencyInjectionSupport @this,
      Action<TTevent, TDependency1, TDependency2, TDependency3, TDependency4, TDependency5, TDependency6, TDependency7, TDependency8, TDependency9> handler) where TTevent : ITevent
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
      @this.Register.ForTevent<TTevent>(tevent => handler(tevent, @this.Resolve<TDependency1>(), @this.Resolve<TDependency2>(), @this.Resolve<TDependency3>(), @this.Resolve<TDependency4>(), @this.Resolve<TDependency5>(), @this.Resolve<TDependency6>(), @this.Resolve<TDependency7>(), @this.Resolve<TDependency8>(), @this.Resolve<TDependency9>()));
      return @this;
   }
}
