using System.Threading.Tasks;
using Composable.SystemCE;
using Composable.SystemCE.ThreadingCE.TasksCE;
using Composable.Testing;
using Composable.Testing.Logging.Serilog;
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
      for(var i = 1; i < 100; i++)
      {
         log.Information("Another message, world! {@SomeThing}", new Something() { Age = i });
      }

      log.Information("Val1: {val}, val2: {vop}", "vallo", "villo");

      await logger.DisposeAsync().CaF();
   }

   class Something
   {
      public string Name { get; set; } = "Andrew";
      public int Age { get; set; } = 60;
   }
}
