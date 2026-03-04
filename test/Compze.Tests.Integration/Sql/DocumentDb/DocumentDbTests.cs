using Compze.Contracts;
using Compze.Core.DocumentDb.Public;
using Compze.Core.Public;
using Compze.SystemCE.ThreadingCE.TasksCE;
using Compze.Tessaging.Hosting.Testing.Wiring;
using Compze.Tests.Common.Sql.DocumentDb;
using Compze.Tests.Infrastructure;
using Compze.Utilities.DependencyInjection;
using Compze.Tests.Infrastructure.XUnit;
using Compze.Utilities.SystemCE.UsageGuards;
using Compze.Must;
using static Compze.Must.MustActions;

// ReSharper disable AccessToDisposedClosure

namespace Compze.Tests.Integration.Sql.DocumentDb;

public class DocumentDbTests : DocumentDbTestsBase
{
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

            loadedUser.Id.Must().Be(user.Id);
            loadedUser.Email.Must().Be(user.Email);
            loadedUser.Password.Must().Be(user.Password);

            loadedUser.Address.Must().Be(user.Address);
        });
    }

    [PCT]
    public void GetAllWithIdsReturnsAsManyResultsAsPassedIds()
    {
        var ids = 1.Through(9).Select(index => new EntityId(Guid.Parse($"00000000-0000-0000-0000-00000000000{index}"))).ToArray();

        var users = ids.Select(id => new User(id.Value)).ToArray();

        UseInTransactionalScope((_, updater) => users.ForEach(updater.Save));

        UseInScope(reader => reader.GetAll<User>(ids.Take(5))
                                   .Select(fetched => fetched.Id)
                                   .Must()
                                   .SequenceEqual(ids.Take(5)));
    }

    [PCT]
    public void GetAllWithIdsThrowsNoSuchDocumentExceptionExceptionIfAnyIdIsMissing()
    {
        var ids = 1.Through(9)
                   .Select(index => new EntityId(Guid.Parse($"00000000-0000-0000-0000-00000000000{index}")))
                   .ToArray();

        var users = ids.Select(id => new User(id.Value))
                       .ToArray();

        UseInTransactionalScope((_, updater) => users.ForEach(updater.Save));

        UseInScope(reader => Invoking(
                      () => reader.GetAll<User>(ids.Take(5)
                                                   .Append(new EntityId(Guid.Parse("00000000-0000-0000-0000-000000000099")))
                                                   .ToArray())
                                  // ReSharper disable once ReturnValueOfPureMethodIsNotUsed
                                  .ToArray()).Must().Throw<ArgumentOutOfRangeException>());
    }


    [PCT]
    public void GetAllWithIdsReturnsTheSameInstanceForAnyPreviouslyFetchedDocuments()
    {
        var ids = 1.Through(9).Select(index => new EntityId(Guid.Parse($"00000000-0000-0000-0000-00000000000{index}"))).ToArray();

        var users = ids.Select(id => new User(id.Value)).ToArray();

        UseInTransactionalScope((_, updater) => users.ForEach(updater.Save));

        UseInScope(reader =>
        {
            var fetchedIndividually = ids.Select(reader.Get<User>)
                                       .ToArray();
            var fetchedWithGetAll = reader.GetAll<User>(ids)
                                        .ToArray();

            fetchedIndividually.ForEach((user, index) => user.Must().ReferenceEqual(fetchedWithGetAll[index]));
        });
    }



    [PCT]
    public void CanSaveAndLoadTaggregateForUpdate()
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

            loadedUser.Id.Must().Be(user.Id);
            loadedUser.Email.Must().Be(user.Email);
            loadedUser.Password.Must().Be(user.Password);

            loadedUser.Address.Must().Be(user.Address);
        });
    }

    [PCT]
    public void CallingSaveWithAnInterfaceAsTypeParameterDoesNotExplode()
    {
        IEntity user1 = new User { Email = "user1" };
        IEntity user2 = new User { Email = "user2" };

        UseInTransactionalScope((reader, updater) =>
        {
            updater.Save(user2);
            updater.Save(user1.Id, user1);
            reader.Get<User>(user1.Id)
                 .Must()
                 .Be(user1);
            reader.Get<User>(user2.Id)
                 .Must()
                 .Be(user2);
        });

        UseInScope(reader =>
        {
            reader.Get<User>(user1.Id)
                 .Id.Must()
                 .Be(user1.Id);
            reader.Get<User>(user2.Id)
                 .Id.Must()
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
                            .Must()
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

        UseInScope(reader => reader.TryGet(user.Id, out user).Must().BeTrue());
    }

    [PCT]
    public void ObjectsWhoseKeysDifferOnlyByCaseAreConsideredTheSameObjectForCompatibilityWithMsSql()
    {
        var lowerCase = new Email("theemail");
        var upperCase = new Email(lowerCase.TheEmail.ToUpperInvariant());

        UseInTransactionalScope((reader, updater) =>
        {
           updater.Save(lowerCase.TheEmail, lowerCase);
           Invoking(() => updater.Save(upperCase.TheEmail, upperCase)).Must().Throw<ArgumentException>();

           reader.Get<Email>(lowerCase.TheEmail)
                 .Must()
                 .Be(reader.Get<Email>(upperCase.TheEmail));
        });

        UseInTransactionalScope((reader, updater) =>
        {

            Invoking(() => updater.Save(upperCase.TheEmail, upperCase)).Must().Throw<ArgumentException>();
            reader.Get<Email>(upperCase.TheEmail)
                .TheEmail.Must()
                .Be(lowerCase.TheEmail);
            reader.Get<Email>(lowerCase.TheEmail)
                .Must()
                .Be(reader.Get<Email>(upperCase.TheEmail));

            updater.Delete<Email>(upperCase.TheEmail);
            Invoking(() => updater.Delete<Email>(upperCase.TheEmail)).Must().Throw<ArgumentOutOfRangeException>();
            Invoking(() => updater.Delete<Email>(lowerCase.TheEmail)).Must().Throw<ArgumentOutOfRangeException>();
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
            Invoking(() => updater.Save(withWhitespace.TheEmail, withWhitespace)).Must().Throw<ArgumentException>();

            reader.Get<Email>(noWhitespace.TheEmail)
                .Must()
                .Be(reader.Get<Email>(withWhitespace.TheEmail));
        });

        UseInTransactionalScope((reader, updater) =>
        {
            Invoking(() => updater.Save(withWhitespace.TheEmail, withWhitespace)).Must().Throw<ArgumentException>();
            reader.Get<Email>(withWhitespace.TheEmail)
                .TheEmail.Must()
                .Be(noWhitespace.TheEmail);
            reader.Get<Email>(noWhitespace.TheEmail)
                .Must()
                .Be(reader.Get<Email>(withWhitespace.TheEmail));

            updater.Delete<Email>(withWhitespace.TheEmail);
            Invoking(() => updater.Delete<Email>(withWhitespace.TheEmail)).Must().Throw<ArgumentOutOfRangeException>();
            Invoking(() => updater.Delete<Email>(noWhitespace.TheEmail)).Must().Throw<ArgumentOutOfRangeException>();
        });
    }

    [PCT]
    public void TryingToFetchNonExistentItemDoesNotCauseSessionToTryAndAddItWithANullInstance()
    {
        var user = new User();

        UseInScope(reader => reader.TryGet(user.Id, out user)
                                   .Must()
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
                                   .Must()
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
                .Must()
                .Be(false);
            updater.Save(user);
            reader.TryGet(user.Id, out User? _)
                .Must()
                .Be(true);
            updater.Delete(user);
            reader.TryGet(user.Id, out User? _)
                .Must()
                .Be(false);
            updater.Save(user);
            reader.TryGet(user.Id, out User? _)
                .Must()
                .Be(true);
        });

        UseInScope(reader => reader.TryGet(user.Id, out user)
                                   .Must()
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
            loaded1.Must().ReferenceEqual(loaded2);
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
            loaded1.Must().ReferenceEqual(loaded2);
            loaded1.Must().ReferenceEqual(user);
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
            loadedUser.Count.Must().Be(1);
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
            loadedUser.People.Count.Must().Be(1);
            var loadedUserInSet = loadedUser.People.Single();
            loadedUserInSet.Id.Must().Be(userInSet.Id);
        });
    }


    [PCT]
    public void ThrowsExceptionWhenAttemptingToDeleteNonExistingValue()
    {
        UseInTransactionalScope((_, updater) =>
        {
            var lassie = new Dog { Id = new EntityId() };
            updater.Save(lassie);
        });

        var buster = new Dog { Id = new EntityId() };
        UseInTransactionalScope((_, updater) => Invoking(() => updater.Delete(buster)).Must().Throw<ArgumentOutOfRangeException>());
    }

    [PCT]
    public void HandlesDeletesOfInstancesAlreadyLoaded()
    {
        var user = new User();

        UseInTransactionalScope((_, updater) => updater.Save(user));

        UseInTransactionalScope((reader, updater) =>
        {
            var loadedUser = updater.GetForUpdate<User>(user.Id);
            loadedUser.Must()
                    .NotBeNull();
            updater.Delete(user);

            Invoking(() => reader.Get<User>(user.Id)).Must().Throw<ArgumentOutOfRangeException>();
        });

        UseInScope(reader => Invoking(() => reader.Get<User>(user.Id)).Must().Throw<ArgumentOutOfRangeException>());
    }

    [PCT]
    public void HandlesDeletesOfInstancesNotYetLoaded()
    {
        var user = new User();

        UseInTransactionalScope((_, updater) => updater.Save(user));

        UseInTransactionalScope((_, updater) =>
        {
            updater.Delete(user);
            Invoking(() => updater.GetForUpdate<User>(user.Id)).Must().Throw<ArgumentOutOfRangeException>();
        });

        UseInScope(reader => Invoking(() => reader.Get<User>(user.Id)).Must().Throw<ArgumentOutOfRangeException>());
    }

    [PCT]
    public void HandlesAValueBeingAddedAndDeletedDuringTheSameSession()
    {
        var user = new User();

        UseInTransactionalScope((_, updater) =>
        {
            updater.Save(user);
            updater.Delete(user);
            Invoking(() => updater.GetForUpdate<User>(user.Id)).Must().Throw<ArgumentOutOfRangeException>();
        });

        UseInScope(reader => Invoking(() => reader.Get<User>(user.Id)).Must().Throw<ArgumentOutOfRangeException>());
    }

    [PCT]
    public void TracksAndUpdatesLoadedTaggregates()
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
            loadedUser.Password.Must().Be("NewPassword");
        });
    }

    [PCT]
    public void ThrowsWhenAttemptingToSaveExistingTaggregate()
    {
        var user = new User();

        UseInTransactionalScope((_, updater) => updater.Save(user.Id, user));

        Invoking(() => UseInTransactionalScope((_, updater) => updater.Save(user.Id, user))).Must().Throw<ArgumentException>();
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
            updater.Save<IEntity>(user);
            updater.Save<IEntity>(dog);
        });

        UseInScope(reader =>
        {
            var loadedDog = reader.Get<Dog>(dog.Id);
            var loadedUser = reader.Get<User>(dog.Id);

            loadedDog.Name.Must().Be(dog.Name);
            loadedUser.Email.Must().Be(user.Email);
            loadedDog.Id.Must().Be(user.Id);
            loadedUser.Id.Must().Be(user.Id);
        });
    }


    [PCT]
    public void FetchesAllinstancesPerType()
    {
        UseInTransactionalScope((_, updater) =>
        {
            updater.Save(new User());
            updater.Save(new User());
            updater.Save(new Dog { Id = new EntityId() });
            updater.Save(new Dog { Id = new EntityId() });
        });

        using (ServiceLocator.BeginScope())
        {
            ServiceLocator.DocumentDbBulkReader().GetAll<Dog>().ToList().Must().HaveCount(2);
            ServiceLocator.DocumentDbBulkReader().GetAll<User>().ToList().Must().HaveCount(2);
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
        session = session._assert().NotNull();

        var user = new User();

        Invoking(() => session.Get<User>(Guid.NewGuid())).Must().Throw<MultiThreadedUseException>();
        Invoking(() => session.GetAll<User>()).Must().Throw<MultiThreadedUseException>();
        Invoking(() => session.Save(user, user.Id)).Must().Throw<MultiThreadedUseException>();
        Invoking(() => session.Delete(user)).Must().Throw<MultiThreadedUseException>();
        Invoking(() => session.Dispose()).Must().Throw<MultiThreadedUseException>();
        Invoking(() => session.Save(new User())).Must().Throw<MultiThreadedUseException>();
        Invoking(() => session.TryGet(Guid.NewGuid(), out user)).Must().Throw<MultiThreadedUseException>();
        Invoking(() => session.Delete(user)).Must().Throw<MultiThreadedUseException>();
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
            reader.Get<Person>(user1.Id).Must().DeepEqual(user1);
            reader.Get<Person>(person1.Id).Must().DeepEqual(person1);
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
            var people = ServiceLocator.DocumentDbBulkReader().GetAll<Person>().Select(it => it.Id).ToHashSet();

            people.Must().HaveCount(2);
            people.Must().Contain(user1.Id);
            people.Must().Contain(person1.Id);
        }
    }

    [PCT]
    public void ThrowsExceptionIfYouTryToCreateAnIHasPersistentIdentityWithNoId() =>
       Invoking(() => new User(Guid.Empty)).Must().Throw<Exception>();

    [PCT]
    public void GetByIdsShouldReturnOnlyMatchingResultEvenWhenMoreResultsAreInTheCache()
    {
        var user1 = new User(Guid.Parse("00000000-0000-0000-0000-000000000001"));
        var user2 = new User(Guid.Parse("00000000-0000-0000-0000-000000000002"));

        UseInTransactionalScope((reader, updater) =>
        {
            updater.Save(user1);
            updater.Save(user2);

            var people = reader.GetAll<User>([user1.Id]).ToList();

            people.Must().HaveCount(1)
                  .Contain(user1);
        });
    }


    [PCT]
    public void GetAllIdsShouldOnlyReturnResultsWithTheGivenType()
    {
        var userid1 = Guid.Parse("00000000-0000-0000-0000-000000000001");
        var userid2 = Guid.Parse("00000000-0000-0000-0000-000000000002");

        var user1 = new User(userid1);
        var user2 = new User(userid2);
        var dog = new Dog { Id = new EntityId(Guid.Parse("00000000-0000-0000-0000-000000000010")) };

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


            ids.Must()
               .HaveCount(2)
             .Contain(userid1)
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
        var dog = new Dog { Id = new EntityId(Guid.Parse("00000000-0000-0000-0000-000000000010")) };

        UseInTransactionalScope((_, updater) =>
        {
            updater.Save(user1);
            updater.Save(user2);
            updater.Save(dog);

            var ids = ServiceLocator.DocumentDbBulkReader()
                                  .GetAllIds<User>()
                                  .ToHashSet();

            ids.Count.Must()
             .Be(2);
            ids.Must()
             .Contain(userid1);
            ids.Must()
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
            store.GetAll<User>().Must().HaveCount(4);
            store.GetAll<Person>().Must().HaveCount(8); //User inherits person

            store.GetAllIds<User>().ForEach(userId => store.Remove(userId, typeof(User)));

            store.GetAll<User>().Must().HaveCount(0);

            store.GetAll<Person>().Must().HaveCount(4);
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
            ServiceLocator.DocumentDbSession().GetAll<User>().Count().Must().Be(1);
        }
    }

    [PCT]
    public async Task Can_get_all_documents_of_previously_unknown_class_added_by_onother_documentDb_instance_byId()
    {
        var userId = Guid.Parse("00000000-0000-0000-0000-000000000001");

        await InsertUsersInOtherDocumentDb(userId);

        UseInScope(reader => reader.GetAll<User>([new EntityId(userId)])
                                   .Count()
                                   .Must()
                                   .Be(1));
    }
}
