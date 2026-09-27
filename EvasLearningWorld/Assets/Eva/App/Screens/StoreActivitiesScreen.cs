using EvasLearningWorld.Rules;
using UnityEngine;
using UnityEngine.UI;

namespace EvasLearningWorld.App
{
    // The Store's own small activity menu (M4.3 dressing cluster onward): one tile per non-shelf Store
    // activity (Shopping, then the dressing games as they are built). Reuses BuildingScreen's tile-grid math
    // (TileLayout, the same scrolling Content rect) but is not a BuildingScreen itself and Store's activities
    // are not entries in Activities.cs/BuildingId: BuildingScreen.OnShow assumes its own ScreenId is named
    // exactly after its BuildingId (`(ScreenId)Enum.Parse(..., building.ToString())`), and ScreenId.Store is
    // already the Furniture Store's own shelf scene - adding BuildingId.Store would collide with that existing
    // registration, and repointing it would touch MapScreen's Store button and the FirstPurchase tutorial step
    // for no real benefit. A second, deliberately separate list here avoids all of that risk while still
    // scaling cleanly to however many Store activities the dressing cluster ends up needing. Reached via a
    // small icon button on StoreScreen's own shelf (see StoreScreen's class comment on that button).
    public sealed class StoreActivitiesScreen : ScreenBase
    {
        private sealed class Entry
        {
            public string Id, IconSprite, VoiceKey;
            public ScreenId Screen;
        }

        private static readonly Entry[] Entries =
        {
            new Entry { Id = "shopping", Screen = ScreenId.Shopping, IconSprite = "activities/shopping", VoiceKey = "activity_shopping" },
            new Entry { Id = "dress_the_character", Screen = ScreenId.DressTheCharacter, IconSprite = "activities/dress_the_character", VoiceKey = "activity_dress_the_character" },
            new Entry { Id = "dress_for_occasion", Screen = ScreenId.DressForOccasion, IconSprite = "activities/dress_for_occasion", VoiceKey = "activity_dress_for_occasion" },
            new Entry { Id = "pack_a_suitcase", Screen = ScreenId.PackASuitcase, IconSprite = "activities/pack_a_suitcase", VoiceKey = "activity_pack_a_suitcase" },
        };

        private EvaGame _game;

        public override void Build(EvaGame game)
        {
            _game = game;
            AddBackdrop();
            var tiles = TileLayout.Compute(Entries.Length);
            var content = AddScrollingMenu(TileLayout.ContentHeight(Entries.Length));
            for (var i = 0; i < Entries.Length; i++)
            {
                var entry = Entries[i];
                EvaUi.IconButton(content, "Tile_" + entry.Id, EvaUi.Sprite(entry.IconSprite),
                    new Vector2(0.5f, 1f), new Vector2(tiles[i].X, tiles[i].Y), tiles[i].Side, () => Open(entry));
            }
        }

        public override void OnShow() => _game.TutorialGuide.Refresh(ScreenId.StoreActivities);

        private void Open(Entry entry)
        {
            _game.Voice.Say(entry.VoiceKey);
            _game.Navigator.Show(entry.Screen);
        }

        // --- Layout (copied from BuildingScreen's own AddScrollingMenu/AddBackdrop - see there for the
        // reasoning; not shared as a base class since BuildingScreen's Open/OnShow are tied to Activities.cs's
        // BuildingId-keyed data, which this screen deliberately doesn't use) --------------------------------

        private RectTransform AddScrollingMenu(float contentHeight)
        {
            var menu = new GameObject("Menu", typeof(RectTransform), typeof(Image), typeof(RectMask2D), typeof(ScrollRect), typeof(TapTarget));
            menu.transform.SetParent(Root, false);
            var menuRect = (RectTransform)menu.transform;
            menuRect.anchorMin = menuRect.anchorMax = menuRect.pivot = new Vector2(0.5f, 0.5f);
            menuRect.sizeDelta = new Vector2(TileLayout.AreaXMax - TileLayout.AreaXMin, TileLayout.AreaYMax - TileLayout.AreaYMin);
            menuRect.anchoredPosition = new Vector2(0f, (TileLayout.AreaYMin + TileLayout.AreaYMax) / 2f);

            var menuImage = menu.GetComponent<Image>();
            menuImage.color = Color.clear;
            menuImage.raycastTarget = true;

            var contentObject = new GameObject("Content", typeof(RectTransform));
            contentObject.transform.SetParent(menu.transform, false);
            var content = (RectTransform)contentObject.transform;
            content.anchorMin = content.anchorMax = content.pivot = new Vector2(0.5f, 1f);
            content.sizeDelta = new Vector2(TileLayout.AreaXMax - TileLayout.AreaXMin, contentHeight);
            content.anchoredPosition = Vector2.zero;

            var scrollRect = menu.GetComponent<ScrollRect>();
            scrollRect.content = content;
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            return content;
        }

        private void AddBackdrop()
        {
            var background = new GameObject("Background", typeof(RectTransform), typeof(Image));
            background.transform.SetParent(Root, false);
            background.transform.SetAsFirstSibling();
            var rect = (RectTransform)background.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            FullBleed.Attach(rect);
            var image = background.GetComponent<Image>();
            image.sprite = EvaUi.Sprite("world/store_bg");
            image.type = Image.Type.Simple;
            image.raycastTarget = false;
        }
    }
}
