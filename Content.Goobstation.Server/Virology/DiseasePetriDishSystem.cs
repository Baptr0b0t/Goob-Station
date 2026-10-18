using Content.Goobstation.Shared.Disease.Systems;
using Content.Goobstation.Shared.Virology;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Robust.Server.Audio;
using Robust.Shared.Timing;

namespace Content.Goobstation.Server.Virology;

/// <summary>
/// Lets virology duplicate a disease.
/// </summary>
public sealed partial class DiseasePetriDishSystem : EntitySystem
{
    [Dependency] private AudioSystem _audio = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SharedDiseaseSystem _disease = default!;
    [Dependency] private SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DiseasePetriDishComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<DiseasePetriDishComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<DiseasePetriDishComponent, InteractUsingEvent>(OnInteractUsing);
    }

    private void OnShutdown(Entity<DiseasePetriDishComponent> ent, ref ComponentShutdown args)
    {
        QueueDel(ent.Comp.DiseaseUid);
    }

    private void OnExamined(Entity<DiseasePetriDishComponent> ent, ref ExaminedEvent args)
    {
        LocId message = ent.Comp.DiseaseUid == null
            ? "disease-petri-dish-sterile"
            : "disease-petri-dish-cultured";

        args.PushMarkup(Loc.GetString(message));
    }

    private void OnInteractUsing(Entity<DiseasePetriDishComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled || !TryComp<DiseaseSwabComponent>(args.Used, out var swab))
            return;

        args.Handled = true;

        if (swab.DiseaseUid != null)
            Seed(ent, (args.Used, swab), args.User);
        else
            Sample(ent, (args.Used, swab), args.User);
    }

    private void Seed(Entity<DiseasePetriDishComponent> ent, Entity<DiseaseSwabComponent> swab, EntityUid user)
    {
        if (ent.Comp.DiseaseUid != null)
        {
            Popup(ent, user, "disease-petri-dish-occupied");
            return;
        }

        var disease = _disease.TryClone(swab.Comp.DiseaseUid!.Value);
        if (disease == null)
            return;

        ent.Comp.DiseaseUid = disease;
        ent.Comp.SampleReadyAt = _timing.CurTime + ent.Comp.CultureDuration;
        Popup(ent, user, "disease-petri-dish-seeded");
    }

    private void Sample(Entity<DiseasePetriDishComponent> ent, Entity<DiseaseSwabComponent> swab, EntityUid user)
    {
        if (ent.Comp.DiseaseUid == null)
        {
            Popup(ent, user, "disease-petri-dish-sterile");
            return;
        }

        if (_timing.CurTime < ent.Comp.SampleReadyAt)
        {
            Popup(ent, user, "disease-petri-dish-regrowing");
            return;
        }

        var disease = _disease.TryClone(ent.Comp.DiseaseUid.Value);
        if (disease == null)
            return;

        swab.Comp.DiseaseUid = disease;
        Dirty(swab);

        ent.Comp.SampleReadyAt = _timing.CurTime + ent.Comp.CultureDuration;
        _audio.PlayPvs(ent.Comp.SampleSound, ent);
        Popup(ent, user, "disease-petri-dish-sampled");
    }

    private void Popup(Entity<DiseasePetriDishComponent> ent, EntityUid user, LocId message)
    {
        _popup.PopupEntity(Loc.GetString(message), ent, user);
    }
}
