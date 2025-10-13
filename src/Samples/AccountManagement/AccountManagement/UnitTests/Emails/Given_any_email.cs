using AccountManagement.Domain;
using Compze.Tests.Infrastructure;
using FluentAssertions;
using NUnit.Framework;
using Compze.Tests.Infrastructure.NUnit;

namespace AccountManagement.Tests.Unit.Emails;

[TestFixture] public class Given_any_email : NUnitTestBase
{
   [Test] public void ToString_returns_the_string_used_to_create_the_email() => Email.Parse("some.valid@email.com").ToString().Should().Be("some.valid@email.com");
}