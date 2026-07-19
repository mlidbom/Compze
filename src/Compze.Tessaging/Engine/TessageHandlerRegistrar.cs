using Compze.Abstractions.Tessaging.Public;
using Compze.Contracts;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.SystemCE.ReflectionCE;
using Compze.Teventive.Tevents.Public;

namespace Compze.Tessaging.Engine;

///<summary>The declaration surface for an engine's handlers — one registrar covering all four tessage kinds, because the<br/>
/// tessage's own type carries its kind, guarantee, and synchrony, so the verbs differ only by handler shape. Handed to the<br/>
/// <see cref="LocalTessagingEngineBuilder.RegisterTessageHandlers"/> callback and existing only inside it: the callback's end is<br/>
/// the registration's end, so nothing can hold a registrar and mutate the engine later — using one after its callback explodes.</summary>
///<remarks>Synchrony follows the type: exactly-once kinds are async end to end — an exactly-once handler transactionally<br/>
/// modifies a database by construction, so registering a synchronous handler for one explodes at declaration — while<br/>
/// strictly-local kinds keep sync as first-class, with async available for handlers that read actual stores. Convenience<br/>
/// overloads that resolve extra lambda parameters from the handling context live in <see cref="TessageHandlerRegistrarCE"/>.</remarks>
public sealed class TessageHandlerRegistrar
{
   readonly TessageHandlerRegistrations _registrations;
   bool _callbackHasEnded;

   internal TessageHandlerRegistrar(TessageHandlerRegistrations registrations) => _registrations = registrations;

   ///<summary>Registers a handler for tevents compatible with <typeparamref name="TTevent"/>. The handler receives the<br/>
   /// <see cref="IUnitOfWorkResolver"/> of the unit of work delivering the tevent: a local publish delivers inside the<br/>
   /// publisher's unit of work, an exactly-once arrival inside the inbox processing's own, and a best-effort arrival inside the<br/>
   /// direct dispatch's own. A handler registered here never runs outside one — delivery detached from any transaction is<br/>
   /// observation, declared under its own verb (<see cref="LocalTessagingEngineBuilder.ObserveTevents"/>).</summary>
   internal TessageHandlerRegistrar ForTevent<TTevent>(Func<TTevent, IUnitOfWorkResolver, Task> handler) where TTevent : ITevent
   {
      AssertUsedOnlyInsideItsCallback();
      _registrations.AddTeventHandler(handler);
      return this;
   }

   ///<summary>The synchronous form of <see cref="ForTevent{TTevent}(Func{TTevent,IUnitOfWorkResolver,Task})"/> — first-class for<br/>
   /// subscriptions whose kind is not exactly-once; a subscription demanding exactly-once delivery explodes here, because<br/>
   /// exactly-once kinds are async end to end.</summary>
   internal TessageHandlerRegistrar ForTevent<TTevent>(Action<TTevent, IUnitOfWorkResolver> handler) where TTevent : ITevent
   {
      AssertUsedOnlyInsideItsCallback();
      AssertSubscriptionAllowsSynchronousHandlers<TTevent>();
      _registrations.AddTeventHandler<TTevent>((tevent, unitOfWork) =>
      {
         handler(tevent, unitOfWork);
         return Task.CompletedTask;
      });
      return this;
   }

   ///<summary>Registers the handler for <typeparamref name="TTommand"/> — a tommand whose type declares no result. The handler<br/>
   /// receives the <see cref="IUnitOfWorkResolver"/> of the unit of work its execution IS: a tommand mutates state, so every<br/>
   /// path that executes one runs it inside a unit of work, and its effects commit or roll back as a whole.</summary>
   public TessageHandlerRegistrar ForTommand<TTommand>(Func<TTommand, IUnitOfWorkResolver, Task> handler) where TTommand : ITommand
   {
      AssertUsedOnlyInsideItsCallback();
      _registrations.AddVoidTommandHandler(handler);
      return this;
   }

   ///<summary>The synchronous form of <see cref="ForTommand{TTommand}(Func{TTommand,IUnitOfWorkResolver,Task})"/> — first-class<br/>
   /// for strictly-local and at-most-once typermedia tommands; an exactly-once tommand explodes here, because exactly-once kinds<br/>
   /// are async end to end.</summary>
   internal TessageHandlerRegistrar ForTommand<TTommand>(Action<TTommand, IUnitOfWorkResolver> handler) where TTommand : ITommand
   {
      AssertUsedOnlyInsideItsCallback();
      AssertTommandKindAllowsSynchronousHandlers<TTommand>();
      _registrations.AddVoidTommandHandler<TTommand>((tommand, unitOfWork) =>
      {
         handler(tommand, unitOfWork);
         return Task.CompletedTask;
      });
      return this;
   }

