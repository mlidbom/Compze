using Compze.Utilities.DependencyInjection;
using Compze.Utilities.DependencyInjection.Abstractions;
using Compze.Utilities.SystemCE;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;

namespace Compze.Tessaging.Hosting.Configuration;

static class AppSettingsJsonConfigurationParameterProviderRegistrar
{
   public static IDependencyRegistrar JSonAppConfigFileConfigurationParameterProvider(this IDependencyRegistrar @this)
      => @this.Register(AppSettingsJsonConfigurationParameterProvider.RegisterWith);
}


///<summary>Fetches configuration variables from the application configuration file.</summary>
class AppSettingsJsonConfigurationParameterProvider : IConfigurationParameterProvider, IStaticInstancePropertySingleton
{
   internal static void RegisterWith(IDependencyRegistrar registrar)
      => registrar.Register(Singleton.For<IConfigurationParameterProvider>()
                                     .CreatedBy(() => new AppSettingsJsonConfigurationParameterProvider()));

   AppSettingsJsonConfigurationParameterProvider(){}

   public static readonly IConfigurationParameterProvider Instance = new AppSettingsJsonConfigurationParameterProvider();

   static readonly OptimizedLazy<IConfigurationSection> AppSettingsSectionInitializer = new(() => new ConfigurationBuilder()
                                                                                                 .SetBasePath(Directory.GetCurrentDirectory())
                                                                                                 .AddJsonFile("appsettings.json", false, true)
                                                                                                 .AddJsonFile("appsettings-testing.json", true, true)
                                                                                                 .Build()
                                                                                                 .GetSection("appSettings"));

   static IConfigurationSection AppSettingsSection => AppSettingsSectionInitializer.Value;

   public string GetString(string parameterName, string? valueIfMissing = null)
   {
      var parameter = AppSettingsSection[parameterName];
      if(parameter != null) return parameter;
      if(valueIfMissing != null)
      {
         return valueIfMissing;
      }

      throw new Exception($"ApplicationSettings Parameter {parameterName} does not exists");
   }
}
