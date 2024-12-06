using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Compze.SystemCE.CollectionsCE.GenericCE;

namespace Compze.Persistence.DocumentDb;

interface IDocumentDbPersistenceLayer
{
   void Update(IReadOnlyList<WriteRow> toUpdate);
   bool TryGet(string idString, IReadOnlySet<Guid> acceptableTypeIds, bool useUpdateLock, [NotNullWhen(true)] out ReadRow? document);
   void Add(WriteRow row);
   int Remove(string idString, IReadOnlySet<Guid> acceptableTypes);
   //Urgent: This whole Guid vs string thing must be fixed.
   IEnumerable<Guid> GetAllIds(IReadOnlySet<Guid> acceptableTypes);
   IReadOnlyList<ReadRow> GetAll(IEnumerable<Guid> ids, IReadOnlySet<Guid> acceptableTypes);
   IReadOnlyList<ReadRow> GetAll(IReadOnlySet<Guid> acceptableTypes);

   class ReadRow(Guid typeId, string serializedDocument)
   {
      public Guid TypeId { get; } = typeId;

      public string SerializedDocument { get; } = serializedDocument;
   }

   class WriteRow(string id, string serializedDocument, DateTime updateTime, Guid typeId)
   {
      public string Id { get; } = id;
      public string SerializedDocument { get; } = serializedDocument;
      public DateTime UpdateTime { get; } = updateTime;
      public Guid TypeId { get; } = typeId;
   }

   internal static class DocumentTableSchemaStrings
   {
      internal const string TableName = "Store";
      internal const string Id = nameof(Id);
      internal const string ValueTypeId = nameof(ValueTypeId);
      internal const string Created = nameof(Created);
      internal const string Updated = nameof(Updated);
      internal const string Value = nameof(Value);
   }
}