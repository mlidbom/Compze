using System;
using Composable.DDD;
using JetBrains.Annotations;
using NUnit.Framework;
#pragma warning disable IDE0052 //Review OK:unread private members are intentional in this test.

namespace Composable.Tests.DDD
{
    [TestFixture]
    public class ValueObjectTests
    {
        class Address : ValueObject<Address>
        {

            [UsedImplicitly] readonly string _address1;
            [UsedImplicitly] readonly string _city;
            [UsedImplicitly] readonly string[] _states;

            public Address(string address1, string city, params string[] states)
            {
                _address1 = address1;
                _city = city;
                _states = states;
            }

            public Address()
            {}

            // ReSharper disable once UnusedAutoPropertyAccessor.Local
            public string Guid { get;  set; }
        }

        class GuidHolder : ValueObject<GuidHolder>
        {
            public GuidHolder(Guid id) => Id = id;

            // ReSharper disable once UnusedAutoPropertyAccessor.Local
            Guid Id { get; set; }
        }

        class ExpandedAddress : Address
        {
            [UsedImplicitly] readonly string _address2;

            public ExpandedAddress(string address1, string address2, string city, params string[] states)
                : base(address1, city, states) => _address2 = address2;
        }

        [Test]
        public void AddressEqualsWorksWithIdenticalAddresses()
        {
            var address = new Address("Address1", "Austin", "TX");
            var address2 = new Address("Address1", "Austin", "TX");

            Assert.That(address.Equals(address2), Is.True);
        }

        [Test]
        public void GuidEqualsWorksWithIdenticalGuid()
        {
            var id = Guid.NewGuid();
            var guid1 = new GuidHolder(id);
            var guid2 = new GuidHolder(id);

            Assert.That(guid1.Equals(guid2), Is.True);
        }

        [Test]
        public void GuidEqualsWorksWithNonIdenticalGuid()
        {
            var guid1 = new GuidHolder(Guid.NewGuid());
            var guid2 = new GuidHolder(Guid.NewGuid());

            Assert.That(guid1.Equals(guid2), Is.False);
        }

        [Test]
        public void AddressEqualsWorksWithNonIdenticalGuids()
        {
            var address = new Address("Address1", "Austin", "TX") { Guid = "test" };
            var address2 = new Address("Address2", "Austin", "TX");

            Assert.That(address.Equals(address2), Is.False);
        }

        [Test]
        public void AddressEqualsWorksWithNulls()
        {
            var address = new Address(null, "Austin", "TX");
            var address2 = new Address("Address2", "Austin", "TX");

            Assert.That(address.Equals(address2), Is.False);
        }

        [Test]
        public void AddressEqualsWorksWithNullsOnOtherObject()
        {
            var address = new Address("Address2", "Austin", "TX");
            var address2 = new Address("Address2", null, "TX");

            Assert.That(address.Equals(address2), Is.False);
        }

        [Test]
        public void AddressEqualsIsReflexive()
        {
            var address = new Address("Address1", "Austin", "TX");

            Assert.That(address.Equals(address), Is.True);
        }

        [Test]
        public void AddressEqualsIsSymmetric()
        {
            var address = new Address("Address1", "Austin", "TX");
            var address2 = new Address("Address2", "Austin", "TX");

            Assert.That(address.Equals(address2), Is.False);
            Assert.That(address2.Equals(address), Is.False);
        }

        [Test]
        public void AddressEqualsIsTransitive()
        {
            var address = new Address("Address1", "Austin", "TX");
            var address2 = new Address("Address1", "Austin", "TX");
            var address3 = new Address("Address1", "Austin", "TX");

            Assert.That(address.Equals(address2), Is.True);
            Assert.That(address2.Equals(address3), Is.True);
            Assert.That(address.Equals(address3), Is.True);
        }

        [Test]
        public void AddressOperatorsWork()
        {
            var address = new Address("Address1", "Austin", "TX");
            var address2 = new Address("Address1", "Austin", "TX");
            var address3 = new Address("Address2", "Austin", "TX");

            Assert.That(address == address2);
            Assert.That(address2 != address3);
        }

