using Compze.SystemCE.LinqCE;
using Compze.Testing;
using FluentAssertions;
using NUnit.Framework;

namespace Compze.Tests.SystemCE.LinqCE;

[TestFixture]
public class ExpressionUtilTests: UniversalTestBase
{
   [Test]
   public void CanExtractFromMemberAccessingLambdaWithNoParameter() => ExpressionUtil.ExtractMemberName(() => MyMember).Should().Be("MyMember");

   [Test]
   public void CanExtractFromMemberAccessingLambdaWithParameter() => ExpressionUtil.ExtractMemberName((ExpressionUtilTests me) => MyMember).Should().Be("MyMember");

   [Test]
   public void CanExtractFromMemberAccessingLambdaWith2Parameters() => ExpressionUtil.ExtractMemberName((ExpressionUtilTests me, object irrelevant) => MyMember).Should().Be("MyMember");

   static object MyMember => throw new global::System.Exception(); //ncrunch: no coverage
}