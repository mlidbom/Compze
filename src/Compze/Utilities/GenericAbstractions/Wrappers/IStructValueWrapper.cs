namespace Compze.Utilities.GenericAbstractions.Wrappers;

public interface IStructValueWrapper
{
   object UntypedValue { get; }
}

public interface IStructValueWrapper<out TWrapped> : IStructValueWrapper
   where TWrapped : struct
{
   TWrapped Value { get; }
   object IStructValueWrapper.UntypedValue => Value;
}
