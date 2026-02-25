using System;
using System.Collections.Generic;
using Compze.Core.DocumentDb.Public;
using Compze.Contracts;
using Compze.Utilities.SystemCE;

namespace Compze.Core.DocumentDb.Private;

public partial class DocumentDbSession
{
   public class DocumentItem
   {
      readonly IDocumentDb _backingStore;
      readonly Dictionary<Type, Dictionary<string, string>> _persistentValues;
      DocumentKey Key { get; set; }

      public DocumentItem(DocumentKey key, IDocumentDb backingStore, Dictionary<Type, Dictionary<string, string>> persistentValues)
      {
         _backingStore = backingStore;
         _persistentValues = persistentValues;
         Key = key;
      }

      object? Document { get; set; }
      public bool IsDeleted { get; private set; }
      bool IsInBackingStore { get; set; }

      bool ScheduledForAdding => !IsInBackingStore && !IsDeleted && Document != null;
      bool ScheduledForRemoval => IsInBackingStore && IsDeleted;
      bool ScheduledForUpdate => IsInBackingStore && !IsDeleted;

      public void Delete() => IsDeleted = true;

      public void Save(object document)
      {
         Document = document._assertNotNull();
         IsDeleted = false;
      }

      public void DocumentLoadedFromBackingStore(object document)
      {
         Document = Document = document._assertNotNull();
         IsInBackingStore = true;
      }

      readonly ReentrancyGuard _reentrancyGuard = new();

      public void CommitChangesToBackingStore() => _reentrancyGuard.ExecuteIfNotReEntering(() =>
      {
         if(ScheduledForAdding)
         {
            IsInBackingStore = true;
            _backingStore.Add(Key.Id, Document, _persistentValues);
         } else if(ScheduledForRemoval)
         {
            var docType = Document!.GetType();
            Document = null;
            IsInBackingStore = false;
            _backingStore.Remove(Key.Id, docType);
         } else if(ScheduledForUpdate)
         {
            _backingStore.Update([new KeyValuePair<string, object>(Key.Id, Document!)], _persistentValues);
         }
      });
   }
}
