namespace Compze.InterprocessObject;

///<summary>Controls behavior when deserialization of the stored object fails.</summary>
public enum CorruptionAction
{
   ///<summary>Throw an exception without modifying the backing file.</summary>
   ThrowException = 0,

   ///<summary>Delete the corrupt file, write a fresh default, then throw an exception describing what happened.</summary>
   ReplaceContentWithDefaultAndThrow = 1
}
