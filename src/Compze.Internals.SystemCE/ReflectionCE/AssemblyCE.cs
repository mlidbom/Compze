using System.Reflection;

public static class AssemblyCE
{
   extension(Assembly @this)
   {
      public string? SimpleName => @this.GetName().Name;

      public string PublicKeyTokenString
      {
         get
         {
            var tokenBytes = @this.GetName().GetPublicKeyToken();
            if(tokenBytes == null || tokenBytes.Length == 0)
               return "";

            return Convert.ToHexStringLower(tokenBytes);
         }
      }
   }
}