        [Test]
        public void DerivedTypesBehaveCorrectly()
        {
            var address = new Address("Address1", "Austin", "TX");
            var address2 = new ExpandedAddress("Address1", "Apt 123", "Austin", "TX");

            Assert.That(address.Equals(address2), Is.False);
            Assert.That(address == address2, Is.False);
        }

        [Test]
        public void EqualValueObjectsHaveSameHashCode()
        {
            var address = new Address("Address1", "Austin", "TX");
            var address2 = new Address("Address1", "Austin", "TX");

            Assert.That(address.GetHashCode(), Is.EqualTo(address2.GetHashCode()));
        }

        [Test]
        public void EqualValuesInEnumerableObjectsHaveSameHashCode()
        {
            var address = new Address("Address1", "Austin", "TX", "BB");
            var address2 = new Address("Address1", "Austin", "TX", "BB");

            Assert.That(address.GetHashCode(), Is.EqualTo(address2.GetHashCode()));
        }

        [Test]
        public void DifferentNumberOfEntriesInArrayMeansObjectAreNotEqual()
        {
            var address = new Address("Address1", "Austin", "TX");
            var address2 = new Address("Address1", "Austin", "TX", "TX");

            Assert.That(address, Is.Not.EqualTo(address2));
        }

        [Test]
        public void DifferentNumberOfEntriesInArrayMeansHashAreNotEqual()
        {
            var address = new Address("Address1", "Austin", "TX");
            var address2 = new Address("Address1", "Austin", "TX", "TX");

            Assert.That(address.GetHashCode(), Is.Not.EqualTo(address2.GetHashCode()));
        }

        [Test]
        public void MultipleValuesInEnumerableAreStillEqual()
        {
            var address = new Address("Address1", "Austin", "TX", "TX");
            var address2 = new Address("Address1", "Austin", "TX", "TX");

            Assert.That(address, Is.EqualTo(address2));
        }

        [Test]
        public void EnumerablesHandleNulls()
        {
            var address = new Address("Address1", "Austin", "TX", null, "TX");
            var address2 = new Address("Address1", "Austin", "TX", null, "TX");

            Assert.That(address, Is.EqualTo(address2));
        }

        [Test]
        public void TransposedValuesGiveDifferentHashCodes()
        {
            var address = new Address(null, "Austin", "TX");
            var address2 = new Address("TX", "Austin", null);

            Assert.That(address.GetHashCode(), Is.Not.EqualTo(address2.GetHashCode()));
        }

        [Test]
        public void UnequalValueObjectsHaveDifferentHashCodes()
        {
            var address = new Address("Address1", "Austin", "TX");
            var address2 = new Address("Address2", "Austin", "TX");

            Assert.That(address.GetHashCode(), Is.Not.EqualTo(address2.GetHashCode()));
        }

        [Test]
        public void TransposedValuesOfFieldNamesGivesDifferentHashCodes()
        {
            var address = new Address("_city", null, null);
            var address2 = new Address(null, "_address1", null);

            Assert.That(address.GetHashCode(), Is.Not.EqualTo(address2.GetHashCode()));
        }

        [Test]
        public void DerivedTypesHashCodesBehaveCorrectly()
        {
            var address = new ExpandedAddress("Address99999", "Apt 123", "New Orleans", "LA");
            var address2 = new ExpandedAddress("Address1", "Apt 123", "Austin", "TX");

            Assert.That(address.GetHashCode(), Is.Not.EqualTo(address2.GetHashCode()));
        }

        [Test]
        public void ComparisonWithRhsNullReturnsFalse()
        {
            var lhs = new Address();
            Assert.That(lhs.Equals(null!), Is.False);
            // ReSharper disable once ConditionIsAlwaysTrueOrFalse
            Assert.That(lhs == null, Is.False);
        }

        [Test]
        public void ComparisonWithLhsNullReturnsFalse()
        {
            var rhs = new Address();
            // ReSharper disable once ConditionIsAlwaysTrueOrFalse
            Assert.That(null == rhs, Is.False);
        }

        [Test]
        public void ComparisonWithLhsNullAndRhsNullReturnsTrue()
        {
            Address rhs = null;
            Address lhs = null;
            // ReSharper disable once ConditionIsAlwaysTrueOrFalse
            Assert.That(rhs == lhs, Is.True);
        }
    }
}