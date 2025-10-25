using System;
using System.Collections.Generic;
using System.Linq;
using Compze.Abstractions.Tessaging.Teventive.TeventStore.Public;
using Compze.Abstractions.Tessaging.Teventive.TeventStore.Refactoring.Migrations.Public;
using Compze.Abstractions.Tessaging.Teventive.Public;
using Compze.Sql.Common.TeventStore.Abstractions;
using Compze.Utilities.Contracts;
using Compze.Utilities.Functional;
using Compze.Utilities.SystemCE.CollectionsCE.GenericCE;
using Compze.Utilities.SystemCE.LinqCE;

#pragma warning disable CS0618 // Type or member is obsolete

namespace Compze.Tessaging.Teventive.TeventStore.Refactoring.Migrations;

//Yes this class has quite a bit of code that looks overly complex. Like it could be simplified a lot.
//What you are seeing is likely optimizations. Please do not change this class for reasons of readability unless you do thorough performance testing and verify that no performance is lost.
//The performance of this class is extremely important since it is called at least once for every tevent that is loaded from the tevent store when you have any migrations activated. It is called A LOT.
//This is one of those central classes for which optimization is actually vitally important.
//Each of the optimizations were done with the help of a profiler and running benchmarks on the tested performance improvements time and time again.
//Performance: Consider whether using the new stackalloc and Range types might allow us to improve performance of migrations.
class TeventModifier(Action<IReadOnlyList<TeventModifier.RefactoredTevent>> teventsAddedCallback) : ITeventModifier
{
   internal class RefactoredTevent(AggregateTevent newTevent, AggregateTeventStorageInformation storageInformation)
   {
      public AggregateTevent NewTevent { get; private set; } = newTevent;
      public AggregateTeventStorageInformation StorageInformation { get; private set; } = storageInformation;
   }

   readonly Action<IReadOnlyList<RefactoredTevent>> _teventsAddedCallback = teventsAddedCallback;
   internal LinkedList<AggregateTevent>? Tevents;
   RefactoredTevent[]? _replacementTevents;
   RefactoredTevent[]? _insertedTevents;

   AggregateTevent? _inspectedTevent;

   LinkedListNode<AggregateTevent>? _currentNode;
   AggregateTevent? _lastTeventInActualStream;

   LinkedListNode<AggregateTevent> CurrentNode
   {
      get
      {
         if (_currentNode == null)
         {
            Tevents = [];
            _currentNode = Tevents.AddFirst(_inspectedTevent!);
         }
         return _currentNode;
      }
      set
      {
         _currentNode = value;
         _inspectedTevent = _currentNode.Value;
      }
   }

   void AssertNoPriorModificationsHaveBeenMade()
   {
      if(_replacementTevents != null || _insertedTevents != null)
      {
         throw new Exception("You can only modify the current tevent once.");
      }

   }

   public void Replace(params AggregateTevent[] replacementTevents)
   {
      AssertNoPriorModificationsHaveBeenMade();
      if(_inspectedTevent is EndOfAggregateHistoryTeventPlaceHolder)
      {
         throw new Exception("You cannot call replace on the tevent that signifies the end of the stream");

      }

      _replacementTevents = replacementTevents.Select(@tevent => new RefactoredTevent(@tevent, new AggregateTeventStorageInformation())).ToArray();

      _replacementTevents.ForEach(
         (e, index) =>
         {
#pragma warning disable CS0618 // Type or member is obsolete
             ((IMutableAggregateTevent)e.NewTevent).SetAggregateVersionInternal(_inspectedTevent!.AggregateVersion + index);

             e.StorageInformation.RefactoringInformation = AggregateTeventRefactoringInformation.Replaces(_inspectedTevent.TessageId);
            e.StorageInformation.EffectiveVersion = _inspectedTevent.AggregateVersion + index;

            ((IMutableAggregateTevent)e.NewTevent).SetAggregateIdInternal(_inspectedTevent.AggregateId);
            ((IMutableAggregateTevent)e.NewTevent).SetUtcTimeStampInternal(_inspectedTevent.UtcTimeStamp);
         });
#pragma warning restore CS0618 // Type or member is obsolete

        CurrentNode = CurrentNode.Replace(replacementTevents);
      _teventsAddedCallback.Invoke(_replacementTevents);
   }

   public void Reset(AggregateTevent tevent)
   {
      if(tevent is EndOfAggregateHistoryTeventPlaceHolder && _inspectedTevent is not EndOfAggregateHistoryTeventPlaceHolder)
      {
         _lastTeventInActualStream = _inspectedTevent;
      }
      _inspectedTevent = tevent;
      Tevents = null;
      _currentNode = null;
      _insertedTevents = null;
      _replacementTevents = null;
   }

   public void MoveTo(LinkedListNode<AggregateTevent> current)
   {
      if (current.Value is EndOfAggregateHistoryTeventPlaceHolder && _inspectedTevent is not EndOfAggregateHistoryTeventPlaceHolder)
      {
         _lastTeventInActualStream = _inspectedTevent;
      }
      CurrentNode = current;
      _insertedTevents = null;
      _replacementTevents = null;
   }

   public void InsertBefore(params AggregateTevent[] insert)
   {
      AssertNoPriorModificationsHaveBeenMade();

      _insertedTevents = insert.Select(@tevent => new RefactoredTevent(@tevent, new AggregateTeventStorageInformation())).ToArray();

#pragma warning disable CS0618 // Type or member is obsolete
        if (_inspectedTevent is EndOfAggregateHistoryTeventPlaceHolder)
      {
         _insertedTevents.ForEach(
            (e, index) =>
            {
               ((IMutableAggregateTevent)e.NewTevent).SetAggregateVersionInternal(_inspectedTevent.AggregateVersion + index);

               e.StorageInformation.RefactoringInformation = AggregateTeventRefactoringInformation.InsertAfter(_lastTeventInActualStream!.TessageId);
               e.StorageInformation.EffectiveVersion = _inspectedTevent.AggregateVersion + index;

               ((IMutableAggregateTevent)e.NewTevent).SetAggregateIdInternal(_inspectedTevent.AggregateId);
               ((IMutableAggregateTevent)e.NewTevent).SetUtcTimeStampInternal(_lastTeventInActualStream.UtcTimeStamp);
            });
      }
      else
      {
         _insertedTevents.ForEach(
            (e, index) =>
            {
               ((IMutableAggregateTevent)e.NewTevent).SetAggregateVersionInternal(_inspectedTevent!.AggregateVersion + index);

               e.StorageInformation.RefactoringInformation = AggregateTeventRefactoringInformation.InsertBefore(_inspectedTevent.TessageId);
               e.StorageInformation.EffectiveVersion = _inspectedTevent.AggregateVersion + index;

               ((IMutableAggregateTevent)e.NewTevent).SetAggregateIdInternal(_inspectedTevent.AggregateId);
               ((IMutableAggregateTevent)e.NewTevent).SetUtcTimeStampInternal(_inspectedTevent.UtcTimeStamp);
            });
      }
      CurrentNode.ValuesFrom().ForEach((@tevent, _) => ((IMutableAggregateTevent)@tevent).SetAggregateVersionInternal(@tevent.AggregateVersion + _insertedTevents.Length));

      CurrentNode.AddBefore(insert);
      _teventsAddedCallback.Invoke(_insertedTevents);
#pragma warning restore CS0618 // Type or member is obsolete
    }

    internal AggregateTevent[] MutatedHistory => Tevents?.ToArray() ?? [Assert.Result.NotNull(_inspectedTevent).then(_inspectedTevent)];
}