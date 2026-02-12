using System;

namespace Compze.Sql.Common.Abstractions;

#pragma warning disable CA1711 // This IS a Flags enum
[Flags] public enum PoolableConnectionFlags
{
   Defaults = 0,
   MustUseSameConnectionThroughoutATransaction = 1
}