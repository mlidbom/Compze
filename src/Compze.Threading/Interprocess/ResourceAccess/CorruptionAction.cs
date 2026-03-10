namespace Compze.Threading.Interprocess.ResourceAccess;

public enum CorruptionAction
{
   ThrowException = 0,
   ReplaceContentWithDefaultAndThrow = 1
}
