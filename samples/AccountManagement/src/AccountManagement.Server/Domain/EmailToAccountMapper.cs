using AccountManagement.Domain.Tevents;
using Compze.DocumentDb;
using Compze.Tessaging.Engine;
using Compze.Tessaging.Engine.HandlerRegistration.TessageHandlers;
using JetBrains.Annotations;
using AccountLink = Compze.Teventive.TeventStore.Typermedia.TeventStoreApi.TueryApi.TaggregateLink<AccountManagement.Domain.Account>;
using Compze.Tessaging.Typermedia;

namespace AccountManagement.Domain;

[UsedImplicitly] class EmailToAccountMapper
{
   static DocumentDbApi DocumentDb => new();

   //Account tevents are exactly-once kinds, and exactly-once kinds are async end to end - so the handler is declared async; its work is synchronous today, so it completes its task synchronously.
   internal static void UpdateMappingWhenEmailChanges(TessageHandlerRegistrar registrar) => registrar.ForTevent(
      (IAccountTevent.PropertyUpdated.Email emailUpdated, ILocalTypermediaNavigatorSession navigator) =>
      {
         if(emailUpdated.TaggregateVersion > 1)
         {
            var previousAccountVersion = navigator.Execute(InternalApi.Tueries.GetReadOnlyCopyOfVersion(emailUpdated.TaggregateId, emailUpdated.TaggregateVersion - 1));
            navigator.Execute(DocumentDb.Tommands.Delete<AccountLink>(previousAccountVersion.Email.StringValue));
         }

         var newEmail = emailUpdated.Email;
         navigator.Execute(DocumentDb.Tommands.Save(newEmail.StringValue, InternalApi.Tueries.GetForUpdate(emailUpdated.TaggregateId)));
         return Task.CompletedTask;
      });

   internal static void TryGetAccountByEmail(TessageHandlerRegistrar registrar) => registrar.ForTuery(
      (InternalApi.Tuery.TryGetByEmailTuery tuery, ILocalTypermediaNavigatorSession navigator) =>
         navigator.Execute(DocumentDb.Tueries.TryGet<AccountLink>(tuery.Email.StringValue)) is { } accountLink
            ? navigator.Execute(accountLink)
            : null);
}