﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Composable.Serialization;
using Composable.SystemCE.LinqCE;
using Composable.SystemCE.ThreadingCE;
using Composable.Testing.Threading;
using FluentAssertions;
using FluentAssertions.Extensions;
using JetBrains.Annotations;
using NUnit.Framework;

// ReSharper disable ImplicitlyCapturedClosure

namespace Composable.Tests.SystemCE.ThreadingCE
{
    [UsedImplicitly] class SharedObject : BinarySerialized<SharedObject>
    {
        public string Name { get; set; } = "Default";
        protected override IEnumerable<MemberGetterSetter> CreateGetterSetters() => new[] {GetterSetter.ForString(@this => @this.Name, (@this, value) => @this.Name = value)};
    }

    [TestFixture] public class MachineWideSharedObjectTests
    {
        [Test] public void Create()
        {
            var name = Guid.NewGuid().ToString();
            using var shared = MachineWideSharedObject<SharedObject>.For(name);
            var test = shared.GetCopy();

            test.Name.Should().Be("Default");
        }

        [Test] public void Create_update_and_get()
        {
            var name = Guid.NewGuid().ToString();
            using var shared = MachineWideSharedObject<SharedObject>.For(name);
            var test = shared.GetCopy();

            test.Name.Should().Be("Default");

            test = shared.Update(@this => @this.Name = "Updated");

            test.Name.Should().Be("Updated");

            test = shared.GetCopy();

            test.Name.Should().Be("Updated");
        }

        [Test] public void Two_instances_with_same_name_share_data()
        {
            var name = Guid.NewGuid().ToString();
            using var shared1 = MachineWideSharedObject<SharedObject>.For(name);
            using var shared2 = MachineWideSharedObject<SharedObject>.For(name);
            var test1 = shared1.GetCopy();
            var test2 = shared2.GetCopy();

            test1.Name.Should().Be("Default");
            test2.Name.Should().Be("Default");

            test1 = shared1.Update(@this => @this.Name = "Updated");
            test2 = shared2.GetCopy();

            test1.Name.Should().Be("Updated");
            test2.Name.Should().Be("Updated");

            test1 = shared1.GetCopy();
            test1.Name.Should().Be("Updated");
        }

        [Test] public void Non_persistent_Once_all_instance_are_disposed_data_is_gone()
        {
            var name = Guid.NewGuid().ToString();
            MachineWideSharedObject<SharedObject> shared2;
            using(var shared = MachineWideSharedObject<SharedObject>.For(name))
            {
                shared.Update(@this => @this.Name = "New").Name.Should().Be("New");
                shared2 = MachineWideSharedObject<SharedObject>.For(name);
                shared.GetCopy().Name.Should().Be("New");
            }

            shared2.GetCopy().Name.Should().Be("New");
            shared2.Dispose();

            using(var shared = MachineWideSharedObject<SharedObject>.For(name))
            {
                shared.GetCopy().Name.Should().Be("Default");
            }
        }

        [Test] public void Persistent_Once_all_instance_are_disposed_data_is_retained()
        {
            var name = "40BD77DF-7C32-4B28-9A49-DA2CE202CC4F";
            var newName = Guid.NewGuid().ToString();
            MachineWideSharedObject<SharedObject> shared2;
            using(var shared = MachineWideSharedObject<SharedObject>.For(name, usePersistentFile:true))
            {
                shared.Update(@this => @this.Name = newName).Name.Should().Be(newName);
                shared2 = MachineWideSharedObject<SharedObject>.For(name, usePersistentFile:true);
                shared.GetCopy().Name.Should().Be(newName);
            }

            shared2.GetCopy().Name.Should().Be(newName);
            shared2.Dispose();

            using(var shared = MachineWideSharedObject<SharedObject>.For(name, usePersistentFile:true))
            {
                shared.GetCopy().Name.Should().Be(newName);
            }
        }

        [Test] public void Update_blocks_GetCopy_and_Update_from_both_same_and_other_instances()
        {
            var timeout = 1.Seconds();
            var updateGate = ThreadGate.CreateClosedWithTimeout(timeout);
            var conflictingUpdateSectionSameInstance = GatedCodeSection.WithTimeout(timeout);
            var conflictingUpdateSectionOtherInstance = GatedCodeSection.WithTimeout(timeout);
            var conflictingGetCopySectionSameInstance = GatedCodeSection.WithTimeout(timeout);
            var conflictingGetCopySectionOtherInstance = GatedCodeSection.WithTimeout(timeout);

            var conflictingSections = EnumerableCE.Create(conflictingUpdateSectionSameInstance,
                                              conflictingUpdateSectionOtherInstance,
                                              conflictingGetCopySectionSameInstance,
                                              conflictingGetCopySectionOtherInstance).ToList();

            var name = Guid.NewGuid().ToString();
            using var shared1 = MachineWideSharedObject<SharedObject>.For(name);
            using var shared2 = MachineWideSharedObject<SharedObject>.For(name);
            using var taskRunner = new TestingTaskRunner(timeout);
            // ReSharper disable AccessToDisposedClosure
            taskRunner.Start(() => shared1.Update(@this => { updateGate.AwaitPassThrough(); }));
            taskRunner.Start(() => conflictingUpdateSectionSameInstance.Execute(() => shared1.Update(me => {})));
            taskRunner.Start(() => conflictingGetCopySectionSameInstance.Execute(() => shared1.GetCopy()));
            taskRunner.Start(() => conflictingUpdateSectionOtherInstance.Execute(() => shared2.Update(me => {})));
            taskRunner.Start(() => conflictingGetCopySectionOtherInstance.Execute(() => shared2.GetCopy()));

            updateGate.AwaitQueueLengthEqualTo(1);
            conflictingSections.ForEach(section =>
            {
                section.EntranceGate.AwaitQueueLengthEqualTo(1);
                section.Open();
            });

            Thread.Sleep(50.Milliseconds());

            conflictingSections.ForEach(gate => gate.ExitGate.PassedThrough.Count.Should().Be(0));
            updateGate.Open();
            conflictingSections.ForEach(gate => gate.ExitGate.AwaitPassedThroughCountEqualTo(1));

            taskRunner.WaitForTasksToComplete();
            // ReSharper restore AccessToDisposedClosure
        }
    }
}
