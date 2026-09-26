using System.Collections.Generic;
using EvasLearningWorld.Rules;
using UnityEngine;
using UnityEngine.UI;

namespace EvasLearningWorld.App
{
    // Draws a Finger Maze corridor (Rules/FingerMaze.cs) as a decorative backdrop: a rounded tile at every
    // waypoint plus a stretched tile along every segment between them, with a finish flag on the last point.
    // Shared by every Playground NAVIGATION game (Finger Maze itself, Follow Numbers/Letters in Order, Shortest
    // Path, Avoid Obstacles, Collect Everything) so the corridor always reads the same way regardless of what
    // each game puts on top of it (a dragged character, numbered/lettered checkpoints, pickups, hazards).
    public static class MazeCorridorRenderer
    {
        public const float CorridorWidth = 150f;

        // Clears `field`'s children and redraws the corridor for `path`; returns the segment tiles in path order
        // (segment i joins path[i] to path[i+1]) so a caller can glow one for its own Hint step.
        public static List<Image> Draw(RectTransform field, WorldPoint[] path, bool withFinishFlag = true)
        {
            ClearChildren(field);
            var segments = new List<Image>();
            for (var i = 1; i < path.Length; i++)
                segments.Add(BuildTile(field, "Segment", Midpoint(path[i - 1], path[i]),
                    new Vector2(Distance(path[i - 1], path[i]), CorridorWidth), SegmentAngle(path[i - 1], path[i])));
            foreach (var point in path)
                BuildTile(field, "Waypoint", new Vector2(point.X, point.Y), new Vector2(CorridorWidth, CorridorWidth), 0f);

            if (withFinishFlag)
            {
                var finish = path[path.Length - 1];
                var flag = BuildTile(field, "FinishFlag", new Vector2(finish.X, finish.Y), new Vector2(CorridorWidth, CorridorWidth), 0f);
                flag.sprite = EvaUi.Sprite("fingermaze/finish");
                flag.preserveAspect = true;
            }
            return segments;
        }

        // Cumulative length fraction at each waypoint (0 at the start, 1 at the finish) - lets a Hint find which
        // segment lies just ahead of a given progress fraction.
        public static float[] WaypointFractions(WorldPoint[] path)
        {
            var total = MapPath.Length(path);
            var fractions = new float[path.Length];
            var walked = 0f;
            for (var i = 0; i < path.Length; i++)
            {
                if (i > 0) walked += Distance(path[i - 1], path[i]);
                fractions[i] = total <= 0f ? 0f : walked / total;
            }
            return fractions;
        }

        private static Image BuildTile(RectTransform field, string name, Vector2 position, Vector2 size, float angleDegrees)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(field, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            rect.localRotation = Quaternion.Euler(0f, 0f, angleDegrees);

            var image = go.GetComponent<Image>();
            image.sprite = EvaUi.Sprite("fingermaze/corridor");
            image.raycastTarget = false;
            return image;
        }

        private static Vector2 Midpoint(WorldPoint a, WorldPoint b) => new Vector2((a.X + b.X) / 2f, (a.Y + b.Y) / 2f);
        private static float SegmentAngle(WorldPoint a, WorldPoint b) => Mathf.Atan2(b.Y - a.Y, b.X - a.X) * Mathf.Rad2Deg;
        private static float Distance(WorldPoint a, WorldPoint b) => Mathf.Sqrt((b.X - a.X) * (b.X - a.X) + (b.Y - a.Y) * (b.Y - a.Y));

        private static void ClearChildren(Transform parent)
        {
            for (var i = parent.childCount - 1; i >= 0; i--)
            {
                var child = parent.GetChild(i).gameObject;
                if (Application.isPlaying) Object.Destroy(child);
                else Object.DestroyImmediate(child);
            }
        }
    }
}
