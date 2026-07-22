using Compze.Contracts;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.SystemCE.ReflectionCE;
using Compze.Tessaging.Engine;
using Compze.Tessaging.Engine.HandlerRegistration._private;
using Compze.Tessaging.TessageTypes;

namespace Compze.Tessaging.TessageBus;

///<summary>The declaration surface for an engine's TessageBus handlers — the two tessage kinds the bus's doors serve: tevents<br/>
/// (published) and exactly-once tommands (sent — <see cref="IUnitOfWorkTommandSender.SendAsync"/> accepts only<br/>
/// <see cref="IExactlyOnceTommand"/>, and the constraints here mirror its doors). Every other tommand kind is navigated, so its<br/>
/// handler is Typermedia's, declared through <see cref="LocalTessagingEngineBuilder.RegisterTypermediaHandlers"/>. Handed to the<br/>
/// <see cref="LocalTessagingEngineBuilder.RegisterTessageBusHandlers"/> callback and existing only inside it: the callback's end<br/>
/// is the registration's end, so nothing can hold a registrar and mutate the engine later — using one after its callback<br/>
/// explodes.</summary>
///<remarks>Synchrony follows the type: exactly-once kinds are async end to end — an exactly-once handler transactionally<br/>
/// modifies a database by construction, so tommand handlers here have no synchronous form at all, and a subscription demanding<br/>
/// exactly-once tevent delivery explodes when handed a synchronous handler. Convenience overloads that resolve extra lambda<br/>
/// parameters from the handling context live in <see cref="TessageBusHandlerRegistrarCE"/>.</remarks>
public sealed class TessageBusHandlerRegistrar : IExactlyOnceTommandHandlerRegistrar, IExactlyOnceTeventHandlerRegistrar, IBestEffortTeventHandlerRegistrar
{
   readonly TessageHandlerRegistrations _registrations;
   bool _callbackHasEnded;

   internal TessageBusHandlerRegistrar(TessageHandlerRegistrations registrations) => _registrations = registrations;

   ///<summary>Registers a handler for tevents compatible with <typeparamref name="TTevent"/>. The handler receives the<br/>
   /// <see cref="IUnitOfWorkResolver"/> of the unit of work delivering the tevent: a local publish delivers inside the<br/>
   /// publisher's unit of work, an exactly-once arrival inside the inbox processing's own, and a best-effort arrival inside the<br/>
   /// direct dispatch's own. A handler registered here never runs outside one — delivery detached from any transaction is<br/>
   /// observation, declared under its own verb (<see cref="LocalTessagingEngineBuilder.ObserveTevents"/>).</summary>
   internal TessageBusHandlerRegistrar ForTevent<TTevent>(Func<TTevent, IUnitOfWorkResolver, Task> handler) where TTevent : ITevent
   {
      AssertUsedOnlyInsideItsCallback();
      _registrations.AddTeventHandler(handler);
      return this;
   }

   ///<summary>The synchronous form of <see cref="ForTevent{TTevent}(Func{TTevent,IUnitOfWorkResolver,Task})"/> — first-class for<br/>
   /// subscriptions whose kind is not exactly-once; a subscription demanding exactly-once delivery explodes here, because<br/>
   /// exactly-once kinds are async end to end.</summary>
   internal TessageBusHandlerRegistrar ForTevent<TTevent>(Action<TTevent, IUnitOfWorkResolver> handler) where TTevent : ITevent
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

   ///<summary>Registers the handler for <typeparamref name="TTommand"/> — an exactly-once tommand, the one tommand kind the<br/>
   /// bus's send door serves. The handler receives the <see cref="IUnitOfWorkResolver"/> of the unit of work its execution IS:<br/>
   /// a tommand mutates state, so every path that executes one runs it inside a unit of work, and its effects commit or roll<br/>
   /// back as a whole. Async only, no synchronous form: exactly-once kinds are async end to end.</summary>
   public TessageBusHandlerRegistrar ForTommand<TTommand>(Func<TTommand, IUnitOfWorkResolver, Task> handler) where TTommand : IExactlyOnceTommand
   {
      AssertUsedOnlyInsideItsCallback();
      _registrations.AddVoidTommandHandler(handler);
      return this;
   }

