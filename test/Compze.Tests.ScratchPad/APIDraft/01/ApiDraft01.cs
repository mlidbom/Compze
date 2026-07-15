using Compze.Internals.SystemCE.LinqCE;

// ReSharper disable All
#pragma warning disable //Reviewed OK: This is API experimental code that is never ever used.

namespace Compze.Tests.ScratchPad.APIDraft._01;

class APIDraft01
{
   class TessageHandler<TImplementation>
   {
      public TessageHandler<TImplementation> ForTevent<TTevent>(Action<TImplementation, TTevent> action) => this;
      public TessageHandler<TImplementation> ForTommand<TTommand>(Action<TImplementation, TTommand> action) => this;
      public TessageHandler<TImplementation> ForTuery<TTuery, TResult>(Func<TImplementation, TTuery, TResult> action) => this;


   }

   class TessageHandler
   {
      public TessageHandler ForTevent<TTevent>(Action<TTevent> action) => this;

      public TessageHandler ForTommand<TTommand>(Action<TTommand> action) => this;

      public TessageHandler ForTuery<TTuery, TResult>(Func<TTuery, TResult> action) => this;
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
                  .ForTevent<AccountCreatedTevent>((handler, tevent) => handler.Handle(tevent)),
                new TessageHandler<AccountTommandHandler>()
                  .ForTommand<CreateAccountTommand>((handler, tommand) => handler.Handle(tommand)),
                new TessageHandler<AccountTueryHandler>()
                  .ForTuery<GetAccountTuery, string>((handler, tuery) => handler.Handle(tuery)),
                new TessageHandler<AccountController>()
                  .ForTevent<AccountCreatedTevent>((handler, tevent) => handler.Handle(tevent))
                  .ForTommand<CreateAccountTommand>((handler, tommand) => handler.Handle(tommand))
                  .ForTuery<GetAccountTuery, string>((handler, tuery) => handler.Handle(tuery))
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
