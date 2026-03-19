using System.Text;
using Compze.Tests.Infrastructure;
using SequentialGuid;
#pragma warning disable IDE0230 //We want the explicitness of manually calling Encoding.UTF8.GetBytes in this test.

namespace Compze.Internals.SystemCE.Tests;

public class GuidCE_CreateUuidV5_ : UniversalTestBase
{
   static readonly Guid DnsNamespace = new("6ba7b810-9dad-11d1-80b4-00c04fd430c8");
   static readonly Guid UrlNamespace = new("6ba7b811-9dad-11d1-80b4-00c04fd430c8");

   [XF] public void string_overload_matches_SequentialGuid_for_url()
   {
      var ours = Guid.NewUUIDv5(namespaceId: UrlNamespace, name: "https://example.com");
      var reference = GuidV5.Create(UrlNamespace, "https://example.com");
      ours.Must().Be(reference);
   }

   [XF] public void byte_overload_matches_SequentialGuid_for_url()
   {
      var nameBytes = Encoding.UTF8.GetBytes("https://example.com");
      var ours = Guid.NewUUIDv5(namespaceId: UrlNamespace, payload: nameBytes);
      var reference = GuidV5.Create(UrlNamespace, nameBytes);
      ours.Must().Be(reference);
   }

   [XF] public void string_overload_matches_SequentialGuid_for_dns()
   {
      var ours = Guid.NewUUIDv5(namespaceId: DnsNamespace, name: "www.example.com");
      var reference = GuidV5.Create(DnsNamespace, "www.example.com");
      ours.Must().Be(reference);
   }

   [XF] public void byte_overload_matches_SequentialGuid_for_dns()
   {
      var nameBytes = Encoding.UTF8.GetBytes("www.example.com");
      var ours = Guid.NewUUIDv5(namespaceId: DnsNamespace, payload: nameBytes);
      var reference = GuidV5.Create(DnsNamespace, nameBytes);
      ours.Must().Be(reference);
   }

   [XF] public void string_overload_matches_SequentialGuid_for_empty_string()
   {
      var ours = Guid.NewUUIDv5(namespaceId: UrlNamespace, name: "");
      var reference = GuidV5.Create(UrlNamespace, "");
      ours.Must().Be(reference);
   }

   [XF] public void byte_overload_matches_SequentialGuid_for_empty()
   {
      var nameBytes = Encoding.UTF8.GetBytes("");
      var ours = Guid.NewUUIDv5(namespaceId: UrlNamespace, payload: nameBytes);
      var reference = GuidV5.Create(UrlNamespace, nameBytes);
      ours.Must().Be(reference);
   }

   [XF] public void string_overload_matches_SequentialGuid_with_custom_namespace()
   {
      var customNamespace = new Guid("e4a8c9f2-7b3d-4f1a-9c6e-2d8b5a0f3e7c");
      var ours = Guid.NewUUIDv5(namespaceId: customNamespace, name: "some-type-name");
      var reference = GuidV5.Create(customNamespace, "some-type-name");
      ours.Must().Be(reference);
   }

   [XF] public void byte_overload_matches_SequentialGuid_with_custom_namespace()
   {
      var customNamespace = new Guid("e4a8c9f2-7b3d-4f1a-9c6e-2d8b5a0f3e7c");
      var nameBytes = Encoding.UTF8.GetBytes("some-type-name");
      var ours = Guid.NewUUIDv5(namespaceId: customNamespace, payload: nameBytes);
      var reference = GuidV5.Create(customNamespace, nameBytes);
      ours.Must().Be(reference);
   }

   [XF] public void is_deterministic_same_inputs_produce_same_output()
   {
      var result1 = Guid.NewUUIDv5(namespaceId: DnsNamespace, name: "test");
      var result2 = Guid.NewUUIDv5(namespaceId: DnsNamespace, name: "test");
      result1.Must().Be(result2);
   }

   [XF] public void different_names_produce_different_guids()
   {
      var result1 = Guid.NewUUIDv5(namespaceId: DnsNamespace, name: "name1");
      var result2 = Guid.NewUUIDv5(namespaceId: DnsNamespace, name: "name2");
      result1.Must().NotBe(result2);
   }

   [XF] public void different_namespaces_produce_different_guids()
   {
      var result1 = Guid.NewUUIDv5(namespaceId: DnsNamespace, name: "same-name");
      var result2 = Guid.NewUUIDv5(namespaceId: UrlNamespace, name: "same-name");
      result1.Must().NotBe(result2);
   }
}
