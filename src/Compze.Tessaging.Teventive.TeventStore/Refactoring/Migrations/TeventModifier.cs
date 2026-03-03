using Compze.Core.Tessaging.Teventive.Public;
using Compze.Core.Tessaging.Teventive.Public.Taggregates.Tevents.Public;
using Compze.Core.Tessaging.Teventive.TeventStore.Internal.SqlLayer.Abstractions;
using Compze.Core.Tessaging.Teventive.TeventStore.Refactoring.Migrations.Public;
using Compze.Contracts;
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
public class TeventModifier(Action<IReadOnlyList<TeventModifier.RefactoredTevent>> teventsAddedCallback) : ITeventModifier
{
   public class RefactoredTevent(TaggregateTevent newTevent, TaggregateTeventStorageInformation storageInformation)
   {
      internal TaggregateTevent NewTevent { get; private set; } = newTevent;
      internal TaggregateTeventStorageInformation StorageInformation { get; private set; } = storageInformation;
   }

   readonly Action<IReadOnlyList<RefactoredTevent>> _teventsAddedCallback = teventsAddedCallback;
   internal LinkedList<TaggregateTevent>? Tevents;
   RefactoredTevent[]? _replacementTevents;
   RefactoredTevent[]? _insertedTevents;

   TaggregateTevent? _inspectedTevent;

   LinkedListNode<TaggregateTevent>? _currentNode;
   TaggregateTevent? _lastTeventInActualStream;

   LinkedListNode<TaggregateTevent> CurrentNode
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

   public void Replace(params TaggregateTevent[] tevents)
   {
      AssertNoPriorModificationsHaveBeenMade();
      if(_inspectedTevent is EndOfTaggregateHistoryTeventPlaceHolder)
      {
         throw new Exception("You cannot call replace on the tevent that signifies the end of the stream");

      }

      _replacementTevents = tevents.Select(tevent => new RefactoredTevent(tevent, new TaggregateTeventStorageInformation())).ToArray();

      _replacementTevents.ForEach(
         (e, index) =>
         {
#pragma warning disable CS0618 // Type or member is obsolete
             ((IMutableTaggregateTevent)e.NewTevent).SetTaggregateVersionInternal(_inspectedTevent!.TaggregateVersion + index);

             e.StorageInformation.RefactoringInformation = TaggregateTeventRefactoringInformation.Replaces(_inspectedTevent.Id);
            e.StorageInformation.EffectiveVersion = _inspectedTevent.TaggregateVersion + index;

            ((IMutableTaggregateTevent)e.NewTevent).SetTaggregateIdInternal(_inspectedTevent.TaggregateId);
            ((IMutableTaggregateTevent)e.NewTevent).SetUtcTimeStampInternal(_inspectedTevent.UtcTimeStamp);
         });
#pragma warning restore CS0618 // Type or member is obsolete

        CurrentNode = CurrentNode.Replace(tevents);
      _teventsAddedCallback.Invoke(_replacementTevents);
   }

   public void Reset(TaggregateTevent tevent)
   {
      if(tevent is EndOfTaggregateHistoryTeventPlaceHolder && _inspectedTevent is not EndOfTaggregateHistoryTeventPlaceHolder)
      {
         _lastTeventInActualStream = _inspectedTevent;
      }
      _inspectedTevent = tevent;
      Tevents = null;
      _currentNode = null;
      _insertedTevents = null;
      _replacementTevents = null;
   }

   public void MoveTo(LinkedListNode<TaggregateTevent> current)
   {
      if (current.Value is EndOfTaggregateHistoryTeventPlaceHolder && _inspectedTevent is not EndOfTaggregateHistoryTeventPlaceHolder)
      {
         _lastTeventInActualStream = _inspectedTevent;
      }
      CurrentNode = current;
      _insertedTevents = null;
      _replacementTevents = null;
   }

   public void InsertBefore(params TaggregateTevent[] insert)
   {
      AssertNoPriorModificationsHaveBeenMade();

      _insertedTevents = insert.Select(tevent => new RefactoredTevent(tevent, new TaggregateTeventStorageInformation())).ToArray();

#pragma warning disable CS0618 // Type or member is obsolete
        if (_inspectedTevent is EndOfTaggregateHistoryTeventPlaceHolder)
      {
         _insertedTevents.ForEach(
            (e, index) =>
            {
               ((IMutableTaggregateTevent)e.NewTevent).SetTaggregateVersionInternal(_inspectedTevent.TaggregateVersion + index);

               e.StorageInformation.RefactoringInformation = TaggregateTeventRefactoringInformation.InsertAfter(_lastTeventInActualStream!.Id);
               e.StorageInformation.EffectiveVersion = _inspectedTevent.TaggregateVersion + index;

               ((IMutableTaggregateTevent)e.NewTevent).SetTaggregateIdInternal(_inspectedTevent.TaggregateId);
               ((IMutableTaggregateTevent)e.NewTevent).SetUtcTimeStampInternal(_lastTeventInActualStream.UtcTimeStamp);
            });
      }
      else
      {
         _insertedTevents.ForEach(
            (e, index) =>
            {
               ((IMutableTaggregateTevent)e.NewTevent).SetTaggregateVersionInternal(_inspectedTevent!.TaggregateVersion + index);

               e.StorageInformation.RefactoringInformation = TaggregateTeventRefactoringInformation.InsertBefore(_inspectedTevent.Id);
               e.StorageInformation.EffectiveVersion = _inspectedTevent.TaggregateVersion + index;

               ((IMutableTaggregateTevent)e.NewTevent).SetTaggregateIdInternal(_inspectedTevent.TaggregateId);
               ((IMutableTaggregateTevent)e.NewTevent).SetUtcTimeStampInternal(_inspectedTevent.UtcTimeStamp);
            });
      }
      CurrentNode.ValuesFrom().ForEach((tevent, _) => ((IMutableTaggregateTevent)tevent).SetTaggregateVersionInternal(tevent.TaggregateVersion + _insertedTevents.Length));

      CurrentNode.AddBefore(insert);
      _teventsAddedCallback.Invoke(_insertedTevents);
#pragma warning restore CS0618 // Type or member is obsolete
    }

#pragma warning disable CA1819 // Array property needed for migration tevent history
    public TaggregateTevent[] MutatedHistory => Tevents?.ToArray() ?? [_inspectedTevent._assert().NotNull()];
#pragma warning restore CA1819
}