using System;
using System.Collections.Generic;
using AccountManagement.API;
using AccountManagement.Domain;
using AccountManagement.Domain.Events;
using AccountManagement.Domain.Passwords;
using CommunityToolkit.Diagnostics;
using Compze.Tessaging;
using Compze.Tessaging.Abstractions;
using Compze.Tessaging.Hosting;
using Compze.Tessaging.Hosting.Abstractions;
using Compze.Tessaging.Persistence.EventStore;
using Compze.Tessaging.Teventive.EventStore.Query.Models.SelfGeneratingQueryModels;
using Compze.Tessaging.Typermedia;
using Compze.Tessaging.Typermedia.Abstractions;

namespace AccountManagement.UI.QueryModels;

class AccountQueryModel : SelfGeneratingQueryModel<AccountQueryModel, AccountEvent.Root>, IAccountResourceData
{
   public Password Password { get; private set; } = null!; //Nullable status guaranteed by AssertInvariantsAreMet
   public Email Email { get; private set; } = null!;       //Nullable status guaranteed by AssertInvariantsAreMet

   AccountQueryModel(IEnumerable<AccountEvent.Root> events)
   {
      RegisterEventAppliers()
        .For<AccountEvent.PropertyUpdated.Email>(@event => Email = @event.Email)
        .For<AccountEvent.PropertyUpdated.Password>(@event => Password = @event.Password)
        .IgnoreUnhandled<AccountEvent.LoginAttempted>();

      LoadFromHistory(events);
   }

   protected override void AssertInvariantsAreMet()
   {
      Guard.IsNotNull(Email);
      Guard.IsNotNull(Password);
   }

   // ReSharper disable MemberCanBeMadeStatic.Global fluent composable APIs and statics do not mix
   internal class Api
   {
      internal Query Queries => new();
      internal class Query
      {
         public MessageTypes.StrictlyLocal.Queries.EntityLink<AccountQueryModel> Get(Guid id) => new(id);
      }

      public static void RegisterHandlers(MessageHandlerRegistrarWithDependencyInjectionSupport registrar) => Get(registrar);

      static void Get(MessageHandlerRegistrarWithDependencyInjectionSupport registrar) => registrar.ForQuery(
         (MessageTypes.StrictlyLocal.Queries.EntityLink<AccountQueryModel> query, ILocalHypermediaNavigator navigator) =>
            new AccountQueryModel(navigator.Execute(new EventStoreApi().Queries.GetHistory<AccountEvent.Root>(query.EntityId))));
   }
}