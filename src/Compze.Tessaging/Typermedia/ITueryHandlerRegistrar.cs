using Compze.DependencyInjection.Abstractions;
using Compze.Tessaging.TessageTypes;

namespace Compze.Tessaging.Typermedia;

///<summary>The minimal registrar for tuery handlers — what an endpoint-declaration's <c>RegisterTueryHandlers</c><br/>
/// override receives.<br/>
/// Implemented by <see cref="TypermediaHandlerRegistrar"/>, whose docs carry the full handler semantics.</summary>
public interface ITueryHandlerRegistrar
{
   ///<summary>Registers the handler for <typeparamref name="TTuery"/>. Tuery handlers receive a plain <see cref="IScopeResolver"/>,<br/>
   /// deliberately: a tuery changes nothing, so its execution is a scope, not a unit of work.</summary>
   ITueryHandlerRegistrar ForTuery<TTuery, TResult>(Func<TTuery, IScopeResolver, Task<TResult>> handler) where TTuery : ITuery<TResult>;
}
