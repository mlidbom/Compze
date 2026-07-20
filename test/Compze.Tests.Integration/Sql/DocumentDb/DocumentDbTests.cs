using Compze.Abstractions.Public;
using Compze.Hosting.Testing.Wiring;
using Compze.Tests.Common;
using Compze.Tests.Common.Sql.DocumentDb;
using Compze.DependencyInjection;
using Compze.Internals.SystemCE.TransactionsCE;
using Compze.Tests.Infrastructure.XUnit;
using Compze.Internals.SystemCE.UsageGuards;
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
    public void HandlesNestedHashSets()
    {
        var user = new User();
        var userSet = new UserSet { Users = [user] };

        UseInTransactionalScope((_, updater) => updater.Save(user.Id, userSet));

        UseInScope(reader =>
        {
            var loadedUser = reader.Get<UserSet>(user.Id);
            loadedUser.Users.Count.Must().Be(1);
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

        using var scope = Container.BeginScope();
        scope.Resolver.DocumentDbBulkReader().GetAll<Dog>().ToList().Must().HaveCount(2);
        scope.Resolver.DocumentDbBulkReader().GetAll<User>().ToList().Must().HaveCount(2);
    }

    [PCT]
    public void ThrowsIfUsedByMultipleTransactions()
    {
        //The session's affinity is its transaction, never a thread: an async unit of work legitimately migrates across threads,
        //while one session serving two transactions is the misuse that must fail loud.
        Container.ExecuteInIsolatedScope(scope =>
        {
            var session = scope.DocumentDbSession();
            TransactionScopeCe.Execute(() => session.TryGet(Guid.NewGuid(), out User? _));
            TransactionScopeCe.Execute(() => Invoking(() => session.TryGet(Guid.NewGuid(), out User? _))
                                                           .Must().Throw<ComponentUsedByMultipleTransactionsException>());
        });
    }


    [PCT]
    public void GetReturnsOnlyDocumentsStoredAsTheExactQueriedTypeNotSubtypes()
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
            reader.Get<Person>(person1.Id).Must().DeepEqual(person1);
            // user1 is stored under its concrete type User, not Person, so a Person query does not return it.
            reader.TryGet<Person>(user1.Id, out _).Must().BeFalse();
        });
    }

    [PCT]
    public void GetAllReturnsOnlyDocumentsStoredAsTheExactQueriedTypeNotSubtypes()
    {
        var user1 = new User();
        var person1 = new Person();

        UseInTransactionalScope((_, updater) =>
        {
            updater.Save(user1);
            updater.Save(person1);
        });

        using var scope = Container.BeginScope();
        var people = scope.Resolver.DocumentDbBulkReader().GetAll<Person>().Select(it => it.Id).ToHashSet();

        // Only person1 comes back: user1 is stored as User, not Person. The exact count rules out the User being included.
        people.Must().HaveCount(1);
        people.Must().Contain(person1.Id);
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

        Container.ExecuteInIsolatedScope(scope =>
        {
            var ids = scope.DocumentDbBulkReader()
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

        Container.ExecuteUnitOfWork(unitOfWork =>
        {
            var updater = unitOfWork.DocumentDbUpdater();
            updater.Save(user1);
            updater.Save(user2);
            updater.Save(dog);

            var ids = unitOfWork.DocumentDbBulkReader()
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
        {
            using var scope1 = Container.BeginScope();
            var store = scope1.Resolver.DocumentDb();

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

        {
            using var scope2 = Container.BeginScope();
            var store = scope2.Resolver.DocumentDb();
            store.GetAll<User>().Must().HaveCount(4);
            store.GetAll<Person>().Must().HaveCount(4); //Users are stored as User, not Person, so only the 4 Persons match

            store.GetAllIds<User>().ForEach(userId => store.Remove(userId, typeof(User)));

            store.GetAll<User>().Must().HaveCount(0);

            store.GetAll<Person>().Must().HaveCount(4);
        }

    }

    async Task InsertUsersInOtherDocumentDb(Guid userId)
    {
        await using var clonedContainer = Container.CloneAndBuild();
        clonedContainer.ExecuteUnitOfWork(unitOfWork => unitOfWork.DocumentDbUpdater()
                                                                                       .Save(new User(userId)));
    }

    [PCT]
    public async Task Can_get_document_of_previously_unknown_class_added_by_onother_documentDb_instance()
    {
        var userId = Guid.Parse("00000000-0000-0000-0000-000000000001");

        await InsertUsersInOtherDocumentDb(userId);

        using var scope3 = Container.BeginScope();
        scope3.Resolver.DocumentDbSession().Get<User>(userId);
    }

    [PCT]
    public async Task Can_get_all_documents_of_previously_unknown_class_added_by_onother_documentDb_instance()
    {
        var userId = Guid.Parse("00000000-0000-0000-0000-000000000001");

        await InsertUsersInOtherDocumentDb(userId);

        using var scope4 = Container.BeginScope();
        scope4.Resolver.DocumentDbSession().GetAll<User>().Count().Must().Be(1);
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

