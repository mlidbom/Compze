using System;
using System.Threading.Tasks;

namespace Compze.Persistence.Common.Abstractions;

interface IPoolableConnection : IDisposable, IAsyncDisposable
{
   void Open();
   Task OpenAsync();
}