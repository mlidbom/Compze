using System.Diagnostics.CodeAnalysis;
using Compze.Core.Refactoring.Naming.Internal;

namespace Compze.Core.DocumentDb.Internal.SqlLayer;

public interface IDocumentDbSqlLayer
{
   void Update(IReadOnlyList<WriteRow> toUpdate);
   bool TryGet(string idString, IReadOnlySet<TypeId> acceptableTypeIds, bool useUpdateLock, [NotNullWhen(true)] out ReadRow? document);
   void Add(WriteRow row);
   int Remove(string idString, IReadOnlySet<TypeId> acceptableTypes);
   //Urgent: This whole Guid vs string thing must be fixed.
   IEnumerable<Guid> GetAllIds(IReadOnlySet<TypeId> acceptableTypes);
   IReadOnlyList<ReadRow> GetAll(IEnumerable<Guid> ids, IReadOnlySet<TypeId> acceptableTypes);
   IReadOnlyList<ReadRow> GetAll(IReadOnlySet<TypeId> acceptableTypes);

   public class ReadRow(Guid typeId, string serializedDocument)
   {
      internal Guid TypeId { get; } = typeId;

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