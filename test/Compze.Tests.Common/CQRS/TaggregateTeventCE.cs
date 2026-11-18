using System.Reflection;
using Compze.Abstractions;
using Compze.Teventive.Taggregates.Tevents;

namespace Compze.Tests.Common.CQRS;

///<summary>Gives a taggregate tevent the values its taggregate gives it when it publishes the tevent, or a tevent store when it reads<br/>
/// the tevent back: for the specifications that build tevents without a taggregate, such as the tevent store's and the migrations'.</summary>
///<remarks>review: by reflection on the properties' private setters, since what Compze sets them through is internal to Compze, and<br/>
/// these specification projects see none of Compze's internals. Specifications that published their tevents on a taggregate would<br/>
/// need none of this (jastudio-rep-opus's fork, 2026-11-18, end review Q10).</remarks>
public static class TaggregateTeventCE
{
   extension(TaggregateTevent @this)
   {
      public void SetTaggregateId(TaggregateId taggregateId) => SetPrivately(@this, nameof(TaggregateTevent.TaggregateId), taggregateId);

      public void SetTaggregateVersion(int taggregateVersion) => SetPrivately(@this, nameof(TaggregateTevent.TaggregateVersion), taggregateVersion);

      public void SetUtcTimeStamp(DateTime utcTimeStamp) => SetPrivately(@this, nameof(TaggregateTevent.UtcTimeStamp), utcTimeStamp);
   }

   static void SetPrivately(TaggregateTevent tevent, string property, object value) =>
      typeof(TaggregateTevent).GetProperty(property, BindingFlags.Instance | BindingFlags.Public)!.GetSetMethod(nonPublic: true)!.Invoke(tevent, [value]);
}
