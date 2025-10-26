using System.Threading.Tasks;
using Compze.Core.Tessaging.Public;

namespace Compze.Tessaging.Implementation.Transport.Client.Abstractions;

interface IExactlyOnceTessageSender
{
   Task SendAsync(IExactlyOnceTevent tevent);
   Task SendAsync(IExactlyOnceTommand tommand);
}