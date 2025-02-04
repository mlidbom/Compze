﻿using System;
using System.Threading.Tasks;
using Compze.DependencyInjection;
using Compze.GenericAbstractions.Time;
using Compze.Messaging.Events;
using Compze.Persistence.Common.DependencyInjection;
using Compze.Persistence.DocumentDb;
using Compze.Persistence.EventStore;
using Compze.Persistence.EventStore.Aggregates;
using Compze.Persistence.EventStore.Query.Models.Generators;
using Compze.Refactoring.Naming;
using Compze.Testing;
using Compze.Testing.DependencyInjection;
using Compze.Testing.SystemCE.TransactionsCE;
using Compze.Tests.Integration.CQRS.Query.Models.AutoGenerated.Domain;
using Compze.Tests.Integration.CQRS.Query.Models.AutoGenerated.Domain.UI.QueryModels;
using FluentAssertions;
using JetBrains.Annotations;
using NUnit.Framework;

// ReSharper disable ImplicitlyCapturedClosure

namespace Compze.Tests.Integration.CQRS.Query.Models.AutoGenerated
{
   public class QueryModelGeneratingDocumentDbReaderTests(string pluggableComponentsCombination) : DuplicateByPluggableComponentTest(pluggableComponentsCombination)
   {
      IServiceLocator _serviceLocator;

      [SetUp] public void CreateContainer()
      {
         _serviceLocator = TestingContainerFactory.CreateServiceLocatorForTesting(
            endpointBuilder =>
            {
               endpointBuilder.Container.RegisterEventStore("nonsense");
               endpointBuilder.Container.Register(
                  Scoped.For<IDocumentDbReader, IVersioningDocumentDbReader>().CreatedBy((AccountQueryModelGenerator queryModelGenerator) => new QueryModelGeneratingDocumentDbReader([queryModelGenerator])),
                  Scoped.For<AccountQueryModelGenerator>().CreatedBy((IEventStoreReader eventStoreReader) => new AccountQueryModelGenerator(eventStoreReader))
               );
            });

         _serviceLocator.Resolve<ITypeMappingRegistar>()
                        .Map<MyAccount>("f17eab2f-d0ae-4363-b1ff-85bc6a1b9b5d")
                        .Map<AccountEvents.AccountEvent>("de6d74a5-c039-4f85-b51b-081c1b83e3f3")
                        .Map<AccountEvents.IAccountDeletedEvent>("03598100-598e-4d22-80a4-075f144f9f6c")
                        .Map<AccountEvents.IAccountEvent>("6dd0fad7-91cb-4dd5-a654-b3595d9151c8")
                        .Map<AccountEvents.IAccountRegisteredEvent>("463593ad-61c8-4aea-8a68-9e30899a38fb")
                        .Map<AccountEvents.IEmailChangedEvent>("b36def96-72a6-43eb-b4ce-730e9b16e758")
                        .Map<AccountEvents.Implementation.AccountDeletedEvent>("eb8df880-3fa0-49df-81c3-73b43ad8aeb8")
                        .Map<AccountEvents.Implementation.AccountRegisteredEvent>("9a1ca37c-e6b6-4eea-b553-7125aade02b2")
                        .Map<AccountEvents.Implementation.EmailChangedEvent>("a5b1c528-a41c-4401-956a-6b08092b64cd")
                        .Map<AccountEvents.PropertyUpdated.IEmail>("e6ec9ac8-f317-4883-b450-ee4e4e9f6978")
                        .Map<AccountEvents.PropertyUpdated.IPassword>("0595f981-9adc-472a-b394-e1da44f4e8a9");
      }

      [TearDown] public async Task TearDownTask() => await _serviceLocator.DisposeAsync();

      [Test] public void ThrowsExceptionIfInstanceDoesNotExist()
      {
         using(_serviceLocator.BeginScope())
         {
            var reader = _serviceLocator.Resolve<IDocumentDbReader>();
            reader.Invoking(me => me.Get<MyAccountQueryModel>(Guid.NewGuid()))
                  .Should().Throw<Exception>();
         }
      }

