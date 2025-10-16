using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Compze.Abstractions;
using Compze.Sql.DocumentDb.Abstractions;
using Compze.Tessaging.Hosting.Testing;
using Compze.Tessaging.Hosting.Testing.DependencyInjection;
using Compze.Tests.Common.Sql.DocumentDb;
using Compze.Tests.Infrastructure;
using Compze.Tests.Infrastructure.XUnit.PluggableComponents;
using Compze.Utilities.DependencyInjection;
using Compze.Utilities.SystemCE;
using Compze.Utilities.SystemCE.LinqCE;
using Compze.Utilities.Threading;
using Compze.Utilities.Threading.TasksCE;
using FluentAssertions;
using Xunit;
using static FluentAssertions.FluentActions;

// ReSharper disable AccessToDisposedClosure

namespace Compze.Tests.Integration.XUnit.Sql.DocumentDb;

public class DocumentDbTests : DocumentDbTestsBase, IAsyncLifetime
{
   public DocumentDbTests() => ServiceLocator = TestEnv.DIContainer.SetupTestingServiceLocator(_ => {});

   public async Task InitializeAsync() => await Task.CompletedTask;
   public async Task DisposeAsync() => await ServiceLocator.DisposeAsync();

   [PCT]
    public void CanSaveAndLoadDocument()
    {
        var user = new User
        {
            Email = "email@email.se",
            Password = "password",
            Address = new Address
            {
                City = "MyTown",
                Street = "MyStreet",
                StreetNumber = 234
            }
        };

        UseInTransactionalScope((_, updater) => updater.Save(user.Id, user));

        UseInScope(reader =>
        {
            var loadedUser = reader.Get<User>(user.Id);

            loadedUser.Id.Should().Be(user.Id);
            loadedUser.Email.Should().Be(user.Email);
            loadedUser.Password.Should().Be(user.Password);

            loadedUser.Address.Should().Be(user.Address);
        });
    }

    [PCT]
    public void GetAllWithIdsReturnsAsManyResultsAsPassedIds()
    {
        var ids = 1.Through(9).Select(index => Guid.Parse($"00000000-0000-0000-0000-00000000000{index}")).ToArray();

        var users = ids.Select(id => new User(id)).ToArray();

        UseInTransactionalScope((_, updater) => users.ForEach(user => updater.Save(user)));

        UseInScope(reader => reader.GetAll<User>(ids.Take(5))
                                   .Select(fetched => fetched.Id)
                                   .Should()
                                   .Equal(ids.Take(5)));
    }

    [PCT]
    public void GetAllWithIdsThrowsNoSuchDocumentExceptionExceptionIfAnyIdIsMissing()
    {
        var ids = 1.Through(9)
                   .Select(index => Guid.Parse($"00000000-0000-0000-0000-00000000000{index}"))
                   .ToArray();

        var users = ids.Select(id => new User(id))
                       .ToArray();

        UseInTransactionalScope((_, updater) => users.ForEach(user => updater.Save(user)));

        UseInScope(reader => Invoking(
                      () => reader.GetAll<User>(ids.Take(5)
                                                   .Append(Guid.Parse("00000000-0000-0000-0000-000000000099"))
                                                   .ToArray())
                                  // ReSharper disable once ReturnValueOfPureMethodIsNotUsed
                                  .ToArray()).Should().Throw<ArgumentOutOfRangeException>());
    }


    [PCT]
    public void GetAllWithIdsReturnsTheSameInstanceForAnyPreviouslyFetchedDocuments()
    {
        var ids = 1.Through(9).Select(index => Guid.Parse($"00000000-0000-0000-0000-00000000000{index}")).ToArray();

        var users = ids.Select(id => new User(id)).ToArray();

        UseInTransactionalScope((_, updater) => users.ForEach(user => updater.Save(user)));

        UseInScope(reader =>
        {
            var fetchedIndividually = ids.Select(id => reader.Get<User>(id))
                                       .ToArray();
            var fetchedWithGetAll = reader.GetAll<User>(ids)
                                        .ToArray();

            fetchedIndividually.ForEach((user, index) => user.Should().BeSameAs(fetchedWithGetAll[index]));
        });
    }



