using System;
using Compze.Tessaging.Teventive.EventStore.Abstractions;
using JetBrains.Annotations;
using Newtonsoft.Json;

namespace Compze.Tests.Performance.Internals.XUnit.Serialization;

public class TestEvent : AggregateEvent
{
   [JsonConstructor]public TestEvent(string test1, string test2)
   {
      Test1 = test1;
      Test2 = test2;
   }

   public TestEvent(
      string test1,
      string test2,
      int aggregateVersion,
      Guid aggregateId,
      DateTime utcTimeStamp):base(aggregateId)
   {
      Test1 = test1;
      Test2 = test2;

#pragma warning disable CS0618 // Type or member is obsolete
      ((IMutableAggregateEvent)this).SetAggregateVersionInternal(aggregateVersion);
      ((IMutableAggregateEvent)this).SetUtcTimeStampInternal(utcTimeStamp);
#pragma warning restore CS0618 // Type or member is obsolete
   }

   // ReSharper disable once MemberCanBePrivate.Local
   public string Test1 { [UsedImplicitly] get; private set; }
   // ReSharper disable once MemberCanBePrivate.Local
   public string Test2 { [UsedImplicitly] get; private set; }
}