      [Test] public void CanFetchQueryModelAfterAggregateHasBeenCreated()
      {
         using(_serviceLocator.BeginScope())
         {
            var aggregates = _serviceLocator.Resolve<IEventStoreUpdater>();
            var accountId = Guid.Parse("00000000-0000-0000-0000-000000000001");

            var registered = TransactionScopeCe.Execute(() => MyAccount.Register(aggregates, accountId, "email", "password"));

            registered.Email.Should().Be("email");
            registered.Password.Should().Be("password");

            var reader = _serviceLocator.Resolve<IDocumentDbReader>();
            var loadedModel = reader.Get<MyAccountQueryModel>(registered.Id);

            loadedModel.Should().NotBe(null);
            loadedModel.Id.Should().Be(accountId);
            loadedModel.Email.Should().Be(registered.Email);
            loadedModel.Password.Should().Be(registered.Password);
         }
      }

      [Test] public void ThrowsExceptionWhenTryingToFetchDeletedEntity()
      {
         MyAccount registered;
         using(_serviceLocator.BeginScope())
         {
            var aggregates = _serviceLocator.Resolve<IEventStoreUpdater>();
            var accountId = Guid.Parse("00000000-0000-0000-0000-000000000001");

            registered = TransactionScopeCe.Execute(() => MyAccount.Register(aggregates, accountId, "email", "password"));

            var reader = _serviceLocator.Resolve<IDocumentDbReader>();
            reader.Get<MyAccountQueryModel>(registered.Id); //Here it exists
         }

         using(_serviceLocator.BeginScope())
         {
            TransactionScopeCe.Execute(() => _serviceLocator.Resolve<IEventStoreUpdater>().Get<MyAccount>(registered.Id).Delete());
         }

         using(_serviceLocator.BeginScope())
         {
            var reader2 = _serviceLocator.Resolve<IDocumentDbReader>();
            reader2.Invoking(me => me.Get<MyAccountQueryModel>(registered.Id)).Should().Throw<Exception>();
         }
      }

      [Test] public void ReturnsUpdatedDataAfterTransactionHasCommitted()
      {
         MyAccount registered;
         using(_serviceLocator.BeginScope())
         {
            var accountId = Guid.Parse("00000000-0000-0000-0000-000000000001");

            var aggregates = _serviceLocator.Resolve<IEventStoreUpdater>();
            registered = TransactionScopeCe.Execute(() => MyAccount.Register(aggregates, accountId, "email", "password"));

            _serviceLocator.Resolve<IDocumentDbReader>()
                           .Get<MyAccountQueryModel>(registered.Id); //Make sure we read it once so caches etc get involved.
         }

         using(_serviceLocator.BeginScope())
         {
            TransactionScopeCe.Execute(() => _serviceLocator.Resolve<IEventStoreUpdater>().Get<MyAccount>(registered.Id).ChangeEmail("newEmail"));
         }

         using(_serviceLocator.BeginScope())
         {
            var loadedModel = _serviceLocator.Resolve<IDocumentDbReader>()
                                             .Get<MyAccountQueryModel>(registered.Id);

            loadedModel.Should().NotBe(null);
            loadedModel.Email.Should().Be("newEmail");
         }
      }

      [Test] public void CanReturnPreviousVersionsOfQueryModel()
      {
         MyAccount registered;

         using(_serviceLocator.BeginScope())
         {
            var accountId = Guid.Parse("00000000-0000-0000-0000-000000000001");

            var aggregates = _serviceLocator.Resolve<IEventStoreUpdater>();
            registered = TransactionScopeCe.Execute(() => MyAccount.Register(aggregates, accountId, "originalEmail", "password"));

            _serviceLocator.Resolve<IVersioningDocumentDbReader>()
                           .GetVersion<MyAccountQueryModel>(registered.Id, registered.Version); //Make sure we read it once so caches etc get involved.
         }

         using(_serviceLocator.BeginScope())
         {
            TransactionScopeCe.Execute(() =>
            {
               registered = _serviceLocator.Resolve<IEventStoreUpdater>().Get<MyAccount>(registered.Id);
               registered.ChangeEmail("newEmail1");
               registered.ChangeEmail("newEmail2");
               registered.ChangeEmail("newEmail3");
            });
         }

         using(_serviceLocator.BeginScope())
         {
            var loadedModel = _serviceLocator.Resolve<IVersioningDocumentDbReader>()
                                             .Get<MyAccountQueryModel>(registered.Id);

            loadedModel.Should().NotBe(null);
            loadedModel.Email.Should().Be("newEmail3");

            _serviceLocator.Resolve<IVersioningDocumentDbReader>()
                           .GetVersion<MyAccountQueryModel>(registered.Id, registered.Version - 1)
                           .Email.Should().Be("newEmail2");

            _serviceLocator.Resolve<IVersioningDocumentDbReader>()
                           .GetVersion<MyAccountQueryModel>(registered.Id, registered.Version - 2)
                           .Email.Should().Be("newEmail1");

            _serviceLocator.Resolve<IVersioningDocumentDbReader>()
                           .GetVersion<MyAccountQueryModel>(registered.Id, registered.Version - 3)
                           .Email.Should().Be("originalEmail");
         }
      }
   }

