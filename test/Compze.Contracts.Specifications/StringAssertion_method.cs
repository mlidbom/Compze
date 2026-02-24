using System;
using Compze.Utilities.Testing.XUnit.BDD;

namespace Compze.Contracts.Specifications;

public class StringAssertion_method : AssertionMethodsTest
{
   const string EmptyString = "";
   static readonly string? NullString = null;
   const string SpacesString = " ";
   const string TabsString = "   ";
   static readonly string NewLineString = Environment.NewLine;

   public class NotNull_trows_for
   {
      [XF] public void null_string() => ThrowsAndCapturesArgumentExpressionText(() => Asserter.NotNullOrEmpty(NullString), NullString);
   }

   public class NotNullEmptyOrWhitespace_throws_for_
   {
      [XF] public void null_string() => ThrowsAndCapturesArgumentExpressionText(() => Asserter.NotNullEmptyOrWhitespace(NullString), NullString);
      [XF] public void empty_string() => ThrowsAndCapturesArgumentExpressionText(() => Asserter.NotNullEmptyOrWhitespace(EmptyString), EmptyString);
      [XF] public void spaces_string() => ThrowsAndCapturesArgumentExpressionText(() => Asserter.NotNullEmptyOrWhitespace(SpacesString), SpacesString);
      [XF] public void tabs_string() => ThrowsAndCapturesArgumentExpressionText(() => Asserter.NotNullEmptyOrWhitespace(TabsString), TabsString);
      [XF] public void newLine_string() => ThrowsAndCapturesArgumentExpressionText(() => Asserter.NotNullEmptyOrWhitespace(NewLineString), NewLineString);
   }
}