    [PCT]
    public void CanSaveAndLoadAggregateForUpdate()
    {
        var user = new User
        {
            Email = "email@email.se",
            Password = "password",
            Address = new Address
            {
                City = "MyTown",
                Street = "MyStreet",
                StreetNumber = 234
            }
        };

        UseInTransactionalScope((_, updater) => updater.Save(user.Id, user));

        UseInTransactionalScope((_, updater) =>
        {
            var loadedUser = updater.GetForUpdate<User>(user.Id);

            loadedUser.Id.Should().Be(user.Id);
            loadedUser.Email.Should().Be(user.Email);
            loadedUser.Password.Should().Be(user.Password);

            loadedUser.Address.Should().Be(user.Address);
        });
    }

    [PCT]
    public void CallingSaveWithAnInterfaceAsTypeParameterDoesNotExplode()
    {
        IPersistentEntity user1 = new User { Email = "user1" };
        IPersistentEntity user2 = new User { Email = "user2" };

        UseInTransactionalScope((reader, updater) =>
        {
            updater.Save(user2);
            updater.Save(user1.Id, user1);
            reader.Get<User>(user1.Id)
                 .Should()
                 .Be(user1);
            reader.Get<User>(user2.Id)
                 .Should()
                 .Be(user2);
        });

        UseInScope(reader =>
        {
            reader.Get<User>(user1.Id)
                 .Id.Should()
                 .Be(user1.Id);
            reader.Get<User>(user2.Id)
                 .Id.Should()
                 .Be(user2.Id);
        });
    }

    [PCT]
    public void AddingAndRemovingObjectResultsInNoObjectBeingSaved()
    {
        var user = new User();

        UseInTransactionalScope((_, updater) =>
        {
            updater.Save(user.Id, user);
            updater.Delete(user);
        });

        UseInScope(reader =>
                      reader.TryGet(user.Id, out user)
                            .Should()
                            .BeFalse());
    }

    [PCT]
    public void AddingRemovingAndAddingObjectInTransactionResultsInNoObjectBeingSaved()
    {
        var user = new User();

        UseInTransactionalScope((_, updater) =>
        {
            updater.Save(user.Id, user);
            updater.Delete(user);
            updater.Save(user.Id, user);
        });

        UseInScope(reader => reader.TryGet(user.Id, out user).Should().BeTrue());
    }

    [PCT]
    public void ObjectsWhoseKeysDifferOnlyByCaseAreConsideredTheSameObjectForCompatibilityWithMsSql()
    {
        var lowerCase = new Email("theemail");
        var upperCase = new Email(lowerCase.TheEmail.ToUpperInvariant());

        UseInTransactionalScope((reader, updater) =>
        {
           updater.Save(lowerCase.TheEmail, lowerCase);
           Invoking(() => updater.Save(upperCase.TheEmail, upperCase)).Should().Throw<ArgumentException>();

           reader.Get<Email>(lowerCase.TheEmail)
                 .Should()
                 .Be(reader.Get<Email>(upperCase.TheEmail));
        });

        UseInTransactionalScope((reader, updater) =>
        {

            Invoking(() => updater.Save(upperCase.TheEmail, upperCase)).Should().Throw<ArgumentException>();
            reader.Get<Email>(upperCase.TheEmail)
                .TheEmail.Should()
                .Be(lowerCase.TheEmail);
            reader.Get<Email>(lowerCase.TheEmail)
                .Should()
                .Be(reader.Get<Email>(upperCase.TheEmail));

            updater.Delete<Email>(upperCase.TheEmail);
            Invoking(() => updater.Delete<Email>(upperCase.TheEmail)).Should().Throw<ArgumentOutOfRangeException>();
            Invoking(() => updater.Delete<Email>(lowerCase.TheEmail)).Should().Throw<ArgumentOutOfRangeException>();
        });
    }

