using System;
using EvasLearningWorld.Rules;
using UnityEngine;
using UnityEngine.UI;

namespace EvasLearningWorld.App
{
    // The inside of a building: its backdrop and one big picture tile per activity (Activities.For). Tap = play: the
    // tile's Eva line starts and the activity opens in the same call, so the child never waits for the voice.
    // Deliberately plain: no progress, no locks, no categories.
    public sealed class BuildingScreen : ScreenBase
    {
        private readonly BuildingId _building;
        private EvaGame _game;

        public BuildingScreen(BuildingId building) => _building = building;

        // Where tile `index` of a building's list is, in the canvas units of every screen's Root (also used by
        // TutorialGuide to point the hand at a tile). Valid while the menu is scrolled to the top, which it always
        // is on a fresh Build - the only caller (TutorialGuide) only ever points at a freshly-shown building's tile 0.
        public static Vector2 TilePosition(BuildingId building, int index)
        {
            var tile = TileLayout.Compute(Activities.For(building).Count)[index];
            return new Vector2(tile.X, TileLayout.AreaYMax + tile.Y);
        }

        public override void Build(EvaGame game)
        {
            _game = game;
            AddBackdrop(BackdropFor(_building));
            var activities = Activities.For(_building);
            var tiles = TileLayout.Compute(activities.Count);
            var content = AddScrollingMenu(TileLayout.ContentHeight(activities.Count));
            for (var i = 0; i < activities.Count; i++)
            {
                var activity = activities[i];
                EvaUi.IconButton(content, "Tile_" + activity.Id, EvaUi.Sprite(activity.IconSprite),
                    new Vector2(0.5f, 1f), new Vector2(tiles[i].X, tiles[i].Y), tiles[i].Side, () => Open(activity));
            }
        }

        // A vertically scrolling area the size of the safe box (TileLayout.Area...), so a building's tile list can
        // hold any number of activities. Same shape for every building - School, Playground, and Brain Gym later.
        // The ScrollRect's own rect doubles as its viewport (its `viewport` field is left unassigned), so one
        // GameObject carries the mask, the drag-input image and the ScrollRect together; only Content, sized to fit
        // every row, is a child of it.
        private RectTransform AddScrollingMenu(float contentHeight)
        {
            var menu = new GameObject("Menu", typeof(RectTransform), typeof(Image), typeof(RectMask2D), typeof(ScrollRect));
            menu.transform.SetParent(Root, false);
            var menuRect = (RectTransform)menu.transform;
            menuRect.anchorMin = menuRect.anchorMax = menuRect.pivot = new Vector2(0.5f, 0.5f);
            menuRect.sizeDelta = new Vector2(TileLayout.AreaXMax - TileLayout.AreaXMin, TileLayout.AreaYMax - TileLayout.AreaYMin);
            menuRect.anchoredPosition = new Vector2(0f, (TileLayout.AreaYMin + TileLayout.AreaYMax) / 2f);

            var menuImage = menu.GetComponent<Image>();
            menuImage.color = Color.clear; // invisible, but raycastTarget stays true so drags land on the ScrollRect
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

        public override void OnShow()
        {
            if (_building == BuildingId.School)
            {
                _game.Progress.Advance(TutorialEvent.EnteredSchool);
                _game.Commit();
            }
            _game.TutorialGuide.Refresh((ScreenId)Enum.Parse(typeof(ScreenId), _building.ToString()));
        }

        private void Open(Activity activity)
        {
            _game.Voice.Say(activity.VoiceKey);
            _game.Navigator.Show((ScreenId)Enum.Parse(typeof(ScreenId), activity.ScreenKey));
        }

        private static string BackdropFor(BuildingId building)
        {
            switch (building)
            {
                case BuildingId.Playground: return "world/playground_list_bg";
                case BuildingId.ZooFarm: return "world/zoofarm_list_bg";
                case BuildingId.ScienceLab: return "world/sciencelab_list_bg";
                case BuildingId.Workshop: return "world/workshop_list_bg";
                case BuildingId.ArtStudio: return "world/artstudio_list_bg";
                case BuildingId.FriendsPark: return "world/friendspark_list_bg";
                default: return "world/school_list_bg";
            }
        }

        // Fills exactly the safe area (see CountScreen.AddSchoolBackground for why a real picture cannot use the huge
        // solid-colour offsets).
        private void AddBackdrop(string sprite)
        {
            var background = new GameObject("Background", typeof(RectTransform), typeof(Image));
            background.transform.SetParent(Root, false);
            background.transform.SetAsFirstSibling();
            var rect = (RectTransform)background.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            FullBleed.Attach(rect);
            var image = background.GetComponent<Image>();
            image.sprite = EvaUi.Sprite(sprite);
            image.type = Image.Type.Simple;
            image.raycastTarget = false;
        }
    }
}
