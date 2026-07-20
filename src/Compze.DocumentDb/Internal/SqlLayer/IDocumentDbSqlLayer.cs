using System.Diagnostics.CodeAnalysis;
using Compze.TypeIdentifiers;

namespace Compze.DocumentDb.Internal.SqlLayer;

interface IDocumentDbSqlLayer
{
   void Update(IReadOnlyList<WriteRow> toUpdate);
   bool TryGet(string idString, TypeId typeId, bool useUpdateLock, [NotNullWhen(true)] out ReadRow? document);
   void Add(WriteRow row);
   int Remove(string idString, TypeId typeId);
   IEnumerable<Guid> GetAllIds(TypeId typeId);
   IReadOnlyList<ReadRow> GetAll(IEnumerable<Guid> ids, TypeId typeId);
   IReadOnlyList<ReadRow> GetAll(TypeId typeId);

   public class ReadRow(TypeId typeId, string serializedDocument)
   {
      internal TypeId TypeId { get; } = typeId;

      internal string SerializedDocument { get; } = serializedDocument;
   }

   public class WriteRow(string id, string serializedDocument, DateTime updateTime, TypeId typeId)
   {
      public string Id { get; } = id;
      public string SerializedDocument { get; } = serializedDocument;
      public DateTime UpdateTime { get; } = updateTime;
      public TypeId TypeId { get; } = typeId;
   }

   public static class DocumentTableSchemaStrings
   {
      public const string TableName = "Store";
      public const string Id = nameof(Id);
      public const string ValueTypeId = nameof(ValueTypeId);
      public const string Created = nameof(Created);
      public const string Updated = nameof(Updated);
      public const string Value = nameof(Value);
   }
}