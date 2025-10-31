using Compze.Utilities.SystemCE;
using DiffPlex;
using DiffPlex.Renderer;
using System;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Compze.Tests.Infrastructure.Fluent;

public static class StringBe
{
   public static IMust<string>? Be(this IMust<string> must, string expected)
      => must.Satisfy(it => Equals(it, expected),
                      () =>
                         $"""

                          expected the expression: 
                          {must.Separator}
                          {must.Expression.Indent()}
                          {must.Separator}
                          to Be:
                          {must.Separator}
                          {expected}
                          {must.Separator}
                          but it was:
                          {must.Separator}
                          {must.Actual}
                          {must.Separator}
                          Diff:
                          {must.Separator}
                          {DiffGenerator.CreateDiff(expected, must.Actual)}

                          """);
}

static class DiffGenerator
{
   public static string CreateDiff(string expected, string actual)
   {
      if(expected.ContainsInvariant(Environment.NewLine))
      {
         return UnidiffRenderer.GenerateUnidiff(oldText: expected, newText: actual, oldFileName: "expression", newFileName: "expected");
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
            expectedLine.Append(CultureInfo.InvariantCulture, $"[-{oldPieces[oldIndex]}]");
            oldIndex++;
         }

         // Add inserted pieces (only to actual line)
         for(int i = 0; i < block.InsertCountB; i++)
         {
            actualLine.Append(CultureInfo.InvariantCulture, $"[+{newPieces[newIndex]}]");
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
