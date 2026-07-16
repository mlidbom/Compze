using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using JetBrains.Annotations;
using Compze.Abstractions.Tessaging.Public;
using Compze.Abstractions.Tessaging.Validation;
using Compze.Tessaging.Implementation.Abstractions;
using Compze.Internals.SystemCE.UsageGuards;

namespace Compze.Tessaging.Hosting;

static class UnitOfWorkTommandSenderRegistrar
{
   public static IComponentRegistrar UnitOfWorkTommandSender(this IComponentRegistrar registrar)
      => registrar.Register(Hosting.UnitOfWorkTommandSender.RegisterWith);
}

[UsedImplicitly] class UnitOfWorkTommandSender : IUnitOfWorkTommandSender
{
   public static void RegisterWith(IComponentRegistrar registrar)
      => registrar.Register(UnitOfWork.For<IUnitOfWorkTommandSender>()
                                      .CreatedBy((IOutbox outbox)
                                                    => new UnitOfWorkTommandSender(outbox)));

   readonly IOutbox _transport;
   readonly IUsageGuard _contextGuard;

   public UnitOfWorkTommandSender(IOutbox transport)
   {
      _transport = transport;
      _contextGuard = new CombinationUsageGuard(new SingleTransactionUsageGuard(this));
   }

   public void Send(IExactlyOnceTommand tommand)
   {
      RunAssertions(tommand);
      _transport.SendTransactionally(tommand);
   }

   void RunAssertions(IExactlyOnceTommand tommand)
   {
      _contextGuard.EnsureAccessValid();
      TessageInspector.AssertValidToSendRemote(tommand);
      TommandValidator.AssertTommandIsValid(tommand);
   }
}
