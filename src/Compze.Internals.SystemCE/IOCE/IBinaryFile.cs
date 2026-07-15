namespace Compze.Internals.SystemCE.IOCE;

public interface IBinaryFile : IDisposable
{
   byte[] ReadAllBytes();
   void WriteAllBytes(byte[] bytes);
   void Delete();
}
