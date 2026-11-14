using Compze.Tessaging.Endpoints;

namespace Compze.Hosting.AcrossMachines._private;

///<summary>One of the registries an <see cref="EndpointRegistryServer"/> keeps (<see cref="NamedRegistries"/>): what each<br/>
/// of its participants announces in it. Together that is the registry's membership.</summary>
///<remarks>An endpoint has one announcement in a registry. When a participant announces an endpoint that another<br/>
/// participant announced before, the later announcement replaces the earlier one: the endpoint started again in another<br/>
/// process, whose connection the server has not yet seen end.</remarks>
sealed class NamedRegistry
{
   readonly Dictionary<EndpointId, AnnouncementOfAParticipant> _announcementsByEndpoint = new();

   ///<summary>Changes whenever the membership may have: what a participant waiting to be sent the membership compares<br/>
   /// with the version it was sent last.</summary>
   internal long MembershipVersion { get; private set; }

   internal IReadOnlyList<EndpointAnnouncement> Membership => [.._announcementsByEndpoint.Values.Select(it => it.Announcement)];

   ///<summary><paramref name="announcements"/> are from now on everything <paramref name="participant"/> announces in the registry.</summary>
   internal void ReplaceAnnouncementsOf(RegistryParticipant participant, IReadOnlyList<EndpointAnnouncement> announcements)
   {
      RemoveAnnouncementsOf(participant);
      foreach(var announcement in announcements)
         _announcementsByEndpoint[announcement.EndpointId] = new AnnouncementOfAParticipant(announcement, participant);
      MembershipVersion++;
   }

   ///<summary>Removes what <paramref name="participant"/> announces. An announcement of its that another participant's<br/>
   /// has replaced is not its own any more, and stays.</summary>
   internal void RemoveAnnouncementsOf(RegistryParticipant participant)
   {
      foreach(var endpointId in _announcementsByEndpoint.Where(it => it.Value.Participant == participant).Select(it => it.Key).ToList())
         _announcementsByEndpoint.Remove(endpointId);
      MembershipVersion++;
   }

   sealed class AnnouncementOfAParticipant
   {
      internal AnnouncementOfAParticipant(EndpointAnnouncement announcement, RegistryParticipant participant)
      {
         Announcement = announcement;
         Participant = participant;
      }

      internal EndpointAnnouncement Announcement { get; }
      internal RegistryParticipant Participant { get; }
   }
}
