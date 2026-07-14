using System.Diagnostics;
using System.Globalization;
using System.Runtime.Loader;
using Compze.Abstractions.Configuration.Internal;
using Compze.Abstractions.Hosting.Public;
using Compze.Abstractions.Tessaging.Public;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Microsoft;
using Compze.Hosting;
using Compze.Hosting.SameMachine;
using Compze.Internals.Serialization.Newtonsoft.Wiring;
using Compze.Tessaging.Abstractions.Tessaging.Hosting.TessageHandling.Registration.Public;
using Compze.Tessaging.Hosting;
using Compze.Tessaging.Sqlite.Wiring;
using Compze.Tessaging.Transport.NamedPipes;
using Compze.TypeIdentifiers.Interning.Sqlite.Wiring;

namespace Compze.Tests.SameMachine.EndpointHostProcess;

///<summary>A standalone process hosting one distributed-Tessaging endpoint over named pipes, discovered through an<br/>
/// <see cref="InterprocessEndpointRegistry"/> — the counterpart process the multi-process specifications converse with.<br/>
/// It handles <see cref="TommandSentToTheEndpointHostProcess"/> by sending <see cref="TommandSentBackToTheSpecificationProcess"/>,<br/>
/// proving both directions of a cross-process conversation: discovery, exactly-once delivery, and reply — with no web stack and no<br/>
/// database server, exactly the composition a same-machine application-suite process uses.</summary>
public static class Program
{
   public static async Task<int> Main(string[] args)
   {
      MakeNCrunchInstrumentedDependenciesLoadable();

      var registryName = args[0];
      var workDirectory = new DirectoryInfo(args[1]); //Holds the registry's backing file and this process's sqlite database files.
      var specificationProcessId = int.Parse(args[2], CultureInfo.InvariantCulture);

      using var registry = InterprocessEndpointRegistry.OpenOrCreateSessionLocal(registryName, workDirectory);
      var host = EndpointHost.Production.Create(() => new MicrosoftContainerBuilder(new ComponentRegistrar()));
      await using(host)
      {
         host.RegisterEndpoint(
            "EndpointHostProcess",
            new EndpointId(Guid.Parse("70b8f9be-6a66-4f8d-bd55-0c05dbcbb2c0")),
            builder =>
            {
               builder.TypeMapper.MapTypesFromAssemblyContaining<TommandSentToTheEndpointHostProcess>();

               builder.Registrar
                      .Register(Singleton.For<IConfigurationParameterProvider>().CreatedBy(() => new SqliteDatabasePerConnectionStringNameConfigurationParameterProvider(workDirectory)))
                      .NewtonsoftSerializers()
                      .NamedPipeTessagingTransport()
                      .SqliteEndpointPersistence("EndpointHostProcess")
                      .SqliteTessagingSqlLayer();

               builder.AddDistributedTessaging().ParticipateIn(registry);

               builder.RegisterTessagingHandlers.ForTommand<TommandSentToTheEndpointHostProcess, IServiceBusSession>(
                  (_, serviceBusSession) => serviceBusSession.Send(new TommandSentBackToTheSpecificationProcess()));
            });

         await host.StartAsync();

         //The specification process owns this process's lifetime: it exits (or is killed) when the specifications are done,
         //and we follow — so a crashed test run leaves no orphaned endpoint host processes behind.
         using var specificationProcess = Process.GetProcessById(specificationProcessId);
         await specificationProcess.WaitForExitAsync();
      }

      return 0;
   }

   ///<summary>The name of the environment variable through which the specification that launches this process passes the directory<br/>
   /// holding the NCrunch runtime assemblies — its own base directory. Set only when the specification runs under NCrunch.</summary>
   public const string NCrunchRuntimeAssemblyDirectoryVariableName = "COMPZE_NCRUNCH_RUNTIME_ASSEMBLY_DIRECTORY";

   ///<summary>NCrunch coverage-instruments the assemblies it builds, making their code call into NCrunch runtime assemblies — which are<br/>
   /// not part of this process's dependency closure, so the first instrumented dependency to execute would die failing to load them.<br/>
   /// The specification that launches this process passes the directory holding those assemblies through<br/>
   /// <see cref="NCrunchRuntimeAssemblyDirectoryVariableName"/>; outside NCrunch the variable is absent and this method installs nothing.</summary>
   static void MakeNCrunchInstrumentedDependenciesLoadable()
   {
      var nCrunchRuntimeAssemblyDirectory = Environment.GetEnvironmentVariable(NCrunchRuntimeAssemblyDirectoryVariableName);
      if(nCrunchRuntimeAssemblyDirectory == null) return;
      AssemblyLoadContext.Default.Resolving += (loadContext, assemblyName) =>
      {
         var candidate = Path.Combine(nCrunchRuntimeAssemblyDirectory, assemblyName.Name + ".dll");
         return File.Exists(candidate) ? loadContext.LoadFromAssemblyPath(candidate) : null;
      };
   }

   ///<summary>Serves every requested connection string as a sqlite database file named after it in this process's work directory —<br/>
   /// connection strings are the only configuration this composition reads.</summary>
   class SqliteDatabasePerConnectionStringNameConfigurationParameterProvider(DirectoryInfo workDirectory) : IConfigurationParameterProvider
   {
      readonly DirectoryInfo _workDirectory = workDirectory;

      public string GetString(string parameterName, string? valueIfMissing = null) =>
         $"Data Source={Path.Combine(_workDirectory.FullName, parameterName + ".sqlite")}";
   }
}
