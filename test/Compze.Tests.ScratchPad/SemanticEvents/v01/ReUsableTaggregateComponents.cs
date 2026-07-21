using Compze.Teventive.Taggregates.Tevents;

// ReSharper disable All
#pragma warning disable 414
#pragma warning disable IDE0059 // Unnecessary assignment of a value

namespace Compze.Tests.ScratchPad.SemanticEvents.v01;

interface ITaggregate1Tevent : ITaggregateTevent{}

interface ITaggregate1ComponentTevent<out TComponentTevent> : ITaggregateTevent{}

interface IComponentTeventBase{}

interface IComponentTevent1 : IComponentTeventBase
{
}

interface IComponentTevent2 : IComponentTevent1
{
}

public class ReUsableTaggregateComponents
{
#pragma warning disable IDE0051 // Remove unused private members
   static void DemonstrateSemanticRelationships()
   {
      ITaggregate1ComponentTevent<IComponentTeventBase> wceb = null!;
      ITaggregate1ComponentTevent<IComponentTevent1> wce1 = null!;
      ITaggregate1ComponentTevent<IComponentTevent2> wce2 = null!;

      //Semantic relationship is maintained.
      //For registering handlers we could enable registering via the wrapped type so that handlers need not always do the unwrapping.
      //It would be possible to listen to all component tevents, regardless of the owning taggregate type, or to zoom in on specific taggregate's component tevents.
      wceb = wce1 = wce2;

   }
}
