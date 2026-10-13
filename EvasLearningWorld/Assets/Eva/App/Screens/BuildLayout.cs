using System;
using EvasLearningWorld.Rules;
using UnityEngine;

namespace EvasLearningWorld.App
{
    // Where every part of a Workshop build sits in the finished picture (written by tools/art-import/import-workshop-build.js next to the
    // part pictures). Units are pixels of the finished picture; x, y is the top-left corner of the part's sprite.
    [Serializable]
    public sealed class BuildPart
    {
        public string part;
        public int x, y, w, h;
    }

    [Serializable]
    public sealed class BuildLayout
    {
        public string game;
        public int width, height;
        public int pad;          // the dotted outline sprite is this much bigger than the part on every side
        public BuildPart[] parts;

        public static string GameName(WorkshopBuildKind kind) => kind.ToString().ToLowerInvariant();

        public static BuildLayout Load(WorkshopBuildKind kind)
        {
            var asset = Resources.Load<TextAsset>("Art/workshop/build_" + GameName(kind));
            return asset == null ? null : JsonUtility.FromJson<BuildLayout>(asset.text);
        }
    }
}
