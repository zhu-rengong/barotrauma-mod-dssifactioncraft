using System;
using System.Collections.Generic;
using System.Linq;
using Barotrauma;
using Barotrauma.Extensions;
using Barotrauma.Items.Components;

namespace DSSIFactionCraft.Items.Components
{
    internal class DfcItemCleaner : ItemComponent
    {
        public abstract class CleanOption
        {
            public string[][] includes = Array.Empty<string[]>();
            public string[][] excludes = Array.Empty<string[]>();

            public CleanOption() { }

            protected abstract bool MatchesSpecific(Item item);

            public bool Matches(Item item)
            {
                if (!MatchesSpecific(item)) { return false; }
                return ItemUtils.Matches(item, includes, excludes);
            }
        }

        public class WeakCleanOption : CleanOption
        {
            public WeakCleanOption() : base() { }
            protected override bool MatchesSpecific(Item item)
                => item.PhysicsBodyActive && item.ParentInventory is null;
        }

        public class StrongCleanOption : CleanOption
        {
            public StrongCleanOption() : base() { }
            protected override bool MatchesSpecific(Item item) { return true; }
        }

        protected static bool IsMultiplayerClient => GameMain.NetworkMember?.IsClient ?? false;

        private WeakCleanOption weakCleanOption;
        private StrongCleanOption strongCleanOption;

        private string weakCleanPatternIncludes;
        [InGameEditable, Serialize("clothing,medical,weapon,explosive,diving,tool,mobilecontainer,harpoonammo,stungunammo,pistolammoitem,shotgunammo,rifleammo,assaultrifleammo,smgammo,hmgammo,grenade,reactorfuel,provocative,loadable,oxygensource,weldingfuel,skillbook,mobileradio",
        IsPropertySaveable.Yes, translationTextTag: "sp.")]
        public string WeakCleanPatternIncludes
        {
            get => weakCleanPatternIncludes;
            set => weakCleanPatternIncludes = value;
        }

        private string weakCleanPatternExcludes;
        [InGameEditable, Serialize("disableclean", IsPropertySaveable.Yes, translationTextTag: "sp.")]
        public string WeakCleanPatternExcludes
        {
            get => weakCleanPatternExcludes;
            set => weakCleanPatternExcludes = value;
        }

        private string strongCleanPatternIncludes;
        [InGameEditable, Serialize("duffelbag,crate", IsPropertySaveable.Yes, translationTextTag: "sp.")]
        public string StrongCleanPatternIncludes
        {
            get => strongCleanPatternIncludes;
            set => strongCleanPatternIncludes = value;
        }

        private string strongCleanPatternExcludes;
        [InGameEditable, Serialize("disableclean", IsPropertySaveable.Yes, translationTextTag: "sp.")]
        public string StrongCleanPatternExcludes
        {
            get => strongCleanPatternExcludes;
            set => strongCleanPatternExcludes = value;
        }

        public void UpdateCleanOptions()
        {
            weakCleanOption.includes = ItemUtils.ParseMatcherPattern(weakCleanPatternIncludes);
            weakCleanOption.excludes = ItemUtils.ParseMatcherPattern(weakCleanPatternExcludes);
            strongCleanOption.includes = ItemUtils.ParseMatcherPattern(strongCleanPatternIncludes);
            strongCleanOption.excludes = ItemUtils.ParseMatcherPattern(strongCleanPatternExcludes);
        }

        [InGameEditable(MinValueInt = 1, MaxValueInt = 100), Serialize(1, IsPropertySaveable.Yes, alwaysUseInstanceValues: true, translationTextTag: "sp.")]
        public int ToleranceThreshold { get; set; }

        [InGameEditable, Serialize(true, IsPropertySaveable.Yes, alwaysUseInstanceValues: true, translationTextTag: "sp.")]
        public bool OnlyIndoor { get; set; }

        [InGameEditable, Serialize(true, IsPropertySaveable.Yes, alwaysUseInstanceValues: true, translationTextTag: "sp.")]
        public bool IgnoreAttached { get; set; }

        [InGameEditable, Serialize(true, IsPropertySaveable.Yes, alwaysUseInstanceValues: true, translationTextTag: "sp.")]
        public bool IgnoreStaticBody { get; set; }

        [InGameEditable, Serialize(true, IsPropertySaveable.Yes, alwaysUseInstanceValues: true, translationTextTag: "sp.")]
        public bool IgnoreInitial { get; set; }

        private static List<Item> initialItemList;

        private Dictionary<Item, int> Tolerance;

        private IList<Item> includes;
        private IList<Item> excludes;

        public DfcItemCleaner(Item item, ContentXElement element) : base(item, element)
        {
            IsActive = false;
            if (IsMultiplayerClient) { return; }
            Tolerance = new();
            weakCleanOption = new();
            strongCleanOption = new();
        }

        public override void OnItemLoaded()
        {
            base.OnItemLoaded();
            if (IsMultiplayerClient) { return; }
            UpdateCleanOptions();
        }

        static DfcItemCleaner()
        {
            if (IsMultiplayerClient) { return; }
            GameMain.LuaCs.Hook.Add("roundStart", nameof(DfcItemCleaner), initialize);
            object initialize(params object[] args)
            {
                initialItemList = new(Item.ItemList);
                return default;
            }
        }

        public override void Update(float deltaTime, Camera cam)
        {
            includes ??= RegionUtils.GetFromLinkedTo(item, "dfc_regionincluded");
            excludes ??= RegionUtils.GetFromLinkedTo(item, "dfc_regionexcluded");
            List<Item> itemsToRemove = new();
            foreach (var item in Item.ItemList)
            {
                if (OnlyIndoor && item.CurrentHull == null) { continue; }
                if (IgnoreStaticBody && (item.body?.BodyType ?? FarseerPhysics.BodyType.Static) == FarseerPhysics.BodyType.Static) { continue; }
                if (IgnoreAttached && item.GetComponent<Holdable>() is Holdable holdable && holdable.Attached) { continue; }
                if (IgnoreInitial && initialItemList.Contains(item)) { continue; }
                if (item.GetComponent<Projectile>() is Projectile projectile && (projectile.IsActive || projectile.User != null)) { continue; }
                if (!weakCleanOption.Matches(item) && !strongCleanOption.Matches(item)) { continue; }
                if (!RegionUtils.Contains(item.WorldPosition, includes, excludes)) { continue; }
                if (Character.CharacterList.Any(character =>
                    character.SelectedItem == item
                        || character.SelectedSecondaryItem == item
                        || character.HasItem(item))) { continue; }
                itemsToRemove.Add(item);
            }
            foreach (var pair in Tolerance.ToArray())
            {
                if (pair.Key.Removed)
                {
                    Tolerance.Remove(pair.Key);
                }
            }
            foreach (var item in itemsToRemove.ToArray())
            {
                if (!Tolerance.TryGetValue(item, out int tolerance)) { tolerance = 0; }
                if (++tolerance >= ToleranceThreshold)
                {
                    Tolerance.Remove(item);
                    item.DroppedStack.ForEachMod(stacked => Entity.Spawner.AddItemToRemoveQueue(stacked));
                    Entity.Spawner.AddItemToRemoveQueue(item);
                    continue;
                }
                else
                {
                    Tolerance[item] = tolerance;
                }
            }
            IsActive = false;
        }

        public override void ReceiveSignal(Signal signal, Connection connection)
        {
            if (IsMultiplayerClient) { return; }

            switch (connection.Name)
            {
                case "signal_in":
                    IsActive = true;
                    break;
                default:
                    break;
            }
        }
    }
}
