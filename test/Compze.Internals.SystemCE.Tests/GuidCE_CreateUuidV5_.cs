using System.Text;
using Compze.Tests.Infrastructure;
using SequentialGuid;

namespace Compze.Internals.SystemCE.Tests;

public class GuidCE_CreateUuidV5_ : UniversalTestBase
{
   static readonly Guid DnsNamespace = new("6ba7b810-9dad-11d1-80b4-00c04fd430c8");
   static readonly Guid UrlNamespace = new("6ba7b811-9dad-11d1-80b4-00c04fd430c8");

   [XF] public void matches_SequentialGuid_for_simple_string()
   {
      var name = "https://example.com";
      var ours = UrlNamespace.CreateUuidV5(Encoding.UTF8.GetBytes(name));
      var reference = GuidV5.Create(UrlNamespace, name);
      ours.Must().Be(reference);
   }

   [XF] public void matches_SequentialGuid_for_dns_name()
   {
      var name = "www.example.com";
      var ours = DnsNamespace.CreateUuidV5(Encoding.UTF8.GetBytes(name));
      var reference = GuidV5.Create(DnsNamespace, name);
      ours.Must().Be(reference);
   }

   [XF] public void matches_SequentialGuid_for_empty_string()
   {
      var ours = UrlNamespace.CreateUuidV5(Encoding.UTF8.GetBytes(""));
      var reference = GuidV5.Create(UrlNamespace, "");
      ours.Must().Be(reference);
   }

   [XF] public void matches_SequentialGuid_with_custom_namespace()
   {
      var customNamespace = new Guid("e4a8c9f2-7b3d-4f1a-9c6e-2d8b5a0f3e7c");
      var name = "some-type-name";
      var ours = customNamespace.CreateUuidV5(Encoding.UTF8.GetBytes(name));
      var reference = GuidV5.Create(customNamespace, name);
      ours.Must().Be(reference);
   }

   [XF] public void is_deterministic_same_inputs_produce_same_output()
   {
      var result1 = DnsNamespace.CreateUuidV5(Encoding.UTF8.GetBytes("test"));
      var result2 = DnsNamespace.CreateUuidV5(Encoding.UTF8.GetBytes("test"));
      result1.Must().Be(result2);
   }

   [XF] public void different_names_produce_different_guids()
   {
      var result1 = DnsNamespace.CreateUuidV5(Encoding.UTF8.GetBytes("name1"));
      var result2 = DnsNamespace.CreateUuidV5(Encoding.UTF8.GetBytes("name2"));
      result1.Must().NotBe(result2);
   }

   [XF] public void different_namespaces_produce_different_guids()
   {
      var payload = Encoding.UTF8.GetBytes("same-name");
      var result1 = DnsNamespace.CreateUuidV5(payload);
      var result2 = UrlNamespace.CreateUuidV5(payload);
      result1.Must().NotBe(result2);
   }
}
