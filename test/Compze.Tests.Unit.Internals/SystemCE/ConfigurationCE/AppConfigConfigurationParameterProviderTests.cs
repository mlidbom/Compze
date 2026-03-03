using Compze.Core.Configuration.Internal;
using Compze.Tessaging.Configuration;
using Compze.Tests.Infrastructure;
using Compze.Utilities.Testing.Must;
using Compze.Utilities.Testing.XUnit.BDD;
using static Compze.Utilities.Testing.Must.MustActions;

namespace Compze.Tests.Unit.Internals.SystemCE.ConfigurationCE;

public class AppConfigConfigurationParameterProviderTests: UniversalTestBase
{
   readonly IConfigurationParameterProvider _provider = AppSettingsJsonConfigurationParameterProvider.Instance;

   [XF] public void ParameterProvider_should_return_the_value_specified_in_the_configuration_file() =>
      _provider.GetString("KeyTest1").Must().Be("ValueTest1");

   [XF] public void ParameterProvider_should_throw_ConfigurationErrorsException_when_key_does_not_exist() =>
      Invoking(() => _provider.GetString("ErrorTest1")).Must().Throw<Exception>();

   [XF] public void ParameterProvider_exception_should_contain_parameter_key() =>
      Invoking(() => _provider.GetString("ErrorTest1"))
          .Must().Throw<Exception>()
          .Which.Message.Must()
          .Contain("ErrorTest1");
}
