using System.Reflection;

namespace Compze.Tests.CodePolicies.Infrastructure;

static class CompzeAssemblyLoader
{
   public static void EnsureAllCompzeAssembliesAreLoaded()
   {
      var binDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
      var allDllFiles = Directory.GetFiles(binDirectory, "Compze.*.dll", SearchOption.TopDirectoryOnly);

      foreach(var dllPath in allDllFiles)
      {
         var assemblyName = AssemblyName.GetAssemblyName(dllPath);
         if(AppDomain.CurrentDomain.GetAssemblies().All(loadedAssembly => loadedAssembly.FullName != assemblyName.FullName))
         {
            Assembly.LoadFrom(dllPath);
         }
      }
   }
}