   ///<summary>Registers the handler for <typeparamref name="TTommand"/>, whose result answers the caller — see<br/>
   /// <see cref="ForTommand{TTommand}(Func{TTommand,IUnitOfWorkResolver,Task})"/>.</summary>
   public TessageHandlerRegistrar ForTommand<TTommand, TResult>(Func<TTommand, IUnitOfWorkResolver, Task<TResult>> handler) where TTommand : ITommand<TResult>
   {
      AssertUsedOnlyInsideItsCallback();
      _registrations.AddTommandHandlerWithResult(handler);
      return this;
   }

   ///<summary>The synchronous form of <see cref="ForTommand{TTommand,TResult}(Func{TTommand,IUnitOfWorkResolver,Task{TResult}})"/> —<br/>
   /// first-class: today's result-bearing tommand kinds (strictly-local and at-most-once typermedia) are not exactly-once.</summary>
   public TessageHandlerRegistrar ForTommand<TTommand, TResult>(Func<TTommand, IUnitOfWorkResolver, TResult> handler) where TTommand : ITommand<TResult>
   {
      AssertUsedOnlyInsideItsCallback();
      _registrations.AddTommandHandlerWithResult<TTommand, TResult>((tommand, unitOfWork) => Task.FromResult(handler(tommand, unitOfWork)));
      return this;
   }

   ///<summary>Registers the handler for <typeparamref name="TTuery"/>. Tuery handlers receive a plain <see cref="IScopeResolver"/>,<br/>
   /// deliberately: a tuery changes nothing, so its execution is a scope, not a unit of work — no transaction is demanded, and<br/>
   /// when the caller has one the reads simply join its consistency.</summary>
   public TessageHandlerRegistrar ForTuery<TTuery, TResult>(Func<TTuery, IScopeResolver, Task<TResult>> handler) where TTuery : ITuery<TResult>
   {
      AssertUsedOnlyInsideItsCallback();
      _registrations.AddTueryHandler(handler);
      return this;
   }

   ///<summary>The synchronous form of <see cref="ForTuery{TTuery,TResult}(Func{TTuery,IScopeResolver,Task{TResult}})"/> —<br/>
   /// first-class: a tuery's synchrony is the caller's business, not a delivery guarantee's.</summary>
   internal TessageHandlerRegistrar ForTuery<TTuery, TResult>(Func<TTuery, IScopeResolver, TResult> handler) where TTuery : ITuery<TResult>
   {
      AssertUsedOnlyInsideItsCallback();
      _registrations.AddTueryHandler<TTuery, TResult>((tuery, scope) => Task.FromResult(handler(tuery, scope)));
      return this;
   }

   internal void EndCallback() => _callbackHasEnded = true;

   void AssertUsedOnlyInsideItsCallback() =>
      State.Assert(!_callbackHasEnded,
                   () => $"This {nameof(TessageHandlerRegistrar)}'s declaration callback has ended, and the registrar exists only inside it: the callback's end is the registration's end, so nothing can hold a registrar and mutate the engine later. Declare every handler inside the {nameof(LocalTessagingEngineBuilder.RegisterTessageHandlers)} callback.");

   static void AssertSubscriptionAllowsSynchronousHandlers<TTevent>() where TTevent : ITevent =>
      State.Assert(!PublisherTevent.WrapperTypeMatchingAllWrappingsOf(typeof(TTevent)).Is<IPublisherTevent<IExactlyOnceTevent>>(),
                   () => $"A subscription to {typeof(TTevent).FullName} demands exactly-once delivery, and exactly-once kinds are async end to end: an exactly-once handler transactionally modifies a database by construction, and that does not happen synchronously. Register an async handler ({nameof(Task)}-returning) instead.");

   static void AssertTommandKindAllowsSynchronousHandlers<TTommand>() where TTommand : ITommand =>
      State.Assert(!typeof(TTommand).Is<IExactlyOnceTommand>(),
                   () => $"{typeof(TTommand).FullName} is an exactly-once tommand, and exactly-once kinds are async end to end: an exactly-once handler transactionally modifies a database by construction, and that does not happen synchronously. Register an async handler ({nameof(Task)}-returning) instead.");
}
