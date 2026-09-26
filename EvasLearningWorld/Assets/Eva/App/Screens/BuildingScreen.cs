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
        // TutorialGuide to point the hand at a tile).
        public static Vector2 TilePosition(BuildingId building, int index)
        {
            var tile = TileLayout.Compute(Activities.For(building).Count)[index];
            return new Vector2(tile.X, tile.Y);
        }

        public override void Build(EvaGame game)
        {
            _game = game;
            AddBackdrop(BackdropFor(_building));
            var activities = Activities.For(_building);
            var tiles = TileLayout.Compute(activities.Count);
            for (var i = 0; i < activities.Count; i++)
            {
                var activity = activities[i];
                EvaUi.IconButton(Root, "Tile_" + activity.Id, EvaUi.Sprite(activity.IconSprite),
                    new Vector2(0.5f, 0.5f), new Vector2(tiles[i].X, tiles[i].Y), tiles[i].Side, () => Open(activity));
            }
        }

        public override void OnShow()
        {
            if (_building == BuildingId.School)
            {
                _game.Progress.Advance(TutorialEvent.EnteredSchool);
                _game.Commit();
            }
            _game.TutorialGuide.Refresh(ScreenId.School);
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
