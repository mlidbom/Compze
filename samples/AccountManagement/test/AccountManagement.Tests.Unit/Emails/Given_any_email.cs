using AccountManagement.Domain;
using Compze.Tests.Infrastructure;
using Compze.Must;
using Compze.Must.Assertions;
using Compze.xUnitBDD;

namespace AccountManagement.Tests.Unit.Emails;

public class Given_any_email : UniversalTestBase
{
   [XF] public void ToString_returns_the_string_used_to_create_the_email() => Email.Parse("some.valid@email.com").ToString().Must().Be("some.valid@email.com");
}