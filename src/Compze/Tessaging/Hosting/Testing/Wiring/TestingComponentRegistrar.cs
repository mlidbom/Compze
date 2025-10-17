using System;
using System.Collections.Generic;
using Compze.Tessaging.Hosting.Sql.MicrosoftSql;
using Compze.Utilities.DependencyInjection;
using Compze.Utilities.DependencyInjection.Abstractions;
using Compze.Utilities.Testing.DbPool.MicrosoftSql;

namespace Compze.Tessaging.Hosting.Testing.Wiring;

class TestingComponentRegistrar : ComponentRegistrar
{
   readonly IDictionary<Type, object> _testingRegistrars;

   public TestingComponentRegistrar()
   {
      _testingRegistrars = new Dictionary<Type, object>()
      {
         {typeof(MsSqlSqlLayerRegistrar.ITestingRegistrar), new MsSqlDbPoolRegistrar(this)}
      };
   }

   public override TTestingRegistrar? TryGetTestingRegistrar<TTestingRegistrar>() where TTestingRegistrar : class
   {
      if(_testingRegistrars.TryGetValue(typeof(TTestingRegistrar), out var value))
      {
         return (TTestingRegistrar)value;
      }
      return null;
   }

   public override IRunMode RunMode => Utilities.DependencyInjection.RunMode.Testing;

   public override IComponentRegistrar Clone() => new TestingComponentRegistrar();
}


class MsSqlDbPoolRegistrar : MsSqlSqlLayerRegistrar.ITestingRegistrar
{
   readonly IComponentRegistrar _registrar;
   public MsSqlDbPoolRegistrar(IComponentRegistrar registrar) => _registrar = registrar;
   public IComponentRegistrar Register(string connectionStringName) => _registrar.MicrosoftSqlDbPoolAndConnectionPoolForConnectionStringName(connectionStringName);
}