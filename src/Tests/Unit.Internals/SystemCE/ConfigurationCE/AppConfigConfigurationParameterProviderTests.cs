using System;
using Compze.Tessaging.Hosting.Configuration;
using Compze.Tests.Infrastructure;
using FluentAssertions;
using NUnit.Framework;
using Compze.Tests.Infrastructure.NUnit;

namespace Compze.Tests.Unit.Internals.SystemCE.ConfigurationCE;

[TestFixture] public class AppConfigConfigurationParameterProviderTests: UniversalTestBase
{
   IConfigurationParameterProvider _provider;
   [SetUp] public void SetupTask() => _provider = AppSettingsJsonConfigurationParameterProvider.Instance;

   [Test] public void ParameterProvider_should_return_the_value_specified_in_the_configuration_file() =>
      Assert.That(_provider.GetString("KeyTest1"), Is.EqualTo("ValueTest1"));

   [Test] public void ParameterProvider_should_throw_ConfigurationErrorsException_when_key_does_not_exist() =>
      Assert.Throws<Exception>(() => _provider.GetString("ErrorTest1"));

   [Test] public void ParameterProvider_exception_should_contain_parameter_key() =>
      this.Invoking(_ => _provider.GetString("ErrorTest1"))
          .Should().Throw<Exception>()
          .And.Message.Should()
          .Contain("ErrorTest1");
}