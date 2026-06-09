// ReSharper disable UnusedMember.Global
namespace Compze.xUnitMatrix.Tests._2Components;

#pragma warning disable CA1724 //I don't care that a namespace somewhere has the same name as one of these types

/// <summary>
/// Example Serializer dimension for the matrix tests.
/// Each enum value is one dimension value, matched by name against the TestUsingPluggableComponentCombinations file.
/// </summary>
enum Serializer
{
   Microsoft,
   Newtonsoft
}

/// <summary>
/// Example SQL layer dimension for the matrix tests.
/// Each enum value is one dimension value, matched by name against the TestUsingPluggableComponentCombinations file.
/// </summary>
enum SqlLayer
{
   MsSql,
   Postgre,
   MySql
}

enum DIContainer
{
   Microsoft,
   Autofac,
   DryIoc
}

enum TeventStore
{
   InMemory,
   SqlServer
}

enum TessageBus
{
   InProcess,
   RabbitMQ
}

enum Transport
{
   AspNetCore,
   InMemory
}
