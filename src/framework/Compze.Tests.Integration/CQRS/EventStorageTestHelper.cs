﻿using System.Collections.Generic;
using System.Linq;
using Compze.Persistence.EventStore;
using Compze.SystemCE.LinqCE;

namespace Compze.Tests.Integration.CQRS;

static class EventStorageTestHelper
{
   //Not all storage providers stores with more than 6 decimal points precision
   internal static void StripSeventhDecimalPointFromSecondFractionOnUtcUpdateTime(IReadOnlyList<IAggregateEvent> events)
      => events.Cast<IMutableAggregateEvent>().ForEach(@event => @event.SetUtcTimeStamp(@event.UtcTimeStamp.AddTicks(-(@event.UtcTimeStamp.Ticks % 10))));
}