using System.Threading.Tasks;
using Compze.Abstractions.Tessaging.Public;

namespace Compze.Tessaging.Implementation.Transport.Client.Abstractions;

interface IRemoteTessageSender
{
   Task SendAsync(IExactlyOnceTevent tevent);
   Task SendAsync(IExactlyOnceTommand tommand);
}