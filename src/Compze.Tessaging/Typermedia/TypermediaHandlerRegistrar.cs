using Compze.Contracts;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.SystemCE.ReflectionCE;
using Compze.Tessaging.Engine;
using Compze.Tessaging.Engine.HandlerRegistration._private;
using Compze.Tessaging.TessageTypes;

namespace Compze.Tessaging.Typermedia;

///<summary>The declaration surface for an engine's Typermedia handlers — the conversational kinds a caller navigates: tueries,<br/>
/// tommands whose type declares a result, and the void tommands a caller executes or posts expecting no answer (strictly-local<br/>
/// and at-most-once typermedia). The one tommand kind that is not navigated but sent — the exactly-once tommand — is the bus's,<br/>
/// declared through <see cref="LocalTessagingEngineBuilder.RegisterTessageBusHandlers"/>, and registering one here explodes at<br/>
/// declaration. Handed to the <see cref="LocalTessagingEngineBuilder.RegisterTypermediaHandlers"/> callback and existing only<br/>
/// inside it: the callback's end is the registration's end, so nothing can hold a registrar and mutate the engine later — using<br/>
/// one after its callback explodes.</summary>
///<remarks>Synchrony follows the type here too, and no navigated tommand kind is exactly-once, so the synchronous forms are<br/>
/// first-class; a tuery's synchrony is the caller's business, not a delivery guarantee's. Convenience overloads that resolve<br/>
/// extra lambda parameters from the handling context live in <see cref="TypermediaHandlerRegistrarCE"/>.</remarks>
public sealed class TypermediaHandlerRegistrar : ITypermediaTommandHandlerRegistrar, ITueryHandlerRegistrar
{
   readonly TessageHandlerRegistrations _registrations;
   bool _callbackHasEnded;

   internal TypermediaHandlerRegistrar(TessageHandlerRegistrations registrations) => _registrations = registrations;

   ///<summary>Registers the handler for <typeparamref name="TTommand"/> — a navigated tommand whose type declares no result:<br/>
   /// the caller executes or posts it expecting no answer. The handler receives the <see cref="IUnitOfWorkResolver"/> of the unit<br/>
   /// of work its execution IS: a tommand mutates state, so every path that executes one runs it inside a unit of work, and its<br/>
   /// effects commit or roll back as a whole. An exactly-once tommand explodes here: it is sent, not navigated — the bus's kind.</summary>
   public TypermediaHandlerRegistrar ForTommand<TTommand>(Func<TTommand, IUnitOfWorkResolver, Task> handler) where TTommand : ITommand
   {
      AssertUsedOnlyInsideItsCallback();
      AssertTommandIsANavigatedKind<TTommand>();
      _registrations.AddVoidTommandHandler(handler);
      return this;
   }

   ///<summary>The synchronous form of <see cref="ForTommand{TTommand}(Func{TTommand,IUnitOfWorkResolver,Task})"/> — first-class:<br/>
   /// no navigated tommand kind is exactly-once, so the async-only rule for exactly-once kinds cannot apply here.</summary>
   internal TypermediaHandlerRegistrar ForTommand<TTommand>(Action<TTommand, IUnitOfWorkResolver> handler) where TTommand : ITommand
   {
      AssertUsedOnlyInsideItsCallback();
      AssertTommandIsANavigatedKind<TTommand>();
      _registrations.AddVoidTommandHandler<TTommand>((tommand, unitOfWork) =>
      {
         handler(tommand, unitOfWork);
         return Task.CompletedTask;
      });
      return this;
   }

   ///<summary>Registers the handler for <typeparamref name="TTommand"/>, whose result answers the caller. The handler receives<br/>
   /// the <see cref="IUnitOfWorkResolver"/> of the unit of work its execution IS: a tommand mutates state, so every path that<br/>
   /// executes one runs it inside a unit of work, and its effects commit or roll back as a whole.</summary>
   public TypermediaHandlerRegistrar ForTommand<TTommand, TResult>(Func<TTommand, IUnitOfWorkResolver, Task<TResult>> handler) where TTommand : ITommand<TResult>
   {
      AssertUsedOnlyInsideItsCallback();
      _registrations.AddTommandHandlerWithResult(handler);
      return this;
   }

