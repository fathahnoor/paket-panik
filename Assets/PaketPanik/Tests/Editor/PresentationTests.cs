using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;

namespace PaketPanik.Tests
{
    public class PresentationTests
    {
        [TestCase(90f, "01:30")]
        [TestCase(60f, "01:00")]
        [TestCase(59.1f, "01:00")]
        [TestCase(.1f, "00:01")]
        [TestCase(0f, "00:00")]
        [TestCase(-1f, "00:00")]
        public void CountdownUsesOneRoundedTotal(float seconds, string expected)
        { Assert.That(PanicInterface.FormatTime(seconds), Is.EqualTo(expected)); }

        [TestCase(false, .6f, .5f)]
        [TestCase(true, .9f, .5f)]
        [TestCase(true, 3f, 1f)]
        public void LootMeterRespectsEachItemsHoldDuration(bool gold, float seconds, float expected)
        {
            var state = new GameState { items = new[] { new LootState { id = 7, gold = gold } } };
            var player = new PlayerState { action = PlayerAction.Loot, target = 7, progress = seconds };
            Assert.That(PanicInterface.LootFraction(player, state, new GameConfig(), 7), Is.EqualTo(expected).Within(.001f));
        }

        [Test]
        public void ANewTargetOrNonLootActionDoesNotReuseOldProgress()
        {
            var state = new GameState { items = new[] { new LootState { id = 7 }, new LootState { id = 8 } } };
            var player = new PlayerState { action = PlayerAction.Loot, target = 7, progress = 1 };
            Assert.That(PanicInterface.LootFraction(player, state, new GameConfig(), 8), Is.Zero);
            player.action = PlayerAction.Guard;
            Assert.That(PanicInterface.LootFraction(player, state, new GameConfig(), 7), Is.Zero);
        }

        [Test]
        public void HoldIgnoresAnotherFingerAndAllowsExplicitCancellation()
        {
            var go = new GameObject("Hold test");
            try
            {
                var hold = go.AddComponent<HoldControl>();
                hold.OnPointerDown(new PointerEventData(null) { pointerId = 1 });
                hold.OnPointerDown(new PointerEventData(null) { pointerId = 2 });
                hold.OnPointerUp(new PointerEventData(null) { pointerId = 2 });
                Assert.That(hold.Held, Is.True);
                hold.OnPointerExit(new PointerEventData(null) { pointerId = 1 });
                Assert.That(hold.Held, Is.False);
                hold.OnPointerDown(new PointerEventData(null) { pointerId = 1 });
                hold.Release();
                Assert.That(hold.Held, Is.False);
            }
            finally { Object.DestroyImmediate(go); }
        }
    }
}
