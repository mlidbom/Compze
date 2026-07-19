using System.Transactions;
using Compze.Contracts;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Compze.Tessaging.Abstractions;
using Compze.Tessaging.Abstractions.TessageTypes;
using JetBrains.Annotations;

namespace Compze.Tessaging.Hosting;

static class IndependentTommandSenderRegistrar
{
   public static IComponentRegistrar IndependentTommandSender(this IComponentRegistrar registrar)
      => registrar.Register(Hosting.IndependentTommandSender.RegisterWith);
}

///<summary>The <see cref="IIndependentTommandSender"/>: runs each send as its own unit of work — <c>ExecuteUnitOfWork</c><br/>
/// around the endpoint's <see cref="IUnitOfWorkTommandSender"/> — after asserting that the caller stands outside any ambient<br/>
/// transaction, so an independent send can never silently join a caller's unit of work.</summary>
[UsedImplicitly] class IndependentTommandSender : IIndependentTommandSender
{
   public static void RegisterWith(IComponentRegistrar registrar)
      => registrar.Register(Singleton.For<IIndependentTommandSender>()
                                     .CreatedBy((IScopeFactory scopeFactory) => new IndependentTommandSender(scopeFactory)));

   readonly IScopeFactory _scopeFactory;

   IndependentTommandSender(IScopeFactory scopeFactory) => _scopeFactory = scopeFactory;

   public async Task SendAsync(IExactlyOnceTommand tommand)
   {
      State.Assert(Transaction.Current == null,
                   () => $"{nameof(IIndependentTommandSender)} was called from within an ambient transaction — inside a unit of work. An independent send runs as its own unit of work; called here it would silently join the caller's transaction instead of standing alone. Send through {nameof(IUnitOfWorkTommandSender)}, which deliberately joins the caller's unit of work.");
      await _scopeFactory.ExecuteUnitOfWorkAsync(async unitOfWork => await unitOfWork.Resolve<IUnitOfWorkTommandSender>().SendAsync(tommand).caf()).caf();
   }
}
