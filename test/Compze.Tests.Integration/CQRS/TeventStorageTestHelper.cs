using Compze.Teventive;
using Compze.Teventive.Taggregates.Tevents;
using Compze.Tests.Common.CQRS;

namespace Compze.Tests.Integration.CQRS;

static class TeventStorageTestHelper
{
   //Not all storage providers stores with more than 6 decimal points precision
   internal static void StripSteventhDecimalPointFromSecondFractionOnUtcUpdateTime(IReadOnlyList<ITaggregateTevent> tevents)
       => tevents.Cast<TaggregateTevent>().ForEach(tevent => tevent.SetUtcTimeStamp(tevent.UtcTimeStamp.AddTicks(-(tevent.UtcTimeStamp.Ticks % 10))));
}
