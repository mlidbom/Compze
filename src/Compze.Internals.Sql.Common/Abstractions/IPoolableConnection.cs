namespace Compze.Internals.Sql.Common.Abstractions;

public interface IPoolableConnection : IDisposable, IAsyncDisposable
{
   void Open();
   Task OpenAsync();
}