using System;
using Compze.Utilities.SystemCE.LinqCE;

// ReSharper disable All
#pragma warning disable //Reviewed OK: This is API experimental code that is never ever used.

//todo: Remove this draft API code after looking through it for ideas that are still useful
namespace Compze.Tests.ScratchPad.APIDraft._01;

class APIDraft01
{
   class TessageHandler<TImplementation>
   {
      public TessageHandler<TImplementation> ForEvent<TEvent>(Action<TImplementation, TEvent> action) => this;
      public TessageHandler<TImplementation> ForCommand<TCommand>(Action<TImplementation, TCommand> action) => this;
      public TessageHandler<TImplementation> ForQuery<TQuery, TResult>(Func<TImplementation, TQuery, TResult> action) => this;


   }

   class TessageHandler
   {
      public TessageHandler ForEvent<TEvent>(Action<TEvent> action) => this;

      public TessageHandler ForCommand<TCommand>(Action<TCommand> action) => this;

      public TessageHandler ForQuery<TQuery, TResult>(Func<TQuery, TResult> action) => this;
   }

   class TessageHandlerGroup
   {
      public TessageHandlerGroup(params Object[] children) {}
      public TessageHandlerGroup Add(TessageHandlerGroup child) => this;
      public TessageHandlerGroup Add(TessageHandler handler) => this;
   }

   class TessageHandlerGroupSettings
   {
      //int MaximumThreads = 1;
   }

   class TessageHandlerSettings {}

   enum TessageHandlerGroupFlags
   {
      RunHandlersInParallelWithEachOther,
   }


   enum TessageHandlerFlags
   {
      HandleTessagesInParallel
   }

   class Endpoint
   {
      public Endpoint(params TessageHandlerGroup[] handlerGroups) { handlerGroups.ForEach(Add); }

      protected void Add(TessageHandlerGroup obj) {}
   }

   class AccountEndpoint : Endpoint
   {
      public AccountEndpoint()
      {
         Add(new TessageHandlerGroup(
                new TessageHandler<AccountQueryModelUpdater>()
                  .ForEvent<AccountCreatedEvent>((handler, @event) => handler.Handle(@event)),
                new TessageHandler<AccountCommandHandler>()
                  .ForCommand<CreateAccountCommand>((handler, command) => handler.Handle(command)),
                new TessageHandler<AccountQueryHandler>()
                  .ForQuery<GetAccountQuery, string>((handler, query) => handler.Handle(query)),
                new TessageHandler<AccountController>()
                  .ForEvent<AccountCreatedEvent>((handler, @event) => handler.Handle(@event))
                  .ForCommand<CreateAccountCommand>((handler, command) => handler.Handle(command))
                  .ForQuery<GetAccountQuery, string>((handler, query) => handler.Handle(query))
             ));

         Add(new TessageHandlerGroup(
                new TessageHandler()
             ));
      }
   }

   class ForumsEndpoint : Endpoint {}

   class GlobalBus
   {
      public GlobalBus Register(params Endpoint[] endpoints) => this;
      public GlobalBus RegisterEndPoint<T>() => this;
   }

   class APiTest
   {
      void Setup()
      {
         var bus = new GlobalBus();

         //Use the type so that we can make use of injection in the configuration of the endpoints.
         bus.RegisterEndPoint<AccountEndpoint>()
            .RegisterEndPoint<ForumsEndpoint>();

         var total = new TessageHandlerGroup(
            TessageHandlerGroupFlags.RunHandlersInParallelWithEachOther,
            new TessageHandlerGroup(
               new TessageHandler()
            ),
            new TessageHandlerGroup()
         );
      }
   }

}

#pragma warning restore // Remove unused parameter