   //The declaration doors an endpoint-declaration's overrides receive - each showing only its facet of this registrar, with
   //the door's guarantee-fit asserted at declaration.
   IExactlyOnceTommandHandlerRegistrar IExactlyOnceTommandHandlerRegistrar.ForTommand<TTommand>(Func<TTommand, IUnitOfWorkResolver, Task> handler)
   {
      ForTommand(handler);
      return this;
   }

   IExactlyOnceTeventHandlerRegistrar IExactlyOnceTeventHandlerRegistrar.ForTevent<TTevent>(Func<TTevent, IUnitOfWorkResolver, Task> handler)
   {
      AssertSubscriptionDemandsExactlyOnceDelivery<TTevent>();
      ForTevent(handler);
      return this;
   }

   IBestEffortTeventHandlerRegistrar IBestEffortTeventHandlerRegistrar.ForTevent<TTevent>(Func<TTevent, IUnitOfWorkResolver, Task> handler)
   {
      AssertSubscriptionDoesNotDemandExactlyOnceDelivery<TTevent>();
      ForTevent(handler);
      return this;
   }

   IBestEffortTeventHandlerRegistrar IBestEffortTeventHandlerRegistrar.ForTevent<TTevent>(Action<TTevent, IUnitOfWorkResolver> handler)
   {
      AssertSubscriptionDoesNotDemandExactlyOnceDelivery<TTevent>();
      ForTevent(handler);
      return this;
   }

   internal void EndCallback() => _callbackHasEnded = true;

   void AssertUsedOnlyInsideItsCallback() =>
      State.Assert(!_callbackHasEnded,
                   () => $"This {nameof(TessageBusHandlerRegistrar)}'s declaration callback has ended, and the registrar exists only inside it: the callback's end is the registration's end, so nothing can hold a registrar and mutate the engine later. Declare every handler inside the {nameof(LocalTessagingEngineBuilder.RegisterTessageBusHandlers)} callback.");

   static void AssertSubscriptionAllowsSynchronousHandlers<TTevent>() where TTevent : ITevent =>
      State.Assert(!SubscriptionDemandsExactlyOnceDelivery<TTevent>(),
                   () => $"A subscription to {typeof(TTevent).FullName} demands exactly-once delivery, and exactly-once kinds are async end to end: an exactly-once handler transactionally modifies a database by construction, and that does not happen synchronously. Register an async handler ({nameof(Task)}-returning) instead.");

   static void AssertSubscriptionDemandsExactlyOnceDelivery<TTevent>() where TTevent : ITevent =>
      State.Assert(SubscriptionDemandsExactlyOnceDelivery<TTevent>(),
                   () => $"A subscription to {typeof(TTevent).FullName} does not demand exactly-once delivery, so it does not belong behind the exactly-once tevent door. Register it through the best-effort tevent door (RegisterBestEffortTeventHandlers).");

   static void AssertSubscriptionDoesNotDemandExactlyOnceDelivery<TTevent>() where TTevent : ITevent =>
      State.Assert(!SubscriptionDemandsExactlyOnceDelivery<TTevent>(),
                   () => $"A subscription to {typeof(TTevent).FullName} demands exactly-once delivery, which only the exactly-once tier's durable vertical can honor. Register it through the exactly-once tevent door (RegisterExactlyOnceTeventHandlers) of an exactly-once endpoint's declaration.");

   static bool SubscriptionDemandsExactlyOnceDelivery<TTevent>() where TTevent : ITevent =>
      PublisherTevent.WrapperTypeMatchingAllWrappingsOf(typeof(TTevent)).Is<IPublisherTevent<IExactlyOnceTevent>>();
}
