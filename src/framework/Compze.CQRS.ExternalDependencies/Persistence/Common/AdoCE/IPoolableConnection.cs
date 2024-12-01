using System;
using System.Threading.Tasks;

namespace Composable.Persistence.Common.AdoCE;

interface IPoolableConnection : IDisposable, IAsyncDisposable
{
   void Open();
   Task OpenAsync();
}