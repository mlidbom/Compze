using System;
using Compze.Testing.TestFrameworkExtensions.XUnit;

namespace Compze.Tests.Unit.Internals.Contracts;

public class StringAssertion_method : AssertionTestBase
{
   const string EmptyString = "";
   static readonly string NullString = null;
   const string SpacesString = " ";
   const string TabsString = "   ";
   static readonly string NewLineString = Environment.NewLine;

   public class NotNull_trows_for
   {
      [XFact] public void null_string() => ThrowsAndCapturesArgumentExpressionText(() => Asserter.NotNullOrEmpty(NullString), NullString);
   }

   public class NotNullEmptyOrWhitespace_throws_for_
   {
      [XFact] public void null_string() => ThrowsAndCapturesArgumentExpressionText(() => Asserter.NotNullEmptyOrWhitespace(NullString), NullString);
      [XFact] public void empty_string() => ThrowsAndCapturesArgumentExpressionText(() => Asserter.NotNullEmptyOrWhitespace(EmptyString), EmptyString);
      [XFact] public void spaces_string() => ThrowsAndCapturesArgumentExpressionText(() => Asserter.NotNullEmptyOrWhitespace(SpacesString), SpacesString);
      [XFact] public void tabs_string() => ThrowsAndCapturesArgumentExpressionText(() => Asserter.NotNullEmptyOrWhitespace(TabsString), TabsString);
      [XFact] public void newLine_string() => ThrowsAndCapturesArgumentExpressionText(() => Asserter.NotNullEmptyOrWhitespace(NewLineString), NewLineString);
   }
}
