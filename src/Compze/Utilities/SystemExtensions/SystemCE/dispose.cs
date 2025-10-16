using System;
using System.Threading.Tasks;

namespace Compze.Utilities.SystemCE;

///<summary>Used everywhere to implement IDisposable concisely and readably. using lowercase dispose so that it does not conflict with the name of the method that uses it.</summary>
#pragma warning disable CS8981 // The type name only contains lower-cased ascii characters. Such names may become reserved for the language.
// ReSharper disable once InconsistentNaming
class dispose
#pragma warning restore CS8981 // The type name only contains lower-cased ascii characters. Such names may become reserved for the language.
{
   ///<summary>Disposes all the passed instances, swallows any <see cref="ObjectDisposedException"/> since they are just noise from incorrect implementations of <see cref="IDisposable"/></summary>
   public static void All(params IDisposable[] disposables)
   {
      foreach(var instance in disposables)
      {
         try { instance.Dispose(); }
         catch(ObjectDisposedException) {}
      }
   }

   ///<summary>Disposes all the passed instances, swallows any <see cref="ObjectDisposedException"/> since they are just noise from incorrect implementations of <see cref="IAsyncDisposable"/></summary>
   public static async ValueTask Async(params IAsyncDisposable[] disposables)
   {
      foreach(var instance in disposables)
      {
         try { await instance.DisposeAsync(); }
         catch(ObjectDisposedException) {}
      }
   }
}
