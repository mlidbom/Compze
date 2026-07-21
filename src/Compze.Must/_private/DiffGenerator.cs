using System.Text;
using Compze.Internals.SystemCE.TextCE;
using DiffPlex;
using DiffPlex.Renderer;

namespace Compze.Must._private;

static class DiffGenerator
{
   public static string CreateDiff(string expected, string actual, string? oldFileName = null, string? newFileName = null)
   {
      if(expected.ContainsOrdinal(Environment.NewLine) || actual.ContainsOrdinal(Environment.NewLine))
      {
         return UnidiffRenderer.GenerateUnidiff(oldText: expected, newText: actual, oldFileName: oldFileName ?? "expected", newFileName: newFileName ?? "actual");
      } else
      {
         return SingleLineDiff(expected, actual);
      }
   }

   static readonly char[] WordSeparators = [' '];

   static string SingleLineDiff(string expected, string actual)
   {
      var differ = new Differ();
      var diff = differ.CreateWordDiffs(expected, actual, false, false, WordSeparators);

      var oldPieces = diff.PiecesOld;
      var newPieces = diff.PiecesNew;

      var expectedLine = new StringBuilder();
      var actualLine = new StringBuilder();

      int oldIndex = 0;
      int newIndex = 0;

      foreach(var block in diff.DiffBlocks.OrderBy(b => b.DeleteStartA))
      {
         // Add unchanged pieces before this block (to both lines)
         while(oldIndex < block.DeleteStartA)
         {
            expectedLine.Append(oldPieces[oldIndex]);
            actualLine.Append(newPieces[newIndex]);
            oldIndex++;
            newIndex++;
         }

         // Add deleted pieces (only to expected line)
         for(int i = 0; i < block.DeleteCountA; i++)
         {
            expectedLine.AppendInvariant($"[-{oldPieces[oldIndex]}]");
            oldIndex++;
         }

         // Add inserted pieces (only to actual line)
         for(int i = 0; i < block.InsertCountB; i++)
         {
            actualLine.AppendInvariant($"[+{newPieces[newIndex]}]");
            newIndex++;
         }
      }

      // Add any remaining unchanged pieces
      while(oldIndex < oldPieces.Count)
      {
         expectedLine.Append(oldPieces[oldIndex]);
         actualLine.Append(newPieces[newIndex]);
         oldIndex++;
         newIndex++;
      }

      return $"""
              {expectedLine}
              {actualLine}
              """;
   }
}
