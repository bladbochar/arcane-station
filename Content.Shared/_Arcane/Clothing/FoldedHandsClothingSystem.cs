using Content.Shared.Clothing;
using Content.Shared.Clothing.Components;
using Content.Shared.Foldable;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction.Components;
using Content.Shared.Inventory.VirtualItem;
using Robust.Shared.Containers;

namespace Content.Shared._Arcane.Clothing;

public sealed class FoldedHandsClothingSystem : EntitySystem
{
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedVirtualItemSystem _virtualItem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<FoldedHandsClothingComponent, FoldedEvent>(OnFolded);
        SubscribeLocalEvent<FoldedHandsClothingComponent, ClothingGotEquippedEvent>(OnEquipped);
        SubscribeLocalEvent<FoldedHandsClothingComponent, ClothingGotUnequippedEvent>(OnUnequipped);
    }

    private void OnFolded(Entity<FoldedHandsClothingComponent> ent, ref FoldedEvent args)
    {
        if (!TryGetWearer(ent, out var wearer))
            return;

        if (args.IsFolded)
            BlockHands(ent, wearer);
        else
            _virtualItem.DeleteInHandsMatching(wearer, ent.Owner);
    }

    private void OnEquipped(Entity<FoldedHandsClothingComponent> ent, ref ClothingGotEquippedEvent args)
    {
        if (!TryComp<FoldableComponent>(ent, out var foldable) || !foldable.IsFolded)
            return;

        BlockHands(ent, args.Wearer);
    }

    private void OnUnequipped(Entity<FoldedHandsClothingComponent> ent, ref ClothingGotUnequippedEvent args)
    {
        _virtualItem.DeleteInHandsMatching(args.Wearer, ent.Owner);
    }

    private void BlockHands(Entity<FoldedHandsClothingComponent> ent, EntityUid wearer)
    {
        // Snapshot the hand list before spawning virtual items to avoid mutating
        // the hands collection while iterating it.
        var handCount = _hands.EnumerateHands(wearer).Count();
        for (var i = 0; i < handCount; i++)
        {
            if (_virtualItem.TrySpawnVirtualItemInHand(ent.Owner, wearer, out var vItem, dropOthers: true))
                EnsureComp<UnremoveableComponent>(vItem.Value);
        }
    }

    private bool TryGetWearer(Entity<FoldedHandsClothingComponent> ent, out EntityUid wearer)
    {
        wearer = default;

        if (!TryComp<ClothingComponent>(ent, out var clothing) || clothing.InSlot == null)
            return false;

        if (!_container.TryGetContainingContainer(ent.Owner, out var cont))
            return false;

        wearer = cont.Owner;
        return true;
    }
}
