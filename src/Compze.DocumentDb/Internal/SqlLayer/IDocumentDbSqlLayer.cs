using System.Diagnostics.CodeAnalysis;
using Compze.TypeIdentifiers;

namespace Compze.DocumentDb.Internal.SqlLayer;

public interface IDocumentDbSqlLayer
{
   void Update(IReadOnlyList<WriteRow> toUpdate);
   bool TryGet(string idString, IReadOnlySet<MappedTypeId> acceptableTypeIds, bool useUpdateLock, [NotNullWhen(true)] out ReadRow? document);
   void Add(WriteRow row);
   int Remove(string idString, IReadOnlySet<MappedTypeId> acceptableTypes);
   //Urgent: This whole Guid vs string thing must be fixed.
   IEnumerable<Guid> GetAllIds(IReadOnlySet<MappedTypeId> acceptableTypes);
   IReadOnlyList<ReadRow> GetAll(IEnumerable<Guid> ids, IReadOnlySet<MappedTypeId> acceptableTypes);
   IReadOnlyList<ReadRow> GetAll(IReadOnlySet<MappedTypeId> acceptableTypes);

   public class ReadRow(Guid typeId, string serializedDocument)
   {
      internal Guid TypeId { get; } = typeId;

      internal string SerializedDocument { get; } = serializedDocument;
   }

   public class WriteRow(string id, string serializedDocument, DateTime updateTime, MappedTypeId typeId)
   {
      public string Id { get; } = id;
      public string SerializedDocument { get; } = serializedDocument;
      public DateTime UpdateTime { get; } = updateTime;
      public MappedTypeId TypeId { get; } = typeId;
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