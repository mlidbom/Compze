using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Compze.Core.Refactoring.Naming.Internal;

namespace Compze.Core.DocumentDb.Internal.SqlLayer;

interface IDocumentDbSqlLayer
{
   void Update(IReadOnlyList<WriteRow> toUpdate);
   bool TryGet(string idString, IReadOnlySet<TypeId> acceptableTypeIds, bool useUpdateLock, [NotNullWhen(true)] out ReadRow? document);
   void Add(WriteRow row);
   int Remove(string idString, IReadOnlySet<TypeId> acceptableTypes);
   //Urgent: This whole Guid vs string thing must be fixed.
   IEnumerable<Guid> GetAllIds(IReadOnlySet<TypeId> acceptableTypes);
   IReadOnlyList<ReadRow> GetAll(IEnumerable<Guid> ids, IReadOnlySet<TypeId> acceptableTypes);
   IReadOnlyList<ReadRow> GetAll(IReadOnlySet<TypeId> acceptableTypes);

   class ReadRow(Guid typeId, string serializedDocument)
   {
      public Guid TypeId { get; } = typeId;

      public string SerializedDocument { get; } = serializedDocument;
   }

   class WriteRow(string id, string serializedDocument, DateTime updateTime, TypeId typeId)
   {
      public string Id { get; } = id;
      public string SerializedDocument { get; } = serializedDocument;
      public DateTime UpdateTime { get; } = updateTime;
      public TypeId TypeId { get; } = typeId;
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