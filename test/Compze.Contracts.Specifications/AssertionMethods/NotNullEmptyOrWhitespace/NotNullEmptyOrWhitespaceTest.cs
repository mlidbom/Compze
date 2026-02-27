using System;

namespace Compze.Contracts.Specifications.AssertionMethods.NotNullEmptyOrWhitespace;

public abstract class NotNullEmptyOrWhitespaceTest : AssertionMethodsTest
{
   protected static readonly string?[] InvalidValues = [null, "", " ", "\t", Environment.NewLine];
}
