namespace Compze.Abstractions.Configuration.Internal;

///<summary>Allows for reading configuration parameters from a source such as a configuration file</summary>
public interface IConfigurationParameterProvider
{
   ///<summary>Gets a string configuration value.</summary>
   string GetString(string parameterName, string? valueIfMissing = null);
}