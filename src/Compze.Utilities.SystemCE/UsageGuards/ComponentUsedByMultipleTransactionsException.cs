namespace Compze.Utilities.SystemCE.UsageGuards;

class ComponentUsedByMultipleTransactionsException(Type componentType) :
   InvalidOperationException($"Using a {componentType.FullName} in multiple transactions is not safe. It makes you vulnerable to hard to debug concurrency issues and is therefore not allowed.");
