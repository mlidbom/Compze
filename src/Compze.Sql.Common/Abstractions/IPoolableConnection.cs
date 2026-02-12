using System;
using System.Threading.Tasks;

namespace Compze.Sql.Common.Abstractions;

interface IPoolableConnection : IDisposable, IAsyncDisposable
{
   void Open();
   Task OpenAsync();
}