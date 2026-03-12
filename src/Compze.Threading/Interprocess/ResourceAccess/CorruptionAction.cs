namespace Compze.Threading.Interprocess.ResourceAccess;

///<summary>Specifies how a <see cref="IFileBackedProcessShared{TShared}"/> handles a corrupted backing file.</summary>
public enum CorruptionAction
{
   ///<summary>Throw the deserialization exception as-is.</summary>
   ThrowException = 0,
   ///<summary>Replace the corrupted file content with the default value and throw the original exception.</summary>
   ReplaceContentWithDefaultAndThrow = 1
}
