using Compze.Abstractions.Time.Public;
using Compze.Tests.Infrastructure;
using Compze.Internals.SystemCE.ReactiveCE;
using Compze.xUnitBDD;
using Compze.Abstractions.Public;
using Compze.Must;
using Compze.Teventive.Public;
using Compze.Teventive.Public.Taggregates.BaseClasses.Public;
using Compze.Teventive.Public.Taggregates.Tevents.Public;

namespace Compze.Tests.Unit.CQRS.Taggregates;

public class TaggregateTests : UniversalTestBase
{
   [XF]
   public void VersionIncreasesWithEachAppliedTevent()
   {
      var user = new User();
      user.Version.Must().Be(0);

      user.Register("email", "password", new TaggregateId());
      user.Version.Must().Be(1);

      user.ChangeEmail("NewEmail");
      user.Version.Must().Be(2);

      user.ChangePassword("NewPassword");
      user.Version.Must().Be(3);
   }

   [XF]
   public void ResetEmptiesOutListOfUncommittedTevents()
   {
      var user = new User();
      ITaggregate userAsteventStored = user;
      user.Version.Must().Be(0);

      user.Register("email", "password", new TaggregateId());
      userAsteventStored.Commit(_ => {});
      userAsteventStored.Commit(tevents => tevents.Must().BeEmpty());

      user.ChangeEmail("NewEmail");
      userAsteventStored.Commit(_ => {});
      userAsteventStored.Commit(tevents => tevents.Must().BeEmpty());

      user.ChangePassword("NewPassword");
      userAsteventStored.Commit(_ => {});
      userAsteventStored.Commit(tevents => tevents.Must().BeEmpty());
   }

   [XF]
   public void When_Raising_tevent_that_triggers_another_tevent_both_tevents_are_outputted_on_the_observable_only_after_the_triggered_tevent_and_in_the_raised_order()
   {
      UtcTimeSource.Test.FrozenAtUtcNow().Run(() =>
      {
         var taggregate = new CascadingTeventsTaggregate();
         var receivedTevents = new List<ITaggregateTevent>();
         using(((ITaggregate)taggregate).TeventStream.Subscribe(tevent =>
               {
                  receivedTevents.Add(tevent);
                  taggregate.TriggeringTeventApplied.Must()
                            .BeTrue();
                  taggregate.TriggeredTeventApplied.Must()
                            .BeTrue();
               }))
         {
            taggregate.RaiseTriggeringTevent();
         }

         receivedTevents.Count.Must().Be(2);
         receivedTevents[0].GetType().Must().Be(typeof(TriggeringTevent));
         receivedTevents[1].GetType().Must().Be(typeof(TriggeredTevent));
      });
   }

   class CascadingTeventsTaggregate : Taggregate<CascadingTeventsTaggregate, ITaggregateTevent, TaggregateTevent, ITaggregateIdentifyingTevent<ITaggregateTevent>, TaggregateIdentifyingTevent<TaggregateTevent>>
   {
      public CascadingTeventsTaggregate()
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

   interface ITriggeringTevent : ITaggregateCreatedTevent;

   class TriggeringTevent() : TaggregateTevent(new TaggregateId()), ITriggeringTevent;

   interface ITriggeredTevent : ITaggregateTevent;
   class TriggeredTevent : TaggregateTevent, ITriggeredTevent;
}
