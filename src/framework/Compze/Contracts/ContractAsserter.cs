using System;

namespace Compze.Contracts;

partial class ContractAsserter(Func<string, Exception> createException)
{
   readonly Func<string, Exception> _createException = createException;
}
