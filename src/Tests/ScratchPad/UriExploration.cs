using System;
using Compze.Utilities.Logging;
using Compze.Utilities.SystemCE;
using Compze.Utilities.Testing.XUnit.BDD;
using Newtonsoft.Json;

namespace Compze.Tests.ScratchPad;

public class UriExploration
{
   [XF] public void Http()
   {
      var uri = new Uri("http://localhost:8888/internal/rpc/tuery?a=1;b=2;");

        this.Log().Info($"""
                       
                       {uri.ToString()}
                       {uri.Host}
                       {uri.Fragment}
                       {uri.AbsoluteUri}
                       {uri.IsUnc}
                       {uri.Query}
                       
                       """.Indent());
    }

   [XF] public void Memory()
   {
      var uri = new Uri("memory://localhost/aloreucdaoeulst");

      this.Log().Info($"""

                       {uri.ToString()}
                       {uri.Host}
                       {uri.Fragment}
                       {uri.AbsoluteUri}
                       {uri.IsUnc}
                       {uri.Query}
                       {uri.AbsolutePath}

                       """.Indent());
   }
}
