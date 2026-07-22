using Compze.DependencyInjection.Abstractions;
using Compze.Tessaging.TessageTypes;

namespace Compze.Tessaging.Typermedia;

///<summary>The minimal declaration door for Typermedia tommand handlers — what an endpoint-declaration's<br/>
/// <c>RegisterTypermediaTommandHandlers</c> override receives: the navigated tommand kinds, with and without a declared<br/>
/// result. The one tommand kind that is sent rather than navigated — the exactly-once tommand — explodes here at<br/>
/// declaration, pointing at its own door.<br/>
/// Implemented by <see cref="TypermediaHandlerRegistrar"/>, whose docs carry the full handler semantics.</summary>
public interface ITypermediaTommandHandlerRegistrar
{
   ///<summary>Registers the handler for <typeparamref name="TTommand"/> — a navigated tommand whose type declares no result.<br/>
   /// The handler receives the <see cref="IUnitOfWorkResolver"/> of the unit of work its execution IS — a tommand mutates<br/>
   /// state, so its effects commit or roll back as a whole.</summary>
   ITypermediaTommandHandlerRegistrar ForTommand<TTommand>(Func<TTommand, IUnitOfWorkResolver, Task> handler) where TTommand : ITommand;

   ///<summary>Registers the handler for <typeparamref name="TTommand"/>, whose result answers the caller.</summary>
   ITypermediaTommandHandlerRegistrar ForTommand<TTommand, TResult>(Func<TTommand, IUnitOfWorkResolver, Task<TResult>> handler) where TTommand : ITommand<TResult>;
}
