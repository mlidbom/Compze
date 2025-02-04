﻿using System;
using System.IO;
using Microsoft.Extensions.Configuration;

namespace Compze.SystemCE.ConfigurationCE;

///<summary>Fetches configuration variables from the application configuration file.</summary>
class AppSettingsJsonConfigurationParameterProvider : IConfigurationParameterProvider, IStaticInstancePropertySingleton
{
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