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
using Compze.Internals.Transport.NamedPipes;
using Compze.Tessaging.Abstractions.Tessaging.Hosting.TessageHandling.Registration.Public;
using Compze.Tessaging.Hosting;
using Compze.Internals.Sql.Sqlite.Wiring;
using Compze.Tessaging.Sqlite.Wiring;

namespace Compze.Tests.SameMachine.EndpointHostProcess;

///<summary>A standalone process hosting one Tessaging endpoint over named pipes, discovered through an<br/>
/// <see cref="InterprocessEndpointRegistry"/> — the counterpart process the multi-process specifications converse with. Which<br/>
/// Tessaging the endpoint speaks is the launching specification's choice, passed as the composition argument:<br/>
/// <see cref="DistributedTessagingComposition"/> handles <see cref="TommandSentToTheEndpointHostProcess"/> by sending<br/>
/// <see cref="TommandSentBackToTheSpecificationProcess"/> — proving discovery, exactly-once delivery, and reply across real process<br/>
/// boundaries with no web stack and no database server; <see cref="TransientTessagingComposition"/> handles<br/>
/// <see cref="ITransientTeventPublishedByTheSpecificationProcess"/> by publishing<br/>
/// <see cref="TransientTeventPublishedByTheEndpointHostProcess"/> — the same conversation guarantee-free, with no database anywhere.</summary>
public static class Program
{
   ///<summary>The composition argument selecting the full distributed Tessaging pipeline on a sqlite database — the exactly-once tommand conversation.</summary>
   public const string DistributedTessagingComposition = "DistributedTessaging";

   ///<summary>The composition argument selecting guarantee-free transient Tessaging on a database-less foundation — the transient tevent conversation.</summary>
   public const string TransientTessagingComposition = "TransientTessaging";

   public static async Task<int> Main(string[] args)
   {
      MakeNCrunchInstrumentedDependenciesLoadable();

      var registryName = args[0];
      var workDirectory = new DirectoryInfo(args[1]); //Holds the registry's backing file and, for the distributed composition, this process's sqlite database files.
      var specificationProcessId = int.Parse(args[2], CultureInfo.InvariantCulture);
      var composition = args[3];

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

               switch(composition)
               {
                  case DistributedTessagingComposition:
                     ComposeDistributedTessagingOnASqliteDatabase(builder, registry, workDirectory);
                     break;
                  case TransientTessagingComposition:
                     ComposeTransientTessagingWithNoDatabase(builder, registry);
                     break;
                  default:
                     throw new ArgumentOutOfRangeException(nameof(args), composition, $"Unknown composition argument. Pass {DistributedTessagingComposition} or {TransientTessagingComposition}.");
               }
            });

         await host.StartAsync();

         //The specification process owns this process's lifetime: it exits (or is killed) when the specifications are done,
         //and we follow — so a crashed test run leaves no orphaned endpoint host processes behind.
         using var specificationProcess = Process.GetProcessById(specificationProcessId);
         await specificationProcess.WaitForExitAsync();
      }

      return 0;
   }

   static void ComposeDistributedTessagingOnASqliteDatabase(IEndpointBuilder builder, InterprocessEndpointRegistry registry, DirectoryInfo workDirectory)
   {
      builder.Registrar.Register(Singleton.For<IConfigurationParameterProvider>()
                                          .CreatedBy(() => new SqliteDatabasePerConnectionStringNameConfigurationParameterProvider(workDirectory)));

      builder.ComposeEndpoint(it => it.NamedPipeEndpointTransport()
                                      .SqliteEndpointDatabase("EndpointHostProcess"))
             .AddDistributedTessaging(tessaging => tessaging.NewtonsoftSerializer())
             .ParticipateIn(registry)
             .RegisterHandlers(register => register.ForTommand<TommandSentToTheEndpointHostProcess, IServiceBusSession>((_, serviceBusSession) => serviceBusSession.Send(new TommandSentBackToTheSpecificationProcess())));
   }

   ///<summary>The guarantee-free composition: no database declaration, no configuration, nothing persisted anywhere in this<br/>
   /// process — the transient tier and participation are all the delivery there is.</summary>
   static void ComposeTransientTessagingWithNoDatabase(IEndpointBuilder builder, InterprocessEndpointRegistry registry) =>
      builder.ComposeEndpoint(it => it.NamedPipeEndpointTransport())
             .AddTransientTessaging(tessaging => tessaging.NewtonsoftSerializer())
             .ParticipateIn(registry)
             .RegisterHandlers(register => register.ForTevent((ITransientTeventPublishedByTheSpecificationProcess _, ITeventPublisher teventPublisher) =>
                                                                 teventPublisher.Publish(new TransientTeventPublishedByTheEndpointHostProcess())));

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
