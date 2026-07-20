namespace Compze.Teventive.TeventStore.Abstractions.Internal.SqlLayer;

static class TeventTableSchemaStrings
{
   public const string TableName = "Tevent";

   public const string ReadOrderType = "decimal(38,19)";

   public const string TaggregateId = nameof(TaggregateId);
   public const string InsertedVersion = nameof(InsertedVersion);
   public const string EffectiveVersion = nameof(EffectiveVersion);
   public const string InsertionOrder = nameof(InsertionOrder);
   public const string ReadOrder = nameof(ReadOrder);

   ///<summary>Used only by sql layers that cannot store a decimal(38,19). They are forced to use two columns.</summary>
   public const string ReadOrderIntegerPart = nameof(ReadOrderIntegerPart);
   ///<summary>Used only by sql layers that cannot store a decimal(38,19). They are forced to use two columns.</summary>
   public const string ReadOrderFractionPart = nameof(ReadOrderFractionPart);

   public const string TargetTevent = nameof(TargetTevent);
   public const string RefactoringType = nameof(RefactoringType);
   public const string UtcTimeStamp = nameof(UtcTimeStamp);
   public const string SqlInsertTimeStamp = nameof(SqlInsertTimeStamp);
   public const string TeventType = nameof(TeventType);
   public const string TeventId = nameof(TeventId);
   public const string Tevent = nameof(Tevent);
}