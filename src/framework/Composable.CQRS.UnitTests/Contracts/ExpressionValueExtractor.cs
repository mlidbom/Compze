using Composable.Contracts;
using NUnit.Framework;
using Assert = NUnit.Framework.Assert;

namespace Composable.Tests.Contracts;

[TestFixture]
public class ExpressionValueExtractorTests
{
   string TestString { get; } = "TestString";

   readonly object _wrappedIntOne = 1;

   [Test]
   public void ExtractsValuesFromFieldAccessingLambdas()
   {
      var result = ContractsExpression.ExtractValue(() => TestString);
      Assert.That(result, Is.SameAs(TestString));

      var result2 = ContractsExpression.ExtractValue(() => _wrappedIntOne);
      Assert.That(result2, Is.SameAs(_wrappedIntOne));
   }

   [Test]
   public void ExtractsValueFromPropertyAccessLambda()
   {
      var result = ContractsExpression.ExtractValue(() => TestString);
      Assert.That(result, Is.SameAs(TestString));
   }

   [Test]
   public void ExtractsValueFromParameterAccess()
   {
      var result = ReturnExtractedParameterValue(TestString);
      Assert.That(result, Is.SameAs(TestString));
   }

   static TValue ReturnExtractedParameterValue<TValue>(TValue param) => ContractsExpression.ExtractValue(() => param);
}