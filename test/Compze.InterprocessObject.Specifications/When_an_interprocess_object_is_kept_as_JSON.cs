using System.Text.Json;
using System.Text.Json.Serialization;
using Compze.Must;
using Compze.Tests.Infrastructure;
using Compze.Threading.Interprocess.ResourceAccess;
using Compze.xUnitBDD;

namespace Compze.InterprocessObject.Specifications;

public class When_an_interprocess_object_is_kept_as_JSON : UniversalTestBase
{
   static DirectoryInfo TestDirectory => new DirectoryInfo(Path.Combine(Path.GetTempPath(), "Compze", "Tests", "SharedObjects"))._mutate(it => it.Create());
   readonly string _name = Guid.NewGuid().ToString();
   readonly List<Action> _deletions = [];

   protected override void DisposeInternal() => _deletions.ForEach(delete => delete());

   /// <summary>An instance of the interprocess object, as each process that shares it creates one of its own.</summary>
   IInterprocessObject<T> AnInstance<T>(JsonSerializerOptions? options = null) where T : class, new()
   {
      var created = IInterprocessObject.NewGlobal(_name, IInterprocessObjectSerializer.Json<T>(options), () => new T(), CorruptionAction.ThrowException, maxBytes: 4 * 1024, TestDirectory);
      _deletions.Add(created.Delete);
      return created;
   }

   public class Named
   {
      public string Name { get; set; } = "Default";
   }

   public class and_an_instance_updates_it : When_an_interprocess_object_is_kept_as_JSON
   {
      public and_an_instance_updates_it() => AnInstance<Named>().Update(it => it.Name = "Updated");

      [XF] public void another_instance_reads_the_update() => AnInstance<Named>().Read(it => it.Name).Must().Be("Updated");
   }

   public class WithANumber
   {
      public double Number { get; set; }
   }

   public class and_an_instance_whose_options_allow_NaN_updates_a_number_to_NaN : When_an_interprocess_object_is_kept_as_JSON
   {
      static readonly JsonSerializerOptions AllowingNaN = new() { NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals };

      public and_an_instance_whose_options_allow_NaN_updates_a_number_to_NaN() => AnInstance<WithANumber>(AllowingNaN).Update(it => it.Number = double.NaN);

      [XF] public void another_instance_with_the_same_options_reads_NaN() => AnInstance<WithANumber>(AllowingNaN).Read(it => it.Number).Must().Satisfy(double.IsNaN);
   }
}
