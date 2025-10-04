using System;
using System.Threading.Tasks;

namespace Compze.Persistence.Common.AdoCE.Abstractions;

interface IPoolableConnection : IDisposable, IAsyncDisposable
{
   void Open();
   Task OpenAsync();
}