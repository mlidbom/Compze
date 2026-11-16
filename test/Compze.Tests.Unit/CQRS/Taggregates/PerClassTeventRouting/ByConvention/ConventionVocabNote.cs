using Compze.Abstractions;
using Compze.Teventive;
using Compze.Teventive.Taggregates.BaseClasses;
using Compze.Teventive.Taggregates.BaseClasses.Shared;
using Compze.Teventive.Taggregates.Tevents;

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedMember.Local The Apply and Handle methods are found by convention.
// ReSharper disable UnusedParameter.Local
#pragma warning disable CA1812 // Avoid uninstantiated internal classes # used via reflection
#pragma warning disable IDE0051 // The Apply and Handle methods are found by convention.

namespace Compze.Tests.Unit.CQRS.Taggregates.PerClassTeventRouting.ByConvention;

///<summary>The same note as <see cref="Note"/>, its tevents routed by convention instead of declared.</summary>
[TeventRoutingByConvention]
class ConventionNote : Taggregate<ConventionNote, INoteTevent, NoteTevent, INoteTevent<INoteTevent>, NoteTevent<NoteTevent>>
{
   protected ConventionNote() {}

   public string Name { get; private set; } = "";

   public void Rename(string name) => Publish(new NoteTevent.Renamed(name));

   protected void PublishCreated() => Publish(new NoteTevent.Created(new TaggregateId()));

   void Apply(INoteTevent.Created _) {}
   void Apply(INoteTevent.Renamed tevent) => Name = tevent.Name;
}

///<summary>The same vocab as <see cref="VocabNote"/>: its component, entity collection and shared-tomponent slot are found in its fields.</summary>
class ConventionVocabNote : ConventionNote
{
   protected override Type WrapperTeventImplementation => typeof(VocabTevent<VocabTevent>);

   readonly ConventionSense.ICollectionManager _senses;
   readonly SharedTomponentSlot<INoteTevent, NoteTevent, IAudioListTevent, IVocabAudioTevent<IAudioListTevent>> _audioSlot;

   ConventionVocabNote()
   {
      Forms = new ConventionForms(this);
      _senses = ConventionSense.CreateSelfManagingCollection(this);
      _audioSlot = new SharedTomponentSlot<INoteTevent, NoteTevent, IAudioListTevent, IVocabAudioTevent<IAudioListTevent>>(this, typeof(VocabAudioTevent<IAudioListTevent>));
      Audio = new ConventionAudioList(_audioSlot);
   }

   public ConventionForms Forms { get; }
   public ConventionAudioList Audio { get; }
   public IReadOnlyEntityCollection<ConventionSense, Guid> Senses => _senses.Entities;

   public string Question => $"{Name}?";

   public ConventionSense AddSense(Guid senseId, string gloss) => _senses.AddByPublishing(new VocabTevent.Sense.Added(senseId, gloss));

   public static ConventionVocabNote Create()
   {
      var vocab = new ConventionVocabNote();
      vocab.PublishCreated();
      return vocab;
   }

   public static ConventionVocabNote LoadFromHistory(IEnumerable<ITaggregateTevent<ITaggregateTevent>> history)
   {
      var vocab = new ConventionVocabNote();
      ((ITaggregate)vocab).LoadFromHistory(history);
      return vocab;
   }
}

[TeventRoutingByConvention]
class ConventionForms : TeventiveComponent<ConventionVocabNote, INoteTevent, NoteTevent, ConventionForms, IVocabTevent.Forms, VocabTevent.Forms>
{
   readonly ConventionVocabNote _vocab;

   public ConventionForms(ConventionVocabNote vocab) : base(vocab) => _vocab = vocab;

   public IReadOnlyList<string> All { get; private set; } = [];
   public string Summary { get; private set; } = "";
   public int Count { get; private set; }

   public void Update(string allForms) => Publish(new VocabTevent.Forms.Updated(allForms));
   public void PublishATeventNoApplierHandles() => Publish(new VocabTevent.Forms.RoutedNowhere());

   void Apply(IVocabTevent.Forms.Updated tevent)
   {
      All = tevent.AllForms.Split(',');
      Summary = $"{_vocab.Question} {tevent.AllForms}";
   }

   void Apply(IVocabTevent.Forms.Counted tevent) => Count = tevent.Count;

   void Handle(IVocabTevent.Forms.Updated _) => Publish(new VocabTevent.Forms.Counted(All.Count));
}

[TeventRoutingByConvention]
class ConventionSense : Tentity<ConventionVocabNote, INoteTevent, NoteTevent, ConventionSense, Guid, VocabTevent.Sense, IVocabTevent.Sense, IVocabTevent.Sense.Added, VocabTevent.Sense.IdGetterSetter>
{
   public ConventionSense(ConventionVocabNote vocab) : base(vocab) {}

   public string Gloss { get; private set; } = "";

   public void Redefine(string gloss) => Publish(new VocabTevent.Sense.Redefined(gloss));

   void Apply(IVocabTevent.Sense.Defined tevent) => Gloss = tevent.Gloss;
}

[TeventRoutingByConvention]
class ConventionAudioList : SharedTomponent<IAudioListTevent>
{
   readonly List<string> _files = [];

   public ConventionAudioList(ISharedTomponentSlot<IAudioListTevent> slot) : base(slot) {}

   public IReadOnlyList<string> Files => _files;

   public void Add(string file) => Publish(new AudioListTevent.Added(file));

   void Apply(IAudioListTevent.Added tevent) => _files.Add(tevent.File);
}
