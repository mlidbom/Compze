namespace Compze.Internals.SystemCE.Core.IOCE;

public interface IBinaryFile
{
   byte[] ReadAllBytes();
   void WriteAllBytes(byte[] bytes);
   void Delete();
}
