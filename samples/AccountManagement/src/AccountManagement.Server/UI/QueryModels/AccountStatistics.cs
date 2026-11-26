using AccountManagement.Domain.Tevents;
using Compze.DocumentDb;
using Compze.Abstractions;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Compze.Tessaging.TessageBus;
using Compze.Teventive;
using Compze.Teventive.TeventStore.QueryModels.SelfGeneratingQueryModels;
using Compze.Tessaging.Typermedia;

namespace AccountManagement.UI.QueryModels;

static class AccountStatistics
{
   /// <summary>
   /// Note that we use a <see cref="SelfGeneratingQueryModel{TQueryModel,TTaggregateTevent}"/> even though we will store it in a document database.
   /// Doing so lets the tuery model cleanly encapsulate how it maintains its own state when it receives tevents.
   /// </summary>
   public class SingletonStatisticsQueryModel : SelfGeneratingQueryModel<SingletonStatisticsQueryModel, IAccountTevent>
   {
      static readonly TeventRouting<SingletonStatisticsQueryModel, IAccountTevent> Appliers = DeclareAppliers(it => it
        .For<IAccountTevent.Created>(static (statistics, _) => statistics.NumberOfAccounts++)
        .For<IAccountTevent.LoginAttempted>(static (statistics, _) => statistics.NumberOfLoginsAttempts++)
        .For<IAccountTevent.LoggedIn>(static (statistics, _) => statistics.NumberOfSuccessfulLogins++)
        .For<IAccountTevent.LoginFailed>(static (statistics, _) => statistics.NumberOfFailedLogins++));

      protected override TeventRouting TeventAppliers => Appliers;

      int NumberOfAccounts { get; set; }
      int NumberOfLoginsAttempts { get; set; }
      int NumberOfSuccessfulLogins { get; set; }
      int NumberOfFailedLogins { get; set; }

      //Since this is a singleton tuery model and not bound to a specific Taggregate's tevents we override the Id member to always be the singleton Id.
      public override EntityId Id => new(StaticId);
      internal static Guid StaticId = Guid.Parse("93498554-5C2E-4D6A-862D-2DA7BCCAC747");
   }

   //Account tevents are exactly-once kinds, and exactly-once kinds are async end to end - so the handler is declared async, and navigates asynchronously within its unit of work.
   internal static void MaintainStatisticsWhenRelevantTeventsAreReceived(IExactlyOnceTeventHandlerRegistrar registrar) => registrar.ForTevent(
      async (IAccountTevent tevent, ILocalTypermediaNavigatorSession navigator, StatisticsSingletonInitializer initializer) =>
      {
         await initializer.EnsureInitializedAsync(navigator).caf();

         if(new SingletonStatisticsQueryModel().HandlesTevent(tevent))
         {
            (await navigator.ExecuteAsync(new DocumentDbApi().Tueries.GetForUpdate<SingletonStatisticsQueryModel>(SingletonStatisticsQueryModel.StaticId)).caf())
              .ApplyTevent(tevent);
         }
      });

   internal static void RegisterComponents(IComponentRegistrar registrar) =>
      registrar.Register(Singleton.For<StatisticsSingletonInitializer>().CreatedBy(() => new StatisticsSingletonInitializer()));

   class StatisticsSingletonInitializer
   {
      readonly DocumentDbApi _documentDbApi = new();

      ///<summary>Ensures the singleton document exists, inside the caller's own transaction and idempotently per invocation:<br/>
      /// no in-memory initialized-flag, deliberately — such a flag set inside a transaction outlives its rollback, and a<br/>
      /// failed first handling attempt would then skip initialization on every retry. The tevent handlers this serves never<br/>
      /// run in parallel with each other, so the check-then-save cannot race itself.</summary>
      public async Task EnsureInitializedAsync(ILocalTypermediaNavigatorSession navigator)
      {
         if(await navigator.ExecuteAsync(_documentDbApi.Tueries.TryGet<SingletonStatisticsQueryModel>(SingletonStatisticsQueryModel.StaticId)).caf() is null)
         {
            await navigator.ExecuteAsync(_documentDbApi.Tommands.Save(new SingletonStatisticsQueryModel())).caf();
         }
      }
   }
}
