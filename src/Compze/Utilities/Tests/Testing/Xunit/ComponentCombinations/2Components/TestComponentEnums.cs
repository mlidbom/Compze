// ReSharper disable UnusedMember.Global
namespace Compze.Utilities.Tests.Testing.Xunit.ComponentCombinations._2Components;

/// <summary>
/// Serializer component dimension for pluggable components testing.
/// Each enum value corresponds to a component in the TestUsingPluggableComponentCombinations file.
/// </summary>
public enum Serializer
{
   Microsoft,
   Newtonsoft
}

/// <summary>
/// SQL layer component dimension for pluggable components testing.
/// Each enum value corresponds to a component in the TestUsingPluggableComponentCombinations file.
/// </summary>
public enum SqlLayer
{
   MsSql,
   Postgre,
   MySql
}

public enum DIContainer
{
   Microsoft,
   SimpleInjector
}

public enum TeventStore
{
   InMemory,
   SqlServer
}

public enum TessageBus
{
   InProcess,
   RabbitMQ
}
