using Compze.Abstractions.Tessaging.Public;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Tessaging.TessageHandling.Registration.Public;
using Compze.Tessaging.Typermedia.HandlerRegistration;

namespace Compze.Tessaging.Engine;

///<summary>INTERIM: the per-style registrar interfaces adapted onto the one gathering surface, so every existing registration<br/>
/// site keeps compiling while the engine's parts land one green increment at a time. Dies within this migration phase, when the<br/>
/// engine builder's declaration surface — one registrar covering all four tessage kinds, with the target handler signatures —<br/>
/// replaces the per-style interfaces and every registration site moves to the declaration block.</summary>
public partial class TessageHandlerRegistrations : ITessageHandlerRegistrar, ITransactionIgnoringTeventHandlerRegistrar, ITypermediaHandlerRegistrar
{
   ITessageHandlerRegistrar ITessageHandlerRegistrar.ForTevent<TTevent>(Action<TTevent, IUnitOfWorkResolver> handler)
   {
      AddTeventHandler<TTevent>((tevent, unitOfWork) =>
      {
         handler(tevent, unitOfWork);
         return Task.CompletedTask;
      });
      return this;
   }

   ITessageHandlerRegistrar ITessageHandlerRegistrar.ForTommand<TTommand>(Action<TTommand, IUnitOfWorkResolver> handler)
   {
      AddVoidTommandHandler<TTommand>((tommand, unitOfWork) =>
      {
         handler(tommand, unitOfWork);
         return Task.CompletedTask;
      });
      return this;
   }

   ITransactionIgnoringTeventHandlerRegistrar ITransactionIgnoringTeventHandlerRegistrar.ForTevent<TTevent>(Action<TTevent, IScopeResolver> handler)
   {
      AddTeventObserver(handler);
      return this;
   }

   ITypermediaHandlerRegistrar ITypermediaHandlerRegistrar.ForTommand<TTommand>(Action<TTommand, IUnitOfWorkResolver> handler)
   {
      AddVoidTommandHandler<TTommand>((tommand, unitOfWork) =>
      {
         handler(tommand, unitOfWork);
         return Task.CompletedTask;
      });
      return this;
   }

   ITypermediaHandlerRegistrar ITypermediaHandlerRegistrar.ForTommand<TTommand, TResult>(Func<TTommand, IUnitOfWorkResolver, TResult> handler)
   {
      AddTommandHandlerWithResult<TTommand, TResult>((tommand, unitOfWork) => Task.FromResult(handler(tommand, unitOfWork)));
      return this;
   }

   ITypermediaHandlerRegistrar ITypermediaHandlerRegistrar.ForTuery<TTuery, TResult>(Func<TTuery, IScopeResolver, TResult> handler)
   {
      AddTueryHandler<TTuery, TResult>((tuery, scope) => Task.FromResult(handler(tuery, scope)));
      return this;
   }
}