    [PCT]
    public void ObjectsWhoseKeysDifferOnlyByTrailingSpacesTrailingWhiteSpaceCaseAreConsideredTheSameObjectForCompatibilityWithMsSql()
    {
        var noWhitespace = new Email("theemail");
        var withWhitespace = new Email(noWhitespace.TheEmail + "  ");

        UseInTransactionalScope((reader, updater) =>
        {
            updater.Save(noWhitespace.TheEmail, noWhitespace);
            Invoking(() => updater.Save(withWhitespace.TheEmail, withWhitespace)).Should().Throw<ArgumentException>();

            reader.Get<Email>(noWhitespace.TheEmail)
                .Should()
                .Be(reader.Get<Email>(withWhitespace.TheEmail));
        });

        UseInTransactionalScope((reader, updater) =>
        {
            Invoking(() => updater.Save(withWhitespace.TheEmail, withWhitespace)).Should().Throw<ArgumentException>();
            reader.Get<Email>(withWhitespace.TheEmail)
                .TheEmail.Should()
                .Be(noWhitespace.TheEmail);
            reader.Get<Email>(noWhitespace.TheEmail)
                .Should()
                .Be(reader.Get<Email>(withWhitespace.TheEmail));

            updater.Delete<Email>(withWhitespace.TheEmail);
            Invoking(() => updater.Delete<Email>(withWhitespace.TheEmail)).Should().Throw<ArgumentOutOfRangeException>();
            Invoking(() => updater.Delete<Email>(noWhitespace.TheEmail)).Should().Throw<ArgumentOutOfRangeException>();
        });
    }

    [PCT]
    public void TryingToFetchNonExistentItemDoesNotCauseSessionToTryAndAddItWithANullInstance()
    {
        var user = new User();

        UseInScope(reader => reader.TryGet(user.Id, out user)
                                   .Should()
                                   .Be(false));
    }

    [PCT]
    public void RepeatedlyAddingAndRemovingObjectResultsInNoObjectBeingSaved()
    {
        var user = new User();

        UseInTransactionalScope((_, updater) =>
        {
            updater.Save(user.Id, user);
            updater.Delete(user);
            updater.Save(user.Id, user);
            updater.Delete(user);
            updater.Save(user.Id, user);
            updater.Delete(user);
        });

        UseInScope(reader => reader.TryGet(user.Id, out user)
                                   .Should()
                                   .BeFalse());
    }

    [PCT]
    public void LoadingRemovingAndAddingObjectInTransactionResultsInObjectBeingSaved()
    {
        var user = new User();

        UseInTransactionalScope((_, updater) => updater.Save(user.Id, user));

        UseInTransactionalScope((reader, updater) =>
        {
            user = reader.Get<User>(user.Id);
            updater.Delete(user);

            reader.TryGet(user.Id, out User? _)
                .Should()
                .Be(false);
            updater.Save(user);
            reader.TryGet(user.Id, out User? _)
                .Should()
                .Be(true);
            updater.Delete(user);
            reader.TryGet(user.Id, out User? _)
                .Should()
                .Be(false);
            updater.Save(user);
            reader.TryGet(user.Id, out User? _)
                .Should()
                .Be(true);
        });

        UseInScope(reader => reader.TryGet(user.Id, out user)
                                   .Should()
                                   .Be(true));
    }


    [PCT]
    public void ReturnsSameInstanceOnRepeatedLoads()
    {
        var user = new User();

        UseInTransactionalScope((_, updater) => updater.Save(user.Id, user));

        UseInScope(reader =>
        {
            var loaded1 = reader.Get<User>(user.Id);
            var loaded2 = reader.Get<User>(user.Id);
            loaded1.Should().BeSameAs(loaded2);
        });
    }

    [PCT]
    public void ReturnsSameInstanceOnLoadAfterSave()
    {
        var user = new User();

        UseInTransactionalScope((reader, updater) =>
        {
            updater.Save(user.Id, user);

            var loaded1 = reader.Get<User>(user.Id);
            var loaded2 = reader.Get<User>(user.Id);
            loaded1.Should().BeSameAs(loaded2);
            loaded1.Should().BeSameAs(user);
        });
    }

    [PCT]
    public void HandlesHashSets()
    {
        var user = new User();
        var userSet = new HashSet<User> { user };

        UseInTransactionalScope((_, updater) => updater.Save(user.Id, userSet));

        UseInScope(reader =>
        {
            var loadedUser = reader.Get<HashSet<User>>(user.Id);
            loadedUser.Count.Should().Be(1);
        });
    }

