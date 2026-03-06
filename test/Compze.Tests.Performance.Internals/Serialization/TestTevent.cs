using Compze.Abstractions.Public;
using Compze.Core.Tessaging.Teventive.Public;
using Compze.Core.Tessaging.Teventive.Public.Taggregates.Tevents.Public;
using JetBrains.Annotations;
using Newtonsoft.Json;

namespace Compze.Tests.Performance.Internals.Serialization;

public class TestTevent : TaggregateTevent
{
   [JsonConstructor]public TestTevent(string test1, string test2)
   {
      Test1 = test1;
      Test2 = test2;
   }

   public TestTevent(
      string test1,
      string test2,
      int taggregateVersion,
      TaggregateId taggregateId,
      DateTime utcTimeStamp):base(taggregateId)
   {
      Test1 = test1;
      Test2 = test2;

#pragma warning disable CS0618 // Type or member is obsolete
      ((IMutableTaggregateTevent)this).SetTaggregateVersionInternal(taggregateVersion);
      ((IMutableTaggregateTevent)this).SetUtcTimeStampInternal(utcTimeStamp);
#pragma warning restore CS0618 // Type or member is obsolete
   }

   // ReSharper disable once MemberCanBePrivate.Local
   public string Test1 { [UsedImplicitly] get; private set; }
   // ReSharper disable once MemberCanBePrivate.Local
   public string Test2 { [UsedImplicitly] get; private set; }
}
