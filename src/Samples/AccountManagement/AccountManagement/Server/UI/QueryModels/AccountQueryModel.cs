using System;
using System.Collections.Generic;
using AccountManagement.API;
using AccountManagement.Domain;
using AccountManagement.Domain.Events;
using AccountManagement.Domain.Passwords;
using CommunityToolkit.Diagnostics;
using Compze.Abstractions.Tessaging.Hosting.TessageHandling.Registration.Public;
using Compze.Abstractions.Tessaging.Public;
using Compze.Abstractions.Tessaging.Typermedia.Public;
using Compze.Tessaging.Teventive.EventStore.Tuery.Models.SelfGeneratingQueryModels;
using Compze.Tessaging.TyperMediaApi.EventStore;

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
      internal Tuery Queries => new();
      internal class Tuery
      {
         public TessageTypes.StrictlyLocal.Queries.EntityLink<AccountQueryModel> Get(Guid id) => new(id);
      }

      public static void RegisterHandlers(TessageHandlerRegistrarWithDependencyInjectionSupport registrar) => Get(registrar);

      static void Get(TessageHandlerRegistrarWithDependencyInjectionSupport registrar) => registrar.ForTuery(
         (TessageTypes.StrictlyLocal.Queries.EntityLink<AccountQueryModel> tuery, IInProcessHypermediaNavigator navigator) =>
            new AccountQueryModel(navigator.Execute(new EventStoreApi().Queries.GetHistory<AccountEvent.Root>(tuery.EntityId))));
   }
}