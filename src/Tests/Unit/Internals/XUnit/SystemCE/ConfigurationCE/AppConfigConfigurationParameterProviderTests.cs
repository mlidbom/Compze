using System;
using Compze.Tessaging.Hosting.Configuration;
using Compze.Tests.Infrastructure;
using FluentAssertions;
using Xunit;
using Compze.Tests.Infrastructure.XUnit;

namespace Compze.Tests.Unit.Internals.XUnit.SystemCE.ConfigurationCE;

public class AppConfigConfigurationParameterProviderTests: XUnitTestBase
{
   readonly IConfigurationParameterProvider _provider;
   
   public AppConfigConfigurationParameterProviderTests() => _provider = AppSettingsJsonConfigurationParameterProvider.Instance;

   [Fact] public void ParameterProvider_should_return_the_value_specified_in_the_configuration_file() =>
      _provider.GetString("KeyTest1").Should().Be("ValueTest1");

   [Fact] public void ParameterProvider_should_throw_ConfigurationErrorsException_when_key_does_not_exist() =>
      this.Invoking(_ => _provider.GetString("ErrorTest1")).Should().Throw<Exception>();

   [Fact] public void ParameterProvider_exception_should_contain_parameter_key() =>
      this.Invoking(_ => _provider.GetString("ErrorTest1"))
          .Should().Throw<Exception>()
          .And.Message.Should()
          .Contain("ErrorTest1");
}