using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EvasLearningWorld.Tests
{
    // Small black-box helpers shared by the Count screen's behaviour and audit tests. Everything here drives and
    // inspects CountScreen only through its public GameObject hierarchy and components (Find, Button.onClick,
    // TMP text) - the same way NoReadingAuditTests already taps the map buttons. No reflection, no test-only
    // hooks added to production code.
    internal static class CountScreenTestSupport
    {
        public static Transform Screen(Transform canvasRoot) => canvasRoot.Find("ScreenRoot/CountScreen");

        public static int ObjectCount(Transform canvasRoot) => Screen(canvasRoot).Find("ObjectField").childCount;

        // Tiles shown this round (3 at levels 1-2, 5 at levels 3-4, 6 at levels 5-6); the spare tiles stays built but inactive.
        public static int TileCount(Transform canvasRoot)
        {
            var count = 0;
            foreach (Transform tile in Screen(canvasRoot).Find("AnswerField"))
                if (tile.gameObject.activeSelf) count++;
            return count;
        }

        public static int[] TileValues(Transform canvasRoot)
        {
            var screen = Screen(canvasRoot);
            var values = new int[TileCount(canvasRoot)];
            for (var i = 0; i < values.Length; i++)
            {
                var text = screen.Find("AnswerField/Tile" + i + "/Numeral").GetComponent<TextMeshProUGUI>().text;
                values[i] = int.Parse(text, CultureInfo.InvariantCulture);
            }
            return values;
        }

        public static bool TileInteractable(Transform canvasRoot, int i) =>
            Screen(canvasRoot).Find("AnswerField/Tile" + i).GetComponent<Button>().interactable;

        public static Color TileColor(Transform canvasRoot, int i) =>
            Screen(canvasRoot).Find("AnswerField/Tile" + i).GetComponent<Image>().color;

        public static void TapTile(Transform canvasRoot, int i) =>
            Screen(canvasRoot).Find("AnswerField/Tile" + i).GetComponent<Button>().onClick.Invoke();

        public static void TapObject(Transform canvasRoot, int i) =>
            Screen(canvasRoot).Find("ObjectField/Object" + i).GetComponent<Button>().onClick.Invoke();

        // Objects the child is asked to count. Levels 5-6 mix in distractors of other types, so the slot count
        // overstates the answer there. Distractors are exactly the slots whose tap never gains a tick badge; when
        // the field is mixed we tap every slot once (harmless: it only counts objects and wobbles distractors) and
        // count the badges. A single-type field (levels 1-4) is just the slot count, with no taps.
        public static int AskedObjectCount(Transform canvasRoot)
        {
            var field = Screen(canvasRoot).Find("ObjectField");
            string firstSprite = null;
            var mixed = false;
            foreach (Transform slot in field)
            {
                var name = slot.Find("Icon").GetComponent<Image>().sprite.name;
                if (firstSprite == null) firstSprite = name;
                else if (name != firstSprite) mixed = true;
            }
            if (!mixed) return field.childCount;
            var asked = 0;
            for (var i = 0; i < field.childCount; i++)
            {
                TapObject(canvasRoot, i);
                if (field.Find("Object" + i + "/Badge").gameObject.activeSelf) asked++;
            }
            return asked;
        }

        public static bool AnyTileInteractable(Transform canvasRoot)
        {
            for (var i = 0; i < TileCount(canvasRoot); i++)
                if (TileInteractable(canvasRoot, i)) return true;
            return false;
        }

        public static bool SessionEndVisible(Transform canvasRoot) => Screen(canvasRoot).Find("EndPanel").gameObject.activeSelf;

        public static int CorrectTileIndex(Transform canvasRoot)
        {
            var quantity = AskedObjectCount(canvasRoot);
            var values = TileValues(canvasRoot);
            for (var i = 0; i < values.Length; i++)
                if (values[i] == quantity) return i;
            throw new InvalidOperationException("no tile matches the object count");
        }

        public static int[] WrongTileIndices(Transform canvasRoot)
        {
            var quantity = AskedObjectCount(canvasRoot);
            var values = TileValues(canvasRoot);
            var wrong = new List<int>();
            for (var i = 0; i < values.Length; i++)
                if (values[i] != quantity) wrong.Add(i);
            return wrong.ToArray();
        }

        // Polls once per frame (real time: the Count screen's help coroutines use WaitForSeconds and real voice
        // clip lengths, which do not fast-forward in edit-mode tests) until `condition` is true, failing the
        // test if `timeoutSeconds` of real time passes first.
        //
        // CountScreen's own coroutines run on a plain MonoBehaviour (Runner) via StartCoroutine, which - unlike
        // a [UnityTest] method's own enumerator - is not reliably pumped by Unity's edit-mode player loop; it
        // only advances on incidental editor repaints, which can be tens of seconds apart in a headless batch
        // run. Explicitly requesting a player loop update on every poll is the standard workaround (see Unity's
        // own EditorApplication.QueuePlayerLoopUpdate docs) and is what makes every Count-screen coroutine in
        // these tests actually progress each frame instead of stalling.
        public static IEnumerator WaitUntil(Func<bool> condition, float timeoutSeconds, string what)
        {
            var start = Time.realtimeSinceStartup;
            while (!condition())
            {
                if (Time.realtimeSinceStartup - start > timeoutSeconds)
                    Assert.Fail("timed out after " + timeoutSeconds + "s waiting for: " + what);
                UnityEditor.EditorApplication.QueuePlayerLoopUpdate();
                yield return null;
            }
        }

        // One forced-pump frame (see WaitUntil's comment on why this is needed at all): use this wherever a
        // test does `yield return null;` expecting a Runner-hosted CountScreen coroutine to have taken its
        // next step by the following line.
        public static IEnumerator Tick()
        {
            UnityEditor.EditorApplication.QueuePlayerLoopUpdate();
            yield return null;
        }

        // Taps the correct tile for the round currently showing (the question phase must already be
        // interactable) and waits for either the next round's question to finish (a tile becomes interactable
        // again) or the end-of-session panel to appear.
        public static IEnumerator PlayRoundCorrectly(Transform canvasRoot, float timeoutSeconds)
        {
            yield return WaitUntil(() => AnyTileInteractable(canvasRoot), timeoutSeconds, "a tile to become interactable");
            TapTile(canvasRoot, CorrectTileIndex(canvasRoot));
            yield return WaitUntil(() => SessionEndVisible(canvasRoot) || AnyTileInteractable(canvasRoot), timeoutSeconds, "next round or session end");
        }
    }
}
