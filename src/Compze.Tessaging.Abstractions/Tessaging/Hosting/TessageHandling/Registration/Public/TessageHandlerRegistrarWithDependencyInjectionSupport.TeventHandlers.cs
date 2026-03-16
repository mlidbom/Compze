using Compze.Abstractions.Tessaging.Public;

namespace Compze.Tessaging.Abstractions.Tessaging.Hosting.TessageHandling.Registration.Public;

public static partial class TessageHandlerRegistrarWithDependencyInjectionSupportExtensions
{
   public static TessageHandlerRegistrarWithDependencyInjectionSupport ForTevent<TTevent>(
      this TessageHandlerRegistrarWithDependencyInjectionSupport @this,
      Action<TTevent> handler) where TTevent : ITevent
   {
      @this.Registrar.ForTevent<TTevent>((tevent, _) => handler(tevent));
      return @this;
   }

   public static TessageHandlerRegistrarWithDependencyInjectionSupport ForTevent<TTevent, TDependency1>(
      this TessageHandlerRegistrarWithDependencyInjectionSupport @this,
      Action<TTevent, TDependency1> handler) where TTevent : ITevent
                                             where TDependency1 : class
   {
      @this.Registrar.ForTevent<TTevent>((tevent, kernel) => handler(tevent, kernel.Resolve<TDependency1>()));
      return @this;
   }

   public static TessageHandlerRegistrarWithDependencyInjectionSupport ForTevent<TTevent, TDependency1, TDependency2>(
      this TessageHandlerRegistrarWithDependencyInjectionSupport @this,
      Action<TTevent, TDependency1, TDependency2> handler) where TTevent : ITevent
                                                           where TDependency1 : class
                                                           where TDependency2 : class
   {
      @this.Registrar.ForTevent<TTevent>((tevent, kernel) => handler(tevent, kernel.Resolve<TDependency1>(), kernel.Resolve<TDependency2>()));
      return @this;
   }
}
