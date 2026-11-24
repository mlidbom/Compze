namespace Compze.Teventive.MemoryImageStateStore._internal.SqlLayer;

///<summary>Rows read from a memory image state store's taggregate table, and the last commit number, read before them.</summary>
sealed class TaggregateRows
{
   internal TaggregateRows(CommitNumber holdingEveryCommitThrough, IReadOnlyList<TaggregateRow> rows)
   {
      HoldingEveryCommitThrough = holdingEveryCommitThrough;
      Rows = rows;
   }

   ///<summary>The last commit number when the rows were read: they hold every commit up to it, and may hold later ones.</summary>
   internal CommitNumber HoldingEveryCommitThrough { get; }

   internal IReadOnlyList<TaggregateRow> Rows { get; }
}
