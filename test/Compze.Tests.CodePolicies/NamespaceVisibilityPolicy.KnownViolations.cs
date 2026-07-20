namespace Compze.Tests.CodePolicies;

public static partial class NamespaceVisibilityPolicy
{
   ///<summary>The acknowledged remaining violations of the namespace-visibility strategy, shrinking to zero:<br/>
   /// fixing a violation requires deleting its entry here, and nothing new may be added.</summary>
   static class KnownViolations
   {
      ///<summary>Full names of publicly visible types living in a namespace with an Internal or Private section —<br/>
      /// each needs a deliberate decision: internalize the type, or move it to a public home.</summary>
      public static readonly IReadOnlyList<string> PublicTypesInInternalOrPrivateNamespaces =
      [
         "Compze.Abstractions.Wiring.Testing.Internal.DIContainer",
         "Compze.Abstractions.Wiring.Testing.Internal.DIContainerExtensions",
         "Compze.Abstractions.Wiring.Testing.Internal.Serializer",
         "Compze.Abstractions.Wiring.Testing.Internal.SqlLayer",
         "Compze.Abstractions.Wiring.Testing.Internal.SqlLayerExtensions",
         "Compze.Abstractions.Wiring.Testing.Internal.Transport",
      ];

      ///<summary>Namespaces without an Internal or Private section that hold top-level internal types —<br/>
      /// each burns down by moving its internal types below the concept's Internal namespace.</summary>
      public static readonly IReadOnlyList<string> NamespacesWithInternalTopLevelTypesOutsideInternalOrPrivateSections =
      [
         "Compze.Abstractions",
         "Compze.DbPool",
         "Compze.DbPool.MachineWideState",
         "Compze.DbPool.MicrosoftSql",
         "Compze.DbPool.MySql",
         "Compze.DbPool.PostgreSql",
         "Compze.DbPool.Sqlite",
         "Compze.DbPool.SystemCE",
         "Compze.DependencyInjection",
         "Compze.DependencyInjection.DryIoc",
         "Compze.DependencyInjection.LightInject",
         "Compze.DependencyInjection.Microsoft",
         "Compze.DocumentDb.MicrosoftSql",
         "Compze.DocumentDb.MySql",
         "Compze.DocumentDb.PostgreSql",
         "Compze.DocumentDb.Sqlite",
         "Compze.Internals.Logging",
         "Compze.Internals.Serialization.Newtonsoft",
         "Compze.Internals.Sql.MicrosoftSql.Wiring",
         "Compze.Internals.Sql.MySql",
         "Compze.Internals.Sql.MySql.Wiring",
         "Compze.Internals.Sql.PostgreSql",
         "Compze.Internals.Sql.PostgreSql.Wiring",
         "Compze.Internals.Sql.Sqlite.Wiring",
         "Compze.Internals.SystemCE",
         "Compze.Internals.SystemCE.DiagnosticsCE",
         "Compze.Internals.SystemCE.ReactiveCE",
         "Compze.Internals.SystemCE.ThreadingCE.TasksCE",
         "Compze.Internals.SystemCE.TransactionsCE",
         "Compze.Internals.Testing.Performance",
         "Compze.InterprocessObject",
         "Compze.Must",
         "Compze.Must.Serialization",
         "Compze.Tessaging.MicrosoftSql",
         "Compze.Tessaging.MySql",
         "Compze.Tessaging.PostgreSql",
         "Compze.Tessaging.Sqlite",
         "Compze.Tessaging.Transport.AspNetCore",
         "Compze.Teventive",
         "Compze.Teventive.Infrastructure.EventDispatching",
         "Compze.Teventive.Taggregates.BaseClasses",
         "Compze.Teventive.TeventStore",
         "Compze.Teventive.TeventStore.MicrosoftSql",
         "Compze.Teventive.TeventStore.MySql",
         "Compze.Teventive.TeventStore.PostgreSql",
         "Compze.Teventive.TeventStore.Refactoring.Migrations",
         "Compze.Teventive.TeventStore.Sqlite",
         "Compze.Teventive.TeventStore.Typermedia",
         "Compze.Threading.Exceptions",
         "Compze.Threading.Interprocess",
         "Compze.Threading.Interprocess.Exceptions",
         "Compze.Threading.SystemCE",
         "Compze.Threading.Utilities",
         "Compze.TypeIdentifiers",
         "Compze.TypeIdentifiers.DependencyInjection",
         "Compze.TypeIdentifiers.Interning",
         "Compze.TypeIdentifiers.Interning.MicrosoftSql",
         "Compze.TypeIdentifiers.Interning.MySql",
         "Compze.TypeIdentifiers.Interning.PostgreSql",
         "Compze.TypeIdentifiers.Interning.Sqlite",
         "Compze.xUnit",
         "Compze.xUnitBDD",
         "Compze.xUnitMatrix"
      ];
   }
}
