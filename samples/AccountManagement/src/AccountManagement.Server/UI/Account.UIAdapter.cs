using AccountManagement.API;
using AccountManagement.Domain;
using AccountManagement.Domain.Tevents;
using AccountManagement.Domain.Passwords;
using AccountManagement.Domain.Registration;
using Compze.Abstractions.Tessaging.Public;
using Compze.Typermedia;
using Compze.Typermedia.HandlerRegistration;

namespace AccountManagement.UI;

static class AccountUIAdapter
{
   public static void Login(TypermediaHandlerRegistrarWithDependencyInjectionSupport registrar) => registrar.ForTommandWithResult(
      (AccountResource.Tommand.LogIn logIn, ILocalTypermediaNavigatorSession navigator) =>
      {
         var email = Email.Parse(logIn.Email);

         if(navigator.Execute(InternalApi.Tueries.TryGetByEmail(email)) is { } account)
         {
            return account.Login(logIn.Password) switch
            {
               IAccountTevent.LoggedIn loggedIn => AccountResource.Tommand.LogIn.LoginAttemptResult.Success(loggedIn.AuthenticationToken),
               IAccountTevent.LoginFailed       => AccountResource.Tommand.LogIn.LoginAttemptResult.Failure(),
               _                                => throw new ArgumentOutOfRangeException()
            };
         } else
         {
            return AccountResource.Tommand.LogIn.LoginAttemptResult.Failure();
         }
      });

   internal static void ChangePassword(TypermediaHandlerRegistrarWithDependencyInjectionSupport registrar) => registrar.ForTommand(
      (AccountResource.Tommand.ChangePassword tommand, ILocalTypermediaNavigatorSession navigator) =>
         navigator.Execute(InternalApi.Tueries.GetForUpdate(tommand.AccountId)).ChangePassword(tommand.OldPassword, new Password(tommand.NewPassword)));

   internal static void ChangeEmail(TypermediaHandlerRegistrarWithDependencyInjectionSupport registrar) => registrar.ForTommand(
      (AccountResource.Tommand.ChangeEmail tommand, ILocalTypermediaNavigatorSession navigator) =>
         navigator.Execute(InternalApi.Tueries.GetForUpdate(tommand.AccountId)).ChangeEmail(Email.Parse(tommand.Email)));

   internal static void Register(TypermediaHandlerRegistrarWithDependencyInjectionSupport registrar) => registrar.ForTommandWithResult(
      (AccountResource.Tommand.Register tommand, ILocalTypermediaNavigatorSession bus) =>
      {
         var (status, account) = Account.Register(tommand.AccountId, Email.Parse(tommand.Email), new Password(tommand.Password), bus);
         return status switch
         {
            RegistrationAttemptStatus.Successful => new AccountResource.Tommand.Register.RegistrationAttemptResult(status, new AccountResource(account!)),
            RegistrationAttemptStatus.EmailAlreadyRegistered => new AccountResource.Tommand.Register.RegistrationAttemptResult(status, null),
            _ => throw new ArgumentOutOfRangeException()
         };
      });

   internal static void GetById(TypermediaHandlerRegistrarWithDependencyInjectionSupport registrar) => registrar.ForTuery(
      (TessageTypes.Remotable.NonTransactional.Tueries.TaggregateLink<AccountResource> accountTuery, ILocalTypermediaNavigatorSession navigator)
         => new AccountResource(navigator.Execute(InternalApi.AccountQueryModel.Tueries.Get(accountTuery.TaggregateId))));
}