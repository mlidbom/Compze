using System;
using System.Threading.Tasks;

namespace Compze.Sql.Common.Abstractions;

public interface IPoolableConnection : IDisposable, IAsyncDisposable
{
   void Open();
   Task OpenAsync();
}