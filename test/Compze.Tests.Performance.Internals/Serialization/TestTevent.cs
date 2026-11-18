using Compze.Teventive.Taggregates.Tevents;
using JetBrains.Annotations;
using Newtonsoft.Json;

namespace Compze.Tests.Performance.Internals.Serialization;

///<summary>The tevent the serializer's performance specifications roundtrip. Its <see cref="TaggregateTevent"/> values are left as constructed:<br/>
/// the serializer leaves them out, so they make no difference to the work measured.</summary>
public class TestTevent : TaggregateTeventWithIdentity
{
   [JsonConstructor]public TestTevent(string test1, string test2)
   {
      Test1 = test1;
      Test2 = test2;
   }

   // ReSharper disable once MemberCanBePrivate.Local
   public string Test1 { [UsedImplicitly] get; private set; }
   // ReSharper disable once MemberCanBePrivate.Local
   public string Test2 { [UsedImplicitly] get; private set; }
}
