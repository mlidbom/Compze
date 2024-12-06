using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Compze.DDD;
using Compze.DependencyInjection;
using Compze.Persistence.DocumentDb;
using Compze.SystemCE;
using Compze.SystemCE.LinqCE;
using Compze.SystemCE.ThreadingCE;
using Compze.Testing;
using Compze.Testing.DependencyInjection;
using FluentAssertions;
using NUnit.Framework;
using static FluentAssertions.FluentActions;

// ReSharper disable AccessToDisposedClosure

namespace Compze.Tests.Persistence.DocumentDb;

class DocumentDbTests(string pluggableComponentsCombination) : DocumentDbTestsBase(pluggableComponentsCombination)
{
   [Test]
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
                                 Streetnumber = 234
                              }
                 };

      UseInTransactionalScope((_,updater) => updater.Save(user.Id, user));

      UseInScope(reader =>
      {
         var loadedUser = reader.Get<User>(user.Id);

         Assert.That(loadedUser.Id, Is.EqualTo(user.Id));
         Assert.That(loadedUser.Email, Is.EqualTo(user.Email));
         Assert.That(loadedUser.Password, Is.EqualTo(user.Password));

         Assert.That(loadedUser.Address, Is.EqualTo(user.Address));
      });
   }

   [Test]
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

   [Test] public void GetAllWithIdsThrowsNoSuchDocumentExceptionExceptionIfAnyIdIsMissing()
   {
      var ids = 1.Through(9)
                 .Select(index => Guid.Parse($"00000000-0000-0000-0000-00000000000{index}"))
                 .ToArray();

      var users = ids.Select(id => new User(id))
                     .ToArray();

      UseInTransactionalScope((_,updater) => users.ForEach(user => updater.Save(user)));

      UseInScope(reader => Invoking(
                    () => reader.GetAll<User>(ids.Take(5)
                                                 .Append(Guid.Parse("00000000-0000-0000-0000-000000000099"))
                                                 .ToArray())
                                 // ReSharper disable once ReturnValueOfPureMethodIsNotUsed
                                .ToArray()).Should().Throw<ArgumentOutOfRangeException>());
   }


   [Test]
   public void GetAllWithIdsReturnsTheSameInstanceForAnyPreviouslyFetchedDocuments()
   {
      var ids = 1.Through(9).Select(index => Guid.Parse($"00000000-0000-0000-0000-00000000000{index}")).ToArray();

      var users = ids.Select(id => new User(id)).ToArray();

      UseInTransactionalScope((_,updater) => users.ForEach(user => updater.Save(user)));

      UseInScope(reader =>
      {
         var fetchedIndividually = ids.Select(id => reader.Get<User>(id))
                                      .ToArray();
         var fetchedWithGetAll = reader.GetAll<User>(ids)
                                       .ToArray();

         fetchedIndividually.ForEach((user, index) => Assert.That(user, Is.SameAs(fetchedWithGetAll[index])));
      });
   }



   [Test]
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
                                 Streetnumber = 234
                              }
                 };

      UseInTransactionalScope((_,updater) => updater.Save(user.Id, user));

      UseInTransactionalScope((_, updater) =>
      {
         var loadedUser = updater.GetForUpdate<User>(user.Id);

         Assert.That(loadedUser.Id, Is.EqualTo(user.Id));
         Assert.That(loadedUser.Email, Is.EqualTo(user.Email));
         Assert.That(loadedUser.Password, Is.EqualTo(user.Password));

         Assert.That(loadedUser.Address, Is.EqualTo(user.Address));
      });
   }

   [Test]
   public void CallingSaveWithAnInteraceAsTypeParameterDoesNotExplode()
   {
      IPersistentEntity<Guid> user1 = new User { Email = "user1" };
      IPersistentEntity<Guid> user2 = new User { Email = "user2" };

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

   [Test]
   public void AddingAndRemovingObjectResultsInNoObjectBeingSaved()
   {
      var user = new User();

      UseInTransactionalScope((_,updater) =>
      {
         updater.Save(user.Id, user);
         updater.Delete(user);
      });

      UseInScope(reader =>
                    reader.TryGet(user.Id, out user)
                          .Should()
                          .BeFalse());
   }

   [Test]
   public void AddingRemovingAndAddingObjectInTransactionResultsInNoObjectBeingSaved()
   {
      var user = new User();

      UseInTransactionalScope((_,updater) =>
      {
         updater.Save(user.Id, user);
         updater.Delete(user);
         updater.Save(user.Id, user);
      });

      UseInScope(reader => reader.TryGet(user.Id, out user).Should().BeTrue());
   }

   [Test]
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

   [Test]
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

   [Test]
   public void TryingToFetchNonExistentItemDoesNotCauseSessionToTryAndAddItWithANullInstance()
   {
      var user = new User();

      UseInScope(reader => reader.TryGet(user.Id, out user)
                                 .Should()
                                 .Be(false));
   }

   [Test]
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

   [Test]
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


   [Test]
   public void ReturnsSameInstanceOnRepeatedLoads()
   {
      var user = new User();

      UseInTransactionalScope((_, updater) => updater.Save(user.Id, user));

      UseInScope(reader =>
      {
         var loaded1 = reader.Get<User>(user.Id);
         var loaded2 = reader.Get<User>(user.Id);
         Assert.That(loaded1, Is.SameAs(loaded2));
      });
   }

   [Test]
   public void ReturnsSameInstanceOnLoadAfterSave()
   {
      var user = new User();

      UseInTransactionalScope((reader, updater) =>
      {
         updater.Save(user.Id, user);

         var loaded1 = reader.Get<User>(user.Id);
         var loaded2 = reader.Get<User>(user.Id);
         Assert.That(loaded1, Is.SameAs(loaded2));
         Assert.That(loaded1, Is.SameAs(user));
      });
   }

   [Test]
   public void HandlesHashSets()
   {
      var user = new User();
      var userSet = new HashSet<User> { user };

      UseInTransactionalScope((_, updater) => updater.Save(user.Id, userSet));

      UseInScope(reader =>
      {
         var loadedUser = reader.Get<HashSet<User>>(user.Id);
         Assert.That(loadedUser.Count, Is.EqualTo(1));
      });
   }

   [Test]
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
         Assert.That(loadedUser.People.Count, Is.EqualTo(1));
         var loadedUserInSet = loadedUser.People.Single();
         Assert.That(loadedUserInSet.Id, Is.EqualTo(userInSet.Id));
      });
   }


   [Test]
   public void ThrowsExceptionWhenAttemptingToDeleteNonExistingValue()
   {
      UseInTransactionalScope((_, updater) =>
      {
         var lassie = new Dog {Id = Guid.NewGuid()};
         updater.Save(lassie);
      });

      var buster = new Dog { Id = Guid.NewGuid() };
      UseInTransactionalScope((_, updater) => Invoking(() => updater.Delete(buster)).Should().Throw<ArgumentOutOfRangeException>());
   }

   [Test]
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

   [Test]
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

   [Test]
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

   [Test]
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
         Assert.That(loadedUser.Password, Is.EqualTo("NewPassword"));
      });
   }

   [Test]
   public void ThrowsWhenAttemptingToSaveExistingAggregate()
   {
      var user = new User();

      UseInTransactionalScope((_, updater) => updater.Save(user.Id, user));

      Invoking(() => UseInTransactionalScope((_, updater) => updater.Save(user.Id, user))).Should().Throw<ArgumentException>();
   }

   [Test]
   public void HandlesInstancesOfDifferentTypesWithTheSameId()
   {
      var user = new User
                 {
                    Email = "email"
                 };

      var dog = new Dog { Id = user.Id };

      UseInTransactionalScope((_, updater) =>
      {
         updater.Save<IPersistentEntity<Guid>>(user);
         updater.Save<IPersistentEntity<Guid>>(dog);
      });

      UseInScope(reader =>
      {
         var loadedDog = reader.Get<Dog>(dog.Id);
         var loadedUser = reader.Get<User>(dog.Id);

         Assert.That(loadedDog.Name, Is.EqualTo(dog.Name));
         Assert.That(loadedUser.Email, Is.EqualTo(user.Email));
         Assert.That(loadedDog.Id, Is.EqualTo(user.Id));
         Assert.That(loadedUser.Id, Is.EqualTo(user.Id));
      });
   }


   [Test]
   public void FetchesAllinstancesPerType()
   {
      UseInTransactionalScope((_, updater) =>
      {
         updater.Save(new User());
         updater.Save(new User());
         updater.Save(new Dog {Id = Guid.NewGuid()});
         updater.Save(new Dog {Id = Guid.NewGuid()});
      });

      using (ServiceLocator.BeginScope())
      {
         Assert.That(ServiceLocator.DocumentDbBulkReader().GetAll<Dog>().ToList(), Has.Count.EqualTo(2));
         Assert.That(ServiceLocator.DocumentDbBulkReader().GetAll<User>().ToList(), Has.Count.EqualTo(2));
      }
   }

   [Test]
   public void ThrowsIfUsedByMultipleThreads()
   {
      IDocumentDbSession? session = null;
      using var wait = new ManualResetEventSlim();
      var task = Task.Run(() =>
      {
         ServiceLocator.ExecuteInIsolatedScope(() => session = ServiceLocator.DocumentDbSession());
         wait.Set();
      });
      wait.Wait();
      task.Wait();
      session = session.NotNull();

      var user = new User();

      Assert.Throws<MultiThreadedUseException>(() => session.Get<User>(Guid.NewGuid()));
      Assert.Throws<MultiThreadedUseException>(() => session.GetAll<User>());
      Assert.Throws<MultiThreadedUseException>(() => session.Save(user, user.Id));
      Assert.Throws<MultiThreadedUseException>(() => session.Delete(user));
      Assert.Throws<MultiThreadedUseException>(() => session.Dispose());
      Assert.Throws<MultiThreadedUseException>(() => session.Save(new User()));
      Assert.Throws<MultiThreadedUseException>(() => session.TryGet(Guid.NewGuid(), out user));
      Assert.Throws<MultiThreadedUseException>(() => session.Delete(user));
   }


   [Test]
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
         Assert.That(reader.Get<Person>(user1.Id), Is.EqualTo(user1));
         Assert.That(reader.Get<Person>(person1.Id), Is.EqualTo(person1));
      });
   }

   [Test]
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

         Assert.That(people, Has.Count.EqualTo(2));
         Assert.That(people, Contains.Item(user1));
         Assert.That(people, Contains.Item(person1));
      }
   }

   [Test]
   public void ThrowsExceptionIfYouTryToSaveAnIHasPersistentIdentityWithNoId()
   {
      var user1 = new User(Guid.Empty);

      UseInTransactionalScope((_, updater) => updater.Invoking(it => it.Save(user1))
                                                     .Should().Throw<Exception>());
   }

   [Test]
   public void GetByIdsShouldReturnOnlyMatchingResultEvenWhenMoreResultsAreInTheCache()
   {
      var user1 = new User(Guid.Parse("00000000-0000-0000-0000-000000000001"));
      var user2 = new User(Guid.Parse("00000000-0000-0000-0000-000000000002"));

      UseInTransactionalScope((reader, updater) =>
      {
         updater.Save(user1);
         updater.Save(user2);

         var people = reader.GetAll<User>([user1.Id]);

         Assert.That(people.ToList(), Has.Count.EqualTo(1));
         Assert.That(people, Contains.Item(user1));
      });
   }


   [Test]
   public void GetAllIdsShouldOnlyReturnResultsWithTheGivenType()
   {
      var userid1 = Guid.Parse("00000000-0000-0000-0000-000000000001");
      var userid2 = Guid.Parse("00000000-0000-0000-0000-000000000002");

      var user1 = new User(userid1);
      var user2 = new User(userid2);
      var dog = new Dog {Id = Guid.Parse("00000000-0000-0000-0000-000000000010") };

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

   [Test]
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


   [Test]
   public void DeletingAllObjectsOfATypeLeavesNoSuchObjectsInTheDbButLeavesOtherObjectsInPlaceAndReturnsTheNumberOfDeletedObjects()
   {
      using(ServiceLocator.BeginScope())
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

      using(ServiceLocator.BeginScope())
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

   [Test]
   public async Task Can_get_document_of_previously_unknown_class_added_by_onother_documentDb_instance()
   {
      var userId = Guid.Parse("00000000-0000-0000-0000-000000000001");

      await InsertUsersInOtherDocumentDb(userId);

      using(ServiceLocator.BeginScope())
      {
         ServiceLocator.DocumentDbSession().Get<User>(userId);
      }
   }

   [Test]
   public async Task Can_get_all_documents_of_previously_unknown_class_added_by_onother_documentDb_instance()
   {
      var userId = Guid.Parse("00000000-0000-0000-0000-000000000001");

      await InsertUsersInOtherDocumentDb(userId);

      using (ServiceLocator.BeginScope())
      {
         ServiceLocator.DocumentDbSession().GetAll<User>().Count().Should().Be(1);
      }
   }

   [Test]
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