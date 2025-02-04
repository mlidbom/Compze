﻿using System;
using AccountManagement.API;
using AccountManagement.Domain;
using AccountManagement.Domain.Events;
using AccountManagement.Domain.Passwords;
using AccountManagement.Domain.Registration;
using Compze.Functional;
using Compze.Messaging;
using Compze.Messaging.Buses;
using Compze.Messaging.Hypermedia;

namespace AccountManagement.UI;

static class AccountUIAdapter
{
   public static void Login(MessageHandlerRegistrarWithDependencyInjectionSupport registrar) => registrar.ForCommandWithResult(
      (AccountResource.Command.LogIn logIn, ILocalHypermediaNavigator navigator) =>
      {
         var email = Email.Parse(logIn.Email);

         if(navigator.Execute(InternalApi.Queries.TryGetByEmail(email)) is Some<Account> account)
         {
            return account.Value.Login(logIn.Password) switch
            {
               AccountEvent.LoggedIn loggedIn => AccountResource.Command.LogIn.LoginAttemptResult.Success(loggedIn.AuthenticationToken),
               AccountEvent.LoginFailed _ => AccountResource.Command.LogIn.LoginAttemptResult.Failure(),
               _ => throw new ArgumentOutOfRangeException()
            };
         } else
         {
            return AccountResource.Command.LogIn.LoginAttemptResult.Failure();
         }
      });

   internal static void ChangePassword(MessageHandlerRegistrarWithDependencyInjectionSupport registrar) => registrar.ForCommand(
      (AccountResource.Command.ChangePassword command, ILocalHypermediaNavigator navigator) =>
         navigator.Execute(InternalApi.Queries.GetForUpdate(command.AccountId)).ChangePassword(command.OldPassword, new Password(command.NewPassword)));

   internal static void ChangeEmail(MessageHandlerRegistrarWithDependencyInjectionSupport registrar) => registrar.ForCommand(
      (AccountResource.Command.ChangeEmail command, ILocalHypermediaNavigator navigator) =>
         navigator.Execute(InternalApi.Queries.GetForUpdate(command.AccountId)).ChangeEmail(Email.Parse(command.Email)));

   internal static void Register(MessageHandlerRegistrarWithDependencyInjectionSupport registrar) => registrar.ForCommandWithResult(
      (AccountResource.Command.Register command, ILocalHypermediaNavigator bus) =>
      {
         var (status, account) = Account.Register(command.AccountId, Email.Parse(command.Email), new Password(command.Password), bus);
         return status switch
         {
            RegistrationAttemptStatus.Successful => new AccountResource.Command.Register.RegistrationAttemptResult(status, new AccountResource(account!)),
            RegistrationAttemptStatus.EmailAlreadyRegistered => new AccountResource.Command.Register.RegistrationAttemptResult(status, null),
            _ => throw new ArgumentOutOfRangeException()
         };
      });

   internal static void GetById(MessageHandlerRegistrarWithDependencyInjectionSupport registrar) => registrar.ForQuery(
      (MessageTypes.Remotable.NonTransactional.Queries.EntityLink<AccountResource> accountQuery, ILocalHypermediaNavigator navigator)
         => new AccountResource(navigator.Execute(InternalApi.AccountQueryModel.Queries.Get(accountQuery.EntityId))));
}