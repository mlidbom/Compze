namespace Compze.Internals.SystemCE.IOCE;

public interface IBinaryFile
{
   byte[] ReadAllBytes();
   void WriteAllBytes(byte[] bytes);
   void Delete();
}
