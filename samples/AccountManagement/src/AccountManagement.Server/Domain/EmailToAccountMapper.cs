using AccountManagement.Domain.Tevents;
using Compze.DocumentDb;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Compze.Tessaging.TessageBus;
using Compze.Tessaging.Typermedia;
using JetBrains.Annotations;
using AccountLink = Compze.Teventive.TeventStore.Typermedia.TeventStoreApi.TueryApi.TaggregateLink<AccountManagement.Domain.Account>;

namespace AccountManagement.Domain;

[UsedImplicitly] class EmailToAccountMapper
{
   static DocumentDbApi DocumentDb => new();

   //Account tevents are exactly-once kinds, and exactly-once kinds are async end to end - so the handler is declared async, and navigates asynchronously within its unit of work.
   internal static void UpdateMappingWhenEmailChanges(IExactlyOnceTeventHandlerRegistrar registrar) => registrar.ForTevent(
      async (IAccountTevent.PropertyUpdated.Email emailUpdated, ILocalTypermediaNavigatorSession navigator) =>
      {
         if(emailUpdated.TaggregateVersion > 1)
         {
            var previousAccountVersion = await navigator.ExecuteAsync(InternalApi.Tueries.GetReadOnlyCopyOfVersion(emailUpdated.TaggregateId, emailUpdated.TaggregateVersion - 1)).caf();
            await navigator.ExecuteAsync(DocumentDb.Tommands.Delete<AccountLink>(previousAccountVersion.Email.StringValue)).caf();
         }

         var newEmail = emailUpdated.Email;
         await navigator.ExecuteAsync(DocumentDb.Tommands.Save(newEmail.StringValue, InternalApi.Tueries.GetForUpdate(emailUpdated.TaggregateId))).caf();
      });

   internal static void TryGetAccountByEmail(ITueryHandlerRegistrar registrar) => registrar.ForTuery(
      (InternalApi.Tuery.TryGetByEmailTuery tuery, ILocalTypermediaNavigatorSession navigator) =>
         navigator.Execute(DocumentDb.Tueries.TryGet<AccountLink>(tuery.Email.StringValue)) is { } accountLink
            ? navigator.Execute(accountLink)
            : null);
}