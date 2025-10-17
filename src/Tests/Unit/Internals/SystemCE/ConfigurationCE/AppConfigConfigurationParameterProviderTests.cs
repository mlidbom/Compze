using System;
using Compze.Common.Configuration;
using FluentAssertions;
using Compze.Tests.Infrastructure.XUnit;
using Compze.Tests.Infrastructure.XUnit.TestFrameworkExtensions;

namespace Compze.Tests.Unit.Internals.SystemCE.ConfigurationCE;

public class AppConfigConfigurationParameterProviderTests: XUnitTestBase
{
   readonly IConfigurationParameterProvider _provider;
   
   public AppConfigConfigurationParameterProviderTests() => _provider = AppSettingsJsonConfigurationParameterProvider.Instance;

   [XF] public void ParameterProvider_should_return_the_value_specified_in_the_configuration_file() =>
      _provider.GetString("KeyTest1").Should().Be("ValueTest1");

   [XF] public void ParameterProvider_should_throw_ConfigurationErrorsException_when_key_does_not_exist() =>
      this.Invoking(_ => _provider.GetString("ErrorTest1")).Should().Throw<Exception>();

   [XF] public void ParameterProvider_exception_should_contain_parameter_key() =>
      this.Invoking(_ => _provider.GetString("ErrorTest1"))
          .Should().Throw<Exception>()
          .And.Message.Should()
          .Contain("ErrorTest1");
}