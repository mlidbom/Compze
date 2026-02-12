using System.Threading.Tasks;
using Compze.Core.Tessaging.Public;

namespace Compze.Tessaging.Implementation.Transport.Client.Internal;

interface IExactlyOnceTessageSender
{
   Task SendAsync(IExactlyOnceTevent tevent);
   Task SendAsync(IExactlyOnceTommand tommand);
}