    [PCT]
    public void HandlesHashSetsInObjects()
    {
        var userInSet = new User
        {
            Email = "Email"
        };

        var user = new User
        {
            People = [userInSet]
        };

        UseInTransactionalScope((_, updater) => updater.Save(user.Id, user));

        UseInScope(reader =>
        {
            var loadedUser = reader.Get<User>(user.Id);
            loadedUser.People.Count.Should().Be(1);
            var loadedUserInSet = loadedUser.People.Single();
            loadedUserInSet.Id.Should().Be(userInSet.Id);
        });
    }


    [PCT]
    public void ThrowsExceptionWhenAttemptingToDeleteNonExistingValue()
    {
        UseInTransactionalScope((_, updater) =>
        {
            var lassie = new Dog { Id = Guid.NewGuid() };
            updater.Save(lassie);
        });

        var buster = new Dog { Id = Guid.NewGuid() };
        UseInTransactionalScope((_, updater) => Invoking(() => updater.Delete(buster)).Should().Throw<ArgumentOutOfRangeException>());
    }

    [PCT]
    public void HandlesDeletesOfInstancesAlreadyLoaded()
    {
        var user = new User();

        UseInTransactionalScope((_, updater) => updater.Save(user));

        UseInTransactionalScope((reader, updater) =>
        {
            var loadedUser = updater.GetForUpdate<User>(user.Id);
            loadedUser.Should()
                    .NotBeNull();
            updater.Delete(user);

            Invoking(() => reader.Get<User>(user.Id)).Should().Throw<ArgumentOutOfRangeException>();
        });

        UseInScope(reader => Invoking(() => reader.Get<User>(user.Id)).Should().Throw<ArgumentOutOfRangeException>());
    }

    [PCT]
    public void HandlesDeletesOfInstancesNotYetLoaded()
    {
        var user = new User();

        UseInTransactionalScope((_, updater) => updater.Save(user));

        UseInTransactionalScope((_, updater) =>
        {
            updater.Delete(user);
            Invoking(() => updater.GetForUpdate<User>(user.Id)).Should().Throw<ArgumentOutOfRangeException>();
        });

        UseInScope(reader => Invoking(() => reader.Get<User>(user.Id)).Should().Throw<ArgumentOutOfRangeException>());
    }

    [PCT]
    public void HandlesAValueBeingAddedAndDeletedDuringTheSameSession()
    {
        var user = new User();

        UseInTransactionalScope((_, updater) =>
        {
            updater.Save(user);
            updater.Delete(user);
            Invoking(() => updater.GetForUpdate<User>(user.Id)).Should().Throw<ArgumentOutOfRangeException>();
        });

        UseInScope(reader => Invoking(() => reader.Get<User>(user.Id)).Should().Throw<ArgumentOutOfRangeException>());
    }

    [PCT]
    public void TracksAndUpdatesLoadedAggregates()
    {
        var user = new User();

        UseInTransactionalScope((_, updater) => updater.Save(user.Id, user));

        UseInTransactionalScope((_, updater) =>
        {
            var loadedUser = updater.GetForUpdate<User>(user.Id);
            loadedUser.Password = "NewPassword";
        });

        UseInScope(reader =>
        {
            var loadedUser = reader.Get<User>(user.Id);
            loadedUser.Password.Should().Be("NewPassword");
        });
    }

    [PCT]
    public void ThrowsWhenAttemptingToSaveExistingAggregate()
    {
        var user = new User();

        UseInTransactionalScope((_, updater) => updater.Save(user.Id, user));

        Invoking(() => UseInTransactionalScope((_, updater) => updater.Save(user.Id, user))).Should().Throw<ArgumentException>();
    }

    [PCT]
    public void HandlesInstancesOfDifferentTypesWithTheSameId()
    {
        var user = new User
        {
            Email = "email"
        };

        var dog = new Dog { Id = user.Id };

        UseInTransactionalScope((_, updater) =>
        {
            updater.Save<IPersistentEntity>(user);
            updater.Save<IPersistentEntity>(dog);
        });

        UseInScope(reader =>
        {
            var loadedDog = reader.Get<Dog>(dog.Id);
            var loadedUser = reader.Get<User>(dog.Id);

            loadedDog.Name.Should().Be(dog.Name);
            loadedUser.Email.Should().Be(user.Email);
            loadedDog.Id.Should().Be(user.Id);
            loadedUser.Id.Should().Be(user.Id);
        });
    }


