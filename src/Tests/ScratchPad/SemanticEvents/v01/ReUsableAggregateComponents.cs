using Compze.Abstractions.Tessaging.Teventive.TeventStore.Public;
using Compze.Abstractions.Tessaging.Teventive.Public;

// ReSharper disable All
#pragma warning disable 414
#pragma warning disable IDE0059 // Unnecessary assignment of a value

namespace Compze.Tests.ScratchPad.SemanticTevents.v01;

//todo: Implement the ability to use this pattern in the aggregate root and ensure that routing on the bus also work correctly.
interface IAggregate1Tevent : IAggregateTevent{}

interface IAggregate1ComponentTevent<out TComponentTevent> : IAggregateTevent{}

interface IComponentTeventBase{}

interface IComponentTevent1 : IComponentTeventBase
{
}

interface IComponentTevent2 : IComponentTevent1
{
}

public class ReUsableAggregateComponents
{
#pragma warning disable IDE0051 // Remove unused private members
   static void DemonstrateSemanticRelationships()
   {
      IAggregate1ComponentTevent<IComponentTeventBase> wceb = null!;
      IAggregate1ComponentTevent<IComponentTevent1> wce1 = null!;
      IAggregate1ComponentTevent<IComponentTevent2> wce2 = null!;

      //Semantic relationship is maintained.
      //For registering handlers we could enable registering via the wrapped type so that handlers need not always do the unwrapping.
      //It would be possible to listen to all component tevents, regardless of the owning aggregate type, or to zoom in on specific aggregate's component tevents.
      wceb = wce1 = wce2;

   }
}