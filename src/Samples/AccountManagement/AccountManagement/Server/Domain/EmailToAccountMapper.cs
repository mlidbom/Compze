using AccountManagement.Domain.Events;
using Compze.Persistence.DocumentDb;
using Compze.Tessaging.Hosting.Abstractions;
using Compze.Tessaging.Typermedia.Abstractions;
using Compze.Utilities.Functional;
using JetBrains.Annotations;
using AccountLink = Compze.Tessaging.Persistence.EventStore.EventStoreApi.QueryApi.AggregateLink<AccountManagement.Domain.Account>;

namespace AccountManagement.Domain;

[UsedImplicitly] class EmailToAccountMapper
{
   static DocumentDbApi DocumentDb => new();

   internal static void UpdateMappingWhenEmailChanges(MessageHandlerRegistrarWithDependencyInjectionSupport registrar) => registrar.ForEvent(
      (AccountEvent.PropertyUpdated.Email emailUpdated, IInProcessHypermediaNavigator navigator) =>
      {
         if(emailUpdated.AggregateVersion > 1)
         {
            var previousAccountVersion = navigator.Execute(InternalApi.Queries.GetReadOnlyCopyOfVersion(emailUpdated.AggregateId, emailUpdated.AggregateVersion - 1));
            navigator.Execute(DocumentDb.Commands.Delete<AccountLink>(previousAccountVersion.Email.StringValue));
         }

         var newEmail = emailUpdated.Email;
         navigator.Execute(DocumentDb.Commands.Save(newEmail.StringValue, InternalApi.Queries.GetForUpdate(emailUpdated.AggregateId)));
      });

   internal static void TryGetAccountByEmail(MessageHandlerRegistrarWithDependencyInjectionSupport registrar) => registrar.ForQuery(
      (InternalApi.Query.TryGetByEmailQuery query, IInProcessHypermediaNavigator navigator) =>
         navigator.Execute(DocumentDb.Queries.TryGet<AccountLink>(query.Email.StringValue)) is Some<AccountLink> accountLink
            ? Option.Some(navigator.Execute(accountLink.Value))
            : Option.None<Account>());
}