    [PCT]
    public void FetchesAllinstancesPerType()
    {
        UseInTransactionalScope((_, updater) =>
        {
            updater.Save(new User());
            updater.Save(new User());
            updater.Save(new Dog { Id = Guid.NewGuid() });
            updater.Save(new Dog { Id = Guid.NewGuid() });
        });

        using (ServiceLocator.BeginScope())
        {
            ServiceLocator.DocumentDbBulkReader().GetAll<Dog>().ToList().Should().HaveCount(2);
            ServiceLocator.DocumentDbBulkReader().GetAll<User>().ToList().Should().HaveCount(2);
        }
    }

    [PCT]
    public void ThrowsIfUsedByMultipleThreads()
    {
        IDocumentDbSession? session = null;
        using var wait = new ManualResetEventSlim();
        var task = TaskCE.Run(() =>
        {
            ServiceLocator.ExecuteInIsolatedScope(() => session = ServiceLocator.DocumentDbSession());
            wait.Set();
        });
        wait.Wait();
        task.Wait();
        session = session.NotNull();

        var user = new User();

        Invoking(() => session.Get<User>(Guid.NewGuid())).Should().Throw<MultiThreadedUseException>();
        Invoking(() => session.GetAll<User>()).Should().Throw<MultiThreadedUseException>();
        Invoking(() => session.Save(user, user.Id)).Should().Throw<MultiThreadedUseException>();
        Invoking(() => session.Delete(user)).Should().Throw<MultiThreadedUseException>();
        Invoking(() => session.Dispose()).Should().Throw<MultiThreadedUseException>();
        Invoking(() => session.Save(new User())).Should().Throw<MultiThreadedUseException>();
        Invoking(() => session.TryGet(Guid.NewGuid(), out user)).Should().Throw<MultiThreadedUseException>();
        Invoking(() => session.Delete(user)).Should().Throw<MultiThreadedUseException>();
    }


    [PCT]
    public void GetHandlesSubTyping()
    {
        var user1 = new User();
        var person1 = new Person();

        UseInTransactionalScope((_, updater) =>
        {
            updater.Save(user1);
            updater.Save(person1);
        });

        UseInScope(reader =>
        {
            reader.Get<Person>(user1.Id).Should().Be(user1);
            reader.Get<Person>(person1.Id).Should().Be(person1);
        });
    }

    [PCT]
    public void GetAllHandlesSubTyping()
    {
        var user1 = new User();
        var person1 = new Person();

        UseInTransactionalScope((_, updater) =>
        {
            updater.Save(user1);
            updater.Save(person1);
        });

        using (ServiceLocator.BeginScope())
        {
            var people = ServiceLocator.DocumentDbBulkReader().GetAll<Person>().ToList();

            people.Should().HaveCount(2);
            people.Should().Contain(user1);
            people.Should().Contain(person1);
        }
    }

    [PCT]
    public void ThrowsExceptionIfYouTryToSaveAnIHasPersistentIdentityWithNoId()
    {
        var user1 = new User(Guid.Empty);

        UseInTransactionalScope((_, updater) => updater.Invoking(it => it.Save(user1))
                                                       .Should().Throw<Exception>());
    }

    [PCT]
    public void GetByIdsShouldReturnOnlyMatchingResultEvenWhenMoreResultsAreInTheCache()
    {
        var user1 = new User(Guid.Parse("00000000-0000-0000-0000-000000000001"));
        var user2 = new User(Guid.Parse("00000000-0000-0000-0000-000000000002"));

        UseInTransactionalScope((reader, updater) =>
        {
            updater.Save(user1);
            updater.Save(user2);

            var people = reader.GetAll<User>([user1.Id]);

            people.ToList().Should().HaveCount(1);
            people.Should().Contain(user1);
        });
    }


