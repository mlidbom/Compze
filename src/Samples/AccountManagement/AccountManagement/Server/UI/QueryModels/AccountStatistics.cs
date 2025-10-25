using System;
using System.Threading;
using AccountManagement.Domain.Events;
using Compze.Abstractions.Tessaging.Hosting.Public;
using Compze.Abstractions.Tessaging.Hosting.TessageHandling.Registration.Public;
using Compze.Abstractions.Tessaging.Typermedia.Public;
using Compze.Sql.DocumentDb;
using Compze.Tessaging.Teventive.EventStore.Query.Models.SelfGeneratingQueryModels;
using Compze.Utilities.DependencyInjection;
using Compze.Utilities.Functional;

namespace AccountManagement.UI.QueryModels;

static class AccountStatistics
{
   /// <summary>
   /// Note that we use a <see cref="SelfGeneratingQueryModel{TQueryModel,TAggregateEvent}"/> even though we will store it in a document database.
   /// Doing so lets the query model cleanly encapsulate how it maintains its own state when it receives events.
   /// </summary>
   public class SingletonStatisticsQueryModel : SelfGeneratingQueryModel<SingletonStatisticsQueryModel, AccountEvent.Root>
   {
      public SingletonStatisticsQueryModel()
      {
         RegisterEventAppliers()
           .For<AccountEvent.Created>(_ => NumberOfAccounts++)
           .For<AccountEvent.LoginAttempted>(_ => NumberOfLoginsAttempts++)
           .For<AccountEvent.LoggedIn>(_ => NumberOfSuccessfulLogins++)
           .For<AccountEvent.LoginFailed>(_ => NumberOfFailedLogins++);
      }

      public int NumberOfAccounts { get; private set; }
      public int NumberOfLoginsAttempts { get; private set; }
      public int NumberOfSuccessfulLogins { get; private set; }
      public int NumberOfFailedLogins { get; private set; }

      //Since this is a singleton query model and not bound to a specific Aggregate's events we override the Id member to always be the singleton Id.
      public override Guid Id => StaticId;
      internal static Guid StaticId = Guid.Parse("93498554-5C2E-4D6A-862D-2DA7BCCAC747");
   }

   static void MaintainStatisticsWhenRelevantEventsAreReceived(TessageHandlerRegistrarWithDependencyInjectionSupport registrar) => registrar.ForEvent(
      (AccountEvent.Root @event, IInProcessHypermediaNavigator navigator, StatisticsSingletonInitializer initializer) =>
      {
         initializer.EnsureInitialized(navigator);

         if(new SingletonStatisticsQueryModel().HandlesEvent(@event))
         {
            navigator.Execute(new DocumentDbApi().Queries.GetForUpdate<SingletonStatisticsQueryModel>(SingletonStatisticsQueryModel.StaticId))
                     .ApplyEvent(@event);
         }
      });

   internal static void Register(IEndpointBuilder builder)
   {
      builder.Container.Register(Singleton.For<StatisticsSingletonInitializer>().CreatedBy(() => new StatisticsSingletonInitializer()));
      MaintainStatisticsWhenRelevantEventsAreReceived(builder.RegisterHandlers);
   }

   class StatisticsSingletonInitializer
   {
      readonly Lock _lock = new Lock();
      bool _isInitialized;
      readonly DocumentDbApi _documentDbApi = new();
      public void EnsureInitialized(IInProcessHypermediaNavigator navigator)
      {
         lock(_lock)
         {
            if(!_isInitialized)
            {
               _isInitialized = true;
               if(navigator.Execute(_documentDbApi.Queries.TryGet<SingletonStatisticsQueryModel>(SingletonStatisticsQueryModel.StaticId)) is None<SingletonStatisticsQueryModel>)
               {
                  navigator.Execute(_documentDbApi.Commands.Save(new SingletonStatisticsQueryModel()));
               }
            }
         }
      }
   }
}