namespace Compze.Sql.Common._internal.Abstractions;

interface IPoolableConnection : IDisposable, IAsyncDisposable
{
   void Open();
   Task OpenAsync();
}