   namespace Domain
   {
      namespace UI
      {
         namespace QueryModels
         {
            [UsedImplicitly] public class MyAccountQueryModel : ISingleAggregateQueryModel
            {
               public Guid Id { get; private set; }
               internal string Email { get; set; } = "";
               internal string Password { get; set; } = "";

               public void SetId(Guid id) => Id = id;
            }

            public class AccountQueryModelGenerator : SingleAggregateQueryModelGenerator<AccountQueryModelGenerator, MyAccountQueryModel, AccountEvents.IAccountEvent, IEventStoreReader>
            {
               public AccountQueryModelGenerator(IEventStoreReader session) : base(session)
               {
                  RegisterHandlers()
                    .For<AccountEvents.PropertyUpdated.IEmail>(e => Model!.Email = e.Email)
                    .For<AccountEvents.PropertyUpdated.IPassword>(e => Model!.Password = e.Password);
               }
            }
         }
      }

      class MyAccount : Aggregate<MyAccount, AccountEvents.AccountEvent, AccountEvents.IAccountEvent>
      {
         MyAccount() : base(new DateTimeNowTimeSource())
         {
            RegisterEventAppliers()
              .For<AccountEvents.PropertyUpdated.IEmail>(e => Email = e.Email)
              .For<AccountEvents.PropertyUpdated.IPassword>(e => Password = e.Password)
              .For<AccountEvents.IAccountDeletedEvent>(_ => {});
         }

         public string Email { get; private set; } = "";
         public string Password { get; private set; } = "";

         public void ChangeEmail(string newEmail) => Publish(new AccountEvents.Implementation.EmailChangedEvent(newEmail));

         public static MyAccount Register(IEventStoreUpdater aggregates, Guid accountId, string email, string password)
         {
            var registered = new MyAccount();
            registered.Publish(new AccountEvents.Implementation.AccountRegisteredEvent(accountId, email, password));
            aggregates.Save(registered);
            return registered;
         }

         public void Delete() => Publish(new AccountEvents.Implementation.AccountDeletedEvent());
      }

      public static class AccountEvents
      {
         public interface IAccountEvent : IAggregateEvent;

         internal abstract class AccountEvent : AggregateEvent, IAccountEvent
         {
            protected AccountEvent() {}
            protected AccountEvent(Guid aggregateId) : base(aggregateId) {}
         }

         internal interface IAccountRegisteredEvent : IAggregateCreatedEvent,
                                                      PropertyUpdated.IEmail,
                                                      PropertyUpdated.IPassword;

         internal interface IEmailChangedEvent : PropertyUpdated.IEmail;

         internal interface IAccountDeletedEvent : IAccountEvent,
                                                   IAggregateDeletedEvent;

         internal static class PropertyUpdated
         {
            public interface IEmail : IAccountEvent
            {
               string Email { get; }
            }

            public interface IPassword : IAccountEvent
            {
               string Password { get; }
            }
         }

         internal static class Implementation
         {
            public class AccountRegisteredEvent(Guid accountId, string email, string password) : AccountEvent(accountId), IAccountRegisteredEvent
            {
               public string Email { get; private set; } = email;
               public string Password { get; private set; } = password;
            }

            public class EmailChangedEvent(string newEmail) : AccountEvent, IEmailChangedEvent
            {
               public string Email { get; private set; } = newEmail;
            }

            public class AccountDeletedEvent : AccountEvent, IAccountDeletedEvent;
         }
      }
   }
}
