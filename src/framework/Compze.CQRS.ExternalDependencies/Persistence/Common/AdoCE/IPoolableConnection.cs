using System;
using System.Threading.Tasks;

namespace Compze.Persistence.Common.AdoCE;

interface IPoolableConnection : IDisposable, IAsyncDisposable
{
   void Open();
   Task OpenAsync();
}