    [PCT]
    public void GetAllIdsShouldOnlyReturnResultsWithTheGivenType()
    {
        var userid1 = Guid.Parse("00000000-0000-0000-0000-000000000001");
        var userid2 = Guid.Parse("00000000-0000-0000-0000-000000000002");

        var user1 = new User(userid1);
        var user2 = new User(userid2);
        var dog = new Dog { Id = Guid.Parse("00000000-0000-0000-0000-000000000010") };

        UseInTransactionalScope((_, updater) =>
        {
            updater.Save(user1);
            updater.Save(user2);
            updater.Save(dog);
        });

        ServiceLocator.ExecuteInIsolatedScope(() =>
        {
            var ids = ServiceLocator.DocumentDbBulkReader()
                                  .GetAllIds<User>()
                                  .ToHashSet();

            ids.Count.Should()
             .Be(2);
            ids.Should()
             .Contain(userid1);
            ids.Should()
             .Contain(userid2);
        });
    }

    [PCT]
    public void GetAllIdsShouldOnlyReturnResultsWithTheGivenTypeWhenCalledWithinTheInsertingTransaction()
    {
        var userid1 = Guid.Parse("00000000-0000-0000-0000-000000000001");
        var userid2 = Guid.Parse("00000000-0000-0000-0000-000000000002");

        var user1 = new User(userid1);
        var user2 = new User(userid2);
        var dog = new Dog { Id = Guid.Parse("00000000-0000-0000-0000-000000000010") };

        UseInTransactionalScope((_, updater) =>
        {
            updater.Save(user1);
            updater.Save(user2);
            updater.Save(dog);

            var ids = ServiceLocator.DocumentDbBulkReader()
                                  .GetAllIds<User>()
                                  .ToHashSet();

            ids.Count.Should()
             .Be(2);
            ids.Should()
             .Contain(userid1);
            ids.Should()
             .Contain(userid2);
        });
    }


    [PCT]
    public void DeletingAllObjectsOfATypeLeavesNoSuchObjectsInTheDbButLeavesOtherObjectsInPlaceAndReturnsTheNumberOfDeletedObjects()
    {
        using (ServiceLocator.BeginScope())
        {
            var store = CreateStore();

            var dictionary = new Dictionary<Type, Dictionary<string, string>>();

            1.Through(4).ForEach(_ =>
            {
                var user = new User();
                store.Add(user.Id, user, dictionary);
            });

            1.Through(4).ForEach(_ =>
            {
                var person = new Person();
                store.Add(person.Id, person, dictionary);
            });
        }

        using (ServiceLocator.BeginScope())
        {
            var store = CreateStore();
            store.GetAll<User>().Should().HaveCount(4);
            store.GetAll<Person>().Should().HaveCount(8); //User inherits person

            store.GetAllIds<User>().ForEach(userId => store.Remove(userId, typeof(User)));

            store.GetAll<User>().Should().HaveCount(0);

            store.GetAll<Person>().Should().HaveCount(4);
        }

    }

    async Task InsertUsersInOtherDocumentDb(Guid userId)
    {
        var cloneServiceLocator = ServiceLocator.Clone();
        await using var serviceLocator = cloneServiceLocator;
        cloneServiceLocator.ExecuteTransactionInIsolatedScope(() => cloneServiceLocator.DocumentDbUpdater()
                                                                                       .Save(new User(userId)));
    }

    [PCT]
    public async Task Can_get_document_of_previously_unknown_class_added_by_onother_documentDb_instance()
    {
        var userId = Guid.Parse("00000000-0000-0000-0000-000000000001");

        await InsertUsersInOtherDocumentDb(userId);

        using (ServiceLocator.BeginScope())
        {
            ServiceLocator.DocumentDbSession().Get<User>(userId);
        }
    }

    [PCT]
    public async Task Can_get_all_documents_of_previously_unknown_class_added_by_onother_documentDb_instance()
    {
        var userId = Guid.Parse("00000000-0000-0000-0000-000000000001");

        await InsertUsersInOtherDocumentDb(userId);

        using (ServiceLocator.BeginScope())
        {
            ServiceLocator.DocumentDbSession().GetAll<User>().Count().Should().Be(1);
        }
    }

    [PCT]
    public async Task Can_get_all_documents_of_previously_unknown_class_added_by_onother_documentDb_instance_byId()
    {
        var userId = Guid.Parse("00000000-0000-0000-0000-000000000001");

        await InsertUsersInOtherDocumentDb(userId);

        UseInScope(reader => reader.GetAll<User>([userId])
                                   .Count()
                                   .Should()
                                   .Be(1));
    }
}