   ///<summary>The synchronous form of <see cref="ForTommand{TTommand,TResult}(Func{TTommand,IUnitOfWorkResolver,Task{TResult}})"/> —<br/>
   /// first-class: today's result-bearing tommand kinds (strictly-local and at-most-once typermedia) are not exactly-once.</summary>
   internal TypermediaHandlerRegistrar ForTommand<TTommand, TResult>(Func<TTommand, IUnitOfWorkResolver, TResult> handler) where TTommand : ITommand<TResult>
   {
      AssertUsedOnlyInsideItsCallback();
      _registrations.AddTommandHandlerWithResult<TTommand, TResult>((tommand, unitOfWork) => Task.FromResult(handler(tommand, unitOfWork)));
      return this;
   }

   ///<summary>Registers the handler for <typeparamref name="TTuery"/>. Tuery handlers receive a plain <see cref="IScopeResolver"/>,<br/>
   /// deliberately: a tuery changes nothing, so its execution is a scope, not a unit of work — no transaction is demanded, and<br/>
   /// when the caller has one the reads simply join its consistency.</summary>
   public TypermediaHandlerRegistrar ForTuery<TTuery, TResult>(Func<TTuery, IScopeResolver, Task<TResult>> handler) where TTuery : ITuery<TResult>
   {
      AssertUsedOnlyInsideItsCallback();
      _registrations.AddTueryHandler(handler);
      return this;
   }

   ///<summary>The synchronous form of <see cref="ForTuery{TTuery,TResult}(Func{TTuery,IScopeResolver,Task{TResult}})"/> —<br/>
   /// first-class: a tuery's synchrony is the caller's business, not a delivery guarantee's.</summary>
   internal TypermediaHandlerRegistrar ForTuery<TTuery, TResult>(Func<TTuery, IScopeResolver, TResult> handler) where TTuery : ITuery<TResult>
   {
      AssertUsedOnlyInsideItsCallback();
      _registrations.AddTueryHandler<TTuery, TResult>((tuery, scope) => Task.FromResult(handler(tuery, scope)));
      return this;
   }

   //The declaration doors an endpoint-declaration's overrides receive - each showing only its facet of this registrar.
   ITypermediaTommandHandlerRegistrar ITypermediaTommandHandlerRegistrar.ForTommand<TTommand>(Func<TTommand, IUnitOfWorkResolver, Task> handler)
   {
      ForTommand(handler);
      return this;
   }

   ITypermediaTommandHandlerRegistrar ITypermediaTommandHandlerRegistrar.ForTommand<TTommand, TResult>(Func<TTommand, IUnitOfWorkResolver, Task<TResult>> handler)
   {
      ForTommand<TTommand, TResult>(handler);
      return this;
   }

   ITueryHandlerRegistrar ITueryHandlerRegistrar.ForTuery<TTuery, TResult>(Func<TTuery, IScopeResolver, Task<TResult>> handler)
   {
      ForTuery<TTuery, TResult>(handler);
      return this;
   }

   internal void EndCallback() => _callbackHasEnded = true;

   void AssertUsedOnlyInsideItsCallback() =>
      State.Assert(!_callbackHasEnded,
                   () => $"This {nameof(TypermediaHandlerRegistrar)}'s declaration callback has ended, and the registrar exists only inside it: the callback's end is the registration's end, so nothing can hold a registrar and mutate the engine later. Declare every handler inside the {nameof(LocalTessagingEngineBuilder.RegisterTypermediaHandlers)} callback.");

   static void AssertTommandIsANavigatedKind<TTommand>() where TTommand : ITommand =>
      State.Assert(!typeof(TTommand).Is<IExactlyOnceTommand>(),
                   () => $"{typeof(TTommand).FullName} is an exactly-once tommand: it is sent through the bus, not navigated, so its handler is the bus's. Register it through {nameof(LocalTessagingEngineBuilder.RegisterTessageBusHandlers)}.");
}
