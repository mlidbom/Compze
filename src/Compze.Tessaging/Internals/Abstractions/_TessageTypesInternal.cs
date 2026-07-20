using Compze.Tessaging.Abstractions.TessageTypes;

namespace Compze.Tessaging.Internals.Abstractions;

//todo:review:urgent: A static class used to nest interface declarations? With the only nested interface name-wise completely inverting the normal ITessage inheritance hierarchy. And it is NOT an ITessage at all. This is wrong in so many ways.
static class TessageTypesInternal
{
#pragma warning disable CA1040 // Marker interface used for type-routing
   internal interface ITessage : IInternalInfrastructureTessage;
#pragma warning restore CA1040
}
