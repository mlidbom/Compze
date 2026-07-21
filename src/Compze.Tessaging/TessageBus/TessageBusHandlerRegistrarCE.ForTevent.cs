using System.Diagnostics.CodeAnalysis;
using Compze.DependencyInjection;
using Compze.Tessaging.TessageTypes;

namespace Compze.Tessaging.TessageBus;

///<summary>Convenience overloads for <see cref="TessageBusHandlerRegistrar"/>: register a handler whose extra lambda parameters<br/>
/// are resolved from the unit of work delivering the tessage, or one that needs no resolutions at all, instead of taking the<br/>
/// resolver and resolving by hand. Each delegates to the core verb of its shape, so the synchrony rules (exactly-once kinds are<br/>
/// async-only) apply identically here.<br/>
/// One file per verb: this file holds the <c>ForTevent</c> overloads, <c>TessageBusHandlerRegistrarCE.ForTommand.cs</c> the rest.</summary>
[SuppressMessage("Naming", "CA1708:Identifiers should differ by more than case", Justification = "The per-verb partial files each declare an extension block for the same receiver; the compiler-generated grouping members share a display name — no real member collides.")]
public static partial class TessageBusHandlerRegistrarCE
{
   extension(TessageBusHandlerRegistrar @this)
   {
      public TessageBusHandlerRegistrar ForTevent<TTevent>(Action<TTevent> handler) where TTevent : ITevent
         => @this.ForTevent<TTevent>((tevent, _) => handler(tevent));

      public TessageBusHandlerRegistrar ForTevent<TTevent>(Func<TTevent, Task> handler) where TTevent : ITevent
         => @this.ForTevent<TTevent>((tevent, _) => handler(tevent));

      public TessageBusHandlerRegistrar ForTevent<TTevent, TDependency1>(Action<TTevent, TDependency1> handler) where TTevent : ITevent
                                                                                                                where TDependency1 : class
         => @this.ForTevent<TTevent>((tevent, unitOfWork) => handler(tevent, unitOfWork.Resolve<TDependency1>()));

      public TessageBusHandlerRegistrar ForTevent<TTevent, TDependency1>(Func<TTevent, TDependency1, Task> handler) where TTevent : ITevent
                                                                                                                    where TDependency1 : class
         => @this.ForTevent<TTevent>((tevent, unitOfWork) => handler(tevent, unitOfWork.Resolve<TDependency1>()));

      public TessageBusHandlerRegistrar ForTevent<TTevent, TDependency1, TDependency2>(Action<TTevent, TDependency1, TDependency2> handler) where TTevent : ITevent
                                                                                                                                            where TDependency1 : class
                                                                                                                                            where TDependency2 : class
         => @this.ForTevent<TTevent>((tevent, unitOfWork) => handler(tevent, unitOfWork.Resolve<TDependency1>(), unitOfWork.Resolve<TDependency2>()));

      public TessageBusHandlerRegistrar ForTevent<TTevent, TDependency1, TDependency2>(Func<TTevent, TDependency1, TDependency2, Task> handler) where TTevent : ITevent
                                                                                                                                                where TDependency1 : class
                                                                                                                                                where TDependency2 : class
         => @this.ForTevent<TTevent>((tevent, unitOfWork) => handler(tevent, unitOfWork.Resolve<TDependency1>(), unitOfWork.Resolve<TDependency2>()));
   }
}
