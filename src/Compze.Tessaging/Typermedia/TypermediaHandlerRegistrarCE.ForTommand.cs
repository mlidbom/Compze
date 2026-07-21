using System.Diagnostics.CodeAnalysis;
using Compze.DependencyInjection;
using Compze.Tessaging.TessageTypes;

namespace Compze.Tessaging.Typermedia;

///<summary>Convenience overloads for <see cref="TypermediaHandlerRegistrar"/>: register a handler whose extra lambda parameters<br/>
/// are resolved from the handling context — the unit of work for tommands, the scope for tueries — or one that needs no<br/>
/// resolutions at all, instead of taking the resolver and resolving by hand. Each delegates to the core verb of its shape.<br/>
/// One file per verb: this file holds the <c>ForTommand</c> overloads, <c>TypermediaHandlerRegistrarCE.ForTuery.cs</c> the rest.</summary>
[SuppressMessage("Naming", "CA1708:Identifiers should differ by more than case", Justification = "The per-verb partial files each declare an extension block for the same receiver; the compiler-generated grouping members share a display name — no real member collides.")]
public static partial class TypermediaHandlerRegistrarCE
{
   extension(TypermediaHandlerRegistrar @this)
   {
      public TypermediaHandlerRegistrar ForTommand<TTommand>(Action<TTommand> handler) where TTommand : ITommand
         => @this.ForTommand<TTommand>((tommand, _) => handler(tommand));

      public TypermediaHandlerRegistrar ForTommand<TTommand>(Func<TTommand, Task> handler) where TTommand : ITommand
         => @this.ForTommand<TTommand>((tommand, _) => handler(tommand));

      public TypermediaHandlerRegistrar ForTommand<TTommand, TDependency1>(Action<TTommand, TDependency1> handler) where TTommand : ITommand
                                                                                                                   where TDependency1 : class
         => @this.ForTommand<TTommand>((tommand, unitOfWork) => handler(tommand, unitOfWork.Resolve<TDependency1>()));

      public TypermediaHandlerRegistrar ForTommand<TTommand, TDependency1>(Func<TTommand, TDependency1, Task> handler) where TTommand : ITommand
                                                                                                                       where TDependency1 : class
         => @this.ForTommand<TTommand>((tommand, unitOfWork) => handler(tommand, unitOfWork.Resolve<TDependency1>()));

      public TypermediaHandlerRegistrar ForTommand<TTommand, TResult>(Func<TTommand, TResult> handler) where TTommand : ITommand<TResult>
         => @this.ForTommand<TTommand, TResult>((tommand, _) => handler(tommand));

      public TypermediaHandlerRegistrar ForTommand<TTommand, TDependency1, TResult>(Func<TTommand, TDependency1, TResult> handler) where TTommand : ITommand<TResult>
                                                                                                                                   where TDependency1 : class
         => @this.ForTommand<TTommand, TResult>((tommand, unitOfWork) => handler(tommand, unitOfWork.Resolve<TDependency1>()));
   }
}
