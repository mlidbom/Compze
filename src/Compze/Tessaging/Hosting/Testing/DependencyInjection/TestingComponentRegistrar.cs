using System;
using System.Collections.Generic;
using Compze.Sql.MicrosoftSql;
using Compze.Utilities.DependencyInjection;
using Compze.Utilities.DependencyInjection.Abstractions;

namespace Compze.Tessaging.Hosting.Testing.DependencyInjection;

class TestingComponentRegistrar(IDependencyInjectionContainer container) : ComponentRegistrar(container)
{
   readonly IDictionary<Type, object> _testingRegistrars= new Dictionary<Type, object>()
                                                                 {
                                                                    {typeof(IMsSqlConnectionPool), "staoeusth"}
                                                                 };

   public override TTestingRegistrar? TryGetTestingRegistrar<TTestingRegistrar>() where TTestingRegistrar : class  => null;
}
