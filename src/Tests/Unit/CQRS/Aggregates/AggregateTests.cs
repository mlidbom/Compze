using System;
using System.Collections.Generic;
using Compze.Abstractions.Tessaging.Teventive.TeventStore.Public;
using Compze.Abstractions.Tessaging.Teventive.Public;
using Compze.Abstractions.Time.Testing.Public;
using Compze.Tessaging.Teventive;
using Compze.Tests.Infrastructure;
using Compze.Utilities.SystemCE.ReactiveCE;
using Compze.Utilities.Testing.XUnit.BDD;
using FluentAssertions;

namespace Compze.Tests.Unit.CQRS.Aggregates;

public class AggregateTests : UniversalTestBase
{
   [XF]
   public void VersionIncreasesWithEachAppliedTevent()
   {
      var user = new User();
      user.Version.Should().Be(0);

      user.Register("email", "password", Guid.NewGuid());
      user.Version.Should().Be(1);

      user.ChangeEmail("NewEmail");
      user.Version.Should().Be(2);

      user.ChangePassword("NewPassword");
      user.Version.Should().Be(3);

   }

   [XF]
   public void ResetEmptiesOutListOfUncommittedTevents()
   {
      var user = new User();
      ITeventStored userAsteventStored = user;
      user.Version.Should().Be(0);

      user.Register("email", "password", Guid.NewGuid());
      userAsteventStored.Commit(_ => {});
      userAsteventStored.Commit(tevents => tevents.Should().BeEmpty());

      user.ChangeEmail("NewEmail");
      userAsteventStored.Commit(_ => {});
      userAsteventStored.Commit(tevents => tevents.Should().BeEmpty());

      user.ChangePassword("NewPassword");
      userAsteventStored.Commit(_ => {});
      userAsteventStored.Commit(tevents => tevents.Should().BeEmpty());
   }




   [XF]
   public void When_Raising_tevent_that_triggers_another_tevent_both_tevents_are_outputted_on_the_observable_only_after_the_triggered_tevent_and_in_the_raised_order()
   {
      var aggregate = new CascadingTeventsAggregate();
      var receivedTevents = new List<IAggregateTevent>();
      using(((ITeventStored)aggregate).TeventStream.Subscribe(@tevent =>
            {
               receivedTevents.Add(@tevent);
               aggregate.TriggeringTeventApplied.Should()
                        .BeTrue();
               aggregate.TriggeredTeventApplied.Should()
                        .BeTrue();
            }))
      {
         aggregate.RaiseTriggeringTevent();
      }

      receivedTevents.Count.Should().Be(2);
      receivedTevents[0].GetType().Should().Be<TriggeringTevent>();
      receivedTevents[1].GetType().Should().Be<TriggeredTevent>();
   }

   class CascadingTeventsAggregate : Aggregate<CascadingTeventsAggregate, IAggregateTevent, AggregateTevent>
   {
      public CascadingTeventsAggregate():base(TestingTimeSource.FrozenUtcNow())
      {
         RegisterTeventHandlers()
           .For<ITriggeringTevent>(_ => Publish(new TriggeredTevent()));

         RegisterTeventAppliers()
           .For<ITriggeringTevent>(_ => TriggeringTeventApplied = true)
           .For<ITriggeredTevent>(_ => TriggeredTeventApplied = true);
      }
      public bool TriggeredTeventApplied { get; private set; }
      public bool TriggeringTeventApplied { get; private set; }
      public void RaiseTriggeringTevent() => Publish(new TriggeringTevent());
   }

   interface ITriggeringTevent : IAggregateCreatedTevent;

   class TriggeringTevent() : AggregateTevent(Guid.NewGuid()), ITriggeringTevent;

   interface ITriggeredTevent : IAggregateTevent;
   class TriggeredTevent : AggregateTevent, ITriggeredTevent;
}