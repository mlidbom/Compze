using Compze.Abstractions.Tessaging.Public;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;

namespace Compze.Typermedia.HandlerRegistration;

public interface ITypermediaHandlerRegistrar
{
   ///<summary>Registers the handler for <typeparamref name="TTommand"/>. Tommand handlers receive the<br/>
   /// <see cref="IUnitOfWorkResolver"/> of the unit of work their execution IS: a tommand mutates state, so every path that<br/>
   /// executes one runs it inside a unit of work, and its effects commit or roll back as a whole.</summary>
   ITypermediaHandlerRegistrar ForTommand<TTommand>(Action<TTommand, IUnitOfWorkResolver> handler) where TTommand : ITommand;

   ///<summary>Registers the handler for <typeparamref name="TTommand"/>, whose result answers the caller — see <see cref="ForTommand{TTommand}"/>.</summary>
   ITypermediaHandlerRegistrar ForTommand<TTommand, TResult>(Func<TTommand, IUnitOfWorkResolver, TResult> handler) where TTommand : ITommand<TResult>;

   ///<summary>Registers the handler for <typeparamref name="TTuery"/>. Tuery handlers receive a plain <see cref="IScopeResolver"/>,<br/>
   /// deliberately: a tuery changes nothing, so its execution is a scope, not a unit of work — no transaction is demanded, and<br/>
   /// when the caller has one the reads simply join its consistency.</summary>
   ITypermediaHandlerRegistrar ForTuery<TTuery, TResult>(Func<TTuery, IScopeResolver, TResult> handler) where TTuery : ITuery<TResult>;
}
