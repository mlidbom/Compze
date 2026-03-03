using Compze.Core.Configuration.Internal;
using Compze.Utilities.DependencyInjection;
using Compze.Utilities.DependencyInjection.Abstractions;
using Microsoft.Extensions.Configuration;

namespace Compze.Tessaging.Configuration
{
   public static class AppSettingsJsonConfigurationParameterProviderRegistrar
   {
      public static IComponentRegistrar JSonAppConfigFileConfigurationParameterProvider(this IComponentRegistrar @this)
         => @this.Register(AppSettingsJsonConfigurationParameterProvider.RegisterWith);
   }


   ///<summary>Fetches configuration variables from the application configuration file.</summary>
   public class AppSettingsJsonConfigurationParameterProvider : IConfigurationParameterProvider, IStaticInstancePropertySingleton<IConfigurationParameterProvider>
   {
      internal static void RegisterWith(IComponentRegistrar registrar)
         => registrar.Register(Singleton.For<IConfigurationParameterProvider>()
                                        .CreatedBy(() => new AppSettingsJsonConfigurationParameterProvider()));

      AppSettingsJsonConfigurationParameterProvider(){}

      public static IConfigurationParameterProvider Instance { get; } = new AppSettingsJsonConfigurationParameterProvider();

      static readonly LazyCE<IConfigurationSection> AppSettingsSectionInitializer = new(() => new ConfigurationBuilder()
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
}
