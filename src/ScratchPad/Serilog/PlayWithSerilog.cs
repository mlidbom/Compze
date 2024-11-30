using System.Threading.Tasks;
using Composable.SystemCE;
using Composable.SystemCE.ThreadingCE.TasksCE;
using Composable.Testing;
using Composable.Testing.Logging.Serilog;
using JetBrains.Annotations;
using NUnit.Framework;
using Serilog;

namespace ScratchPad.Serilog;

class PlayWithSerilog : UniversalTestBase
{
   [Test] public async Task LogToSeq()
   {
      var logger = new LoggerConfiguration()
                  .Enrich.WithMachineName()
                  .Enrich.With<NUnitTestEnricher>()
                  .MinimumLevel.Debug()
                  .WriteTo.Seq("http://192.168.0.11:5341", period: 100.Milliseconds())
                  .CreateLogger();

      var log = logger.ForContext<PlayWithSerilog>();
      for(var age = 10; age < 20; age++)
      {
         log.Information("A deconstructed object: {@SomeThing}", new Something(age, $"Andrew{age}"));
         log.Information("Structured log entry: {Name} is {Age} years old", $"Andrew{age}", age);
      }

      log.Information("Val1: {val}, val2: {vop}", "vallo", "villo");

      await logger.DisposeAsync().CaF();
   }

   class Something(int age, string name)
   {
      [UsedImplicitly] public string Name { get; set; } = name;
      [UsedImplicitly] public int Age { get; set; } = age;
   }
}
