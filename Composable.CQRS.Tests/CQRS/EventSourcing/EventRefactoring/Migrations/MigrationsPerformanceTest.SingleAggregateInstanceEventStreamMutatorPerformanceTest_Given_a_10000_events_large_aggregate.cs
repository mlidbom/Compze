﻿using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using Composable.CQRS.EventSourcing;
using Composable.CQRS.EventSourcing.EventRefactoring.Migrations;
using Composable.System.Linq;
using FluentAssertions;
using NUnit.Framework;
using TestAggregates;
using TestAggregates.Events;

namespace CQRS.Tests.CQRS.EventSourcing.EventRefactoring.Migrations
{
    //Everything in here actually runs much faster than this when executed normally, but with ncrunch instrumentation it runs much slower and the test gives leeway for that.....
    public class SingleAggregateInstanceEventStreamMutatorPerformanceTest_Given_a_10000_events_large_aggregate
    {
        private List<AggregateRootEvent> _history;
        [SetUp]
        public void Given_a_10000_events_large_aggregate()
        {
            var historyTypes = Seq.OfTypes<Ec1>()
                                  .Concat(
                                      1.Through(10)
                                       .SelectMany(
                                           index => 1.Through(996)
                                                     .Select(_ => typeof(E1))
                                                     .Concat(Seq.OfTypes<E2, E4, E6, E8>())));

            var aggregate = TestAggregate.FromEvents(Guid.NewGuid(), historyTypes);
            _history = aggregate.History.Cast<AggregateRootEvent>().ToList();
        }

        [Test]
        public void TestSetupPerformance()
        {
            var history = Seq.OfTypes<Ec1>()
                             .Concat(1.Through(100000).Select(_ => typeof(E1)))
                             .ToEvents();

            TimeAsserter.Execute(
                maxTotal: 150.Milliseconds().AdjustRuntimeForNCrunch(),
                action: () => new TestAggregate2(history));
        }

        [Test]
        public void With_four_migrations_mutation_that_all_actually_changes_things_migration_takes_less_than_15_milliseconds()
        {
            var eventMigrations = Seq.Create<IEventMigration>(
                Before<E2>.Insert<E3>(),
                Before<E4>.Insert<E5>(),
                Before<E6>.Insert<E7>(),
                Before<E8>.Insert<E9>()
                ).ToArray();

            var maxAverage = NCrunchPerformance.AdjustRuntime(15.Milliseconds());

            TimeAsserter.Execute(
                maxAverage: maxAverage,
                iterations: 10,
                description: "load aggregate in isolated scope",
                timeFormat: "ss\\.fff",
                action: () => { SingleAggregateInstanceEventStreamMutator.MutateCompleteAggregateHistory(eventMigrations, _history); });
        }

        [Test]
        public void With_four_migrations_that_change_nothing_mutation_takes_less_than_10_milliseconds()
        {
            var eventMigrations = Seq.Create<IEventMigration>(
                Before<E3>.Insert<E1>(),
                Before<E5>.Insert<E1>(),
                Before<E7>.Insert<E1>(),
                Before<E9>.Insert<E1>()
                ).ToArray();

            var maxAverage = NCrunchPerformance.AdjustRuntime(10.Milliseconds());

            TimeAsserter.Execute(
                maxAverage: maxAverage,
                iterations: 10,
                description: "load aggregate in isolated scope",
                timeFormat: "ss\\.fff",
                action: () => { SingleAggregateInstanceEventStreamMutator.MutateCompleteAggregateHistory(eventMigrations, _history); });
        }

        [Test]
        public void Calling_before_after_or_replace_1000000_times_takes_less_than_50_milliseconds()
        {
            var before = Before<E3>.Insert<E2>().CreateMigrator();
            var replace = Replace<E3>.With<E2>().CreateMigrator();
            var after = After<E3>.Insert<E2>().CreateMigrator();
            var @event = new E2();
            var eventModifier = new EventModifier(@event, _ => { });

            var numberOfEventsToInspect = 1000000;
            var maxtime = NCrunchPerformance.AdjustRuntime(50.Milliseconds(), boost: 8.0);

            TimeAsserter.Execute(
                maxTotal: maxtime,
                description: $"{nameof(before)}",
                iterations: numberOfEventsToInspect,
                action: () => before.MigrateEvent(@event, @eventModifier));
            TimeAsserter.Execute(
                maxTotal: maxtime,
                description: $"{nameof(replace)}",
                iterations: numberOfEventsToInspect,
                action: () => replace.MigrateEvent(@event, @eventModifier));
            TimeAsserter.Execute(
                maxTotal: maxtime,
                description: $"{nameof(after)}",
                iterations: numberOfEventsToInspect,
                action: () => after.MigrateEvent(@event, @eventModifier));
        }

        [Test]
        public void When_there_are_no_migrations_mutation_takes_less_than_a_millisecond()
        {
            var eventMigrations = Seq.Create<IEventMigration>().ToArray();

            TimeAsserter.Execute(
                maxAverage: 1.Milliseconds(),
                iterations: 10,
                description: "load aggregate in isolated scope",
                timeFormat: "ss\\.fff",
                action: () => { SingleAggregateInstanceEventStreamMutator.MutateCompleteAggregateHistory(eventMigrations, _history); });
        }
    }

    public class TestAggregate2 : AggregateRootV2<TestAggregate, RootEvent, IRootEvent>
    {
        public void RaiseEvents(params RootEvent[] events)
        {
            if (GetIdBypassContractValidation() == Guid.Empty && events.First().AggregateRootId == Guid.Empty)
            {
                SetIdBeVerySureYouKnowWhatYouAreDoing(Guid.NewGuid());
                events.Cast<AggregateRootEvent>().First().AggregateRootId = Id;
            }

            foreach (var @event in events)
            {
                RaiseEvent(@event);
            }
        }

        private TestAggregate2()
        {
            RegisterEventAppliers()
                .For<IRootEvent>(e => {})
                .For<E1>(e => { })
                .For<E2>(e => { })
                .For<E3>(e => { })
                .For<E4>(e => { })
                .For<E5>(e => { })
                .For<E6>(e => { })
                .For<E7>(e => { })
                .For<E8>(e => { })
                .For<E9>(e => { })
                .For<Ef>(e => { });
        }

        public TestAggregate2(params RootEvent[] events) : this()
        {
            Contract.Requires(events.First() is IAggregateRootCreatedEvent);

            RaiseEvents(events);
        }

    }
}
