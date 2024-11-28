using AccountManagement.Domain;
using Composable.Testing;
using FluentAssertions;
using NUnit.Framework;

namespace AccountManagement.UnitTests.Emails;

[TestFixture] public class Given_any_email : UniversalTestBase
{
   [Test] public void ToString_returns_the_string_used_to_create_the_email() => Email.Parse("some.valid@email.com").ToString().Should().Be("some.valid@email.com");
}