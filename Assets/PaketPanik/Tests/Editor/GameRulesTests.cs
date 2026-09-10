using NUnit.Framework;
using UnityEngine;

namespace PaketPanik.Tests
{
    public class GameRulesTests
    {
        private GameRules game;
        private int seq;
        [SetUp] public void Setup()
        {
            game = new GameRules(new GameConfig { countdownSeconds = 0 }); game.Start(17); seq = 0;
            Tick(PlayerAction.Idle, PlayerAction.Idle);
        }
        private void Tick(PlayerAction a, PlayerAction b, int targetA = -1, int targetB = -1)
        {
            game.SetIntent(0, game.State.roundId, ++seq, a, targetA, true);
            game.SetIntent(1, game.State.roundId, ++seq, b, targetB, true);
            game.Step(.05f);
        }
        private void Ticks(int count, PlayerAction a = PlayerAction.Idle, PlayerAction b = PlayerAction.Guard, int targetA = -1, int targetB = -1)
        {
            for (int i = 0; i < count; i++) Tick(a, b, targetA, targetB);
        }
        [Test] public void SameItemTwoPlayersAwardsOnce()
        {
            Ticks(21); var id = game.State.items[0].id;
            Ticks(25, PlayerAction.Loot, PlayerAction.Loot, id, id);
            Assert.That(game.State.score, Is.EqualTo(1));
            Assert.That(game.State.players[0].collected + game.State.players[1].collected, Is.EqualTo(1));
            Assert.That(game.FindItem(id), Is.Null);
        }
        [Test] public void ReleasingHoldClearsLockAndProgress()
        {
            Ticks(21); int id = game.State.items[0].id;
            Ticks(10, PlayerAction.Loot, PlayerAction.Guard, id);
            Assert.That(game.State.players[0].progress, Is.GreaterThan(0));
            Tick(PlayerAction.Idle, PlayerAction.Guard);
            Assert.That(game.FindItem(id).owner, Is.EqualTo(-1));
            Assert.That(game.State.players[0].progress, Is.Zero);
        }
        [Test] public void EmptyBatteryRequiresRelease()
        {
            Ticks(82, PlayerAction.Guard, PlayerAction.Idle);
            Assert.That(game.State.players[0].action, Is.EqualTo(PlayerAction.Idle));
            Ticks(15, PlayerAction.Guard, PlayerAction.Guard);
            Assert.That(game.State.players[0].action, Is.EqualTo(PlayerAction.Idle));
            Tick(PlayerAction.Idle, PlayerAction.Guard);
            Tick(PlayerAction.Guard, PlayerAction.Idle);
            Assert.That(game.State.players[0].action, Is.EqualTo(PlayerAction.Guard));
        }
        [Test] public void BiteHappensOnceAndNeverMakesNegativeScore()
        {
            game.State.anger = 99.9f;
            Tick(PlayerAction.Idle, PlayerAction.Idle);
            Assert.That(game.State.bites, Is.EqualTo(1)); Assert.That(game.State.score, Is.Zero);
            Ticks(10, PlayerAction.Idle, PlayerAction.Idle);
            Assert.That(game.State.bites, Is.EqualTo(1));
            Assert.That(game.State.phase, Is.EqualTo(GamePhase.Recovery));
        }
        [Test] public void MissingInputPausesAndDoesNotAdvanceTime()
        {
            for (int i = 0; i < 18; i++) game.Step(.05f);
            Assert.That(game.State.phase, Is.EqualTo(GamePhase.Paused));
            float t = game.State.gameTime, anger = game.State.anger;
            for (int i = 0; i < 20; i++) game.Step(.05f);
            Assert.That(game.State.gameTime, Is.EqualTo(t)); Assert.That(game.State.anger, Is.EqualTo(anger));
        }
        [Test] public void PauseNeedsBothReadyAndFreshTracking()
        {
            for (int i = 0; i < 18; i++) game.Step(.05f);
            game.SetReady(0, true); Tick(PlayerAction.Idle, PlayerAction.Idle);
            Assert.That(game.State.phase, Is.EqualTo(GamePhase.Paused));
            game.SetReady(1, true); Tick(PlayerAction.Idle, PlayerAction.Idle);
            Assert.That(game.State.phase, Is.EqualTo(GamePhase.Playing));
        }
        [Test] public void OldRoundAndDuplicateSequenceRejected()
        {
            int old = game.State.roundId; game.Start(12);
            Assert.That(game.SetIntent(0, old, 500, PlayerAction.Loot, 1, true), Is.False);
            Assert.That(game.SetIntent(0, game.State.roundId, 2, PlayerAction.Idle, -1, true), Is.True);
            Assert.That(game.SetIntent(0, game.State.roundId, 2, PlayerAction.Guard, -1, true), Is.False);
        }
        [Test] public void SealRequiresQuotaAndBothPlayers()
        {
            Ticks(35, PlayerAction.Seal, PlayerAction.Seal);
            Assert.That(game.IsFinished, Is.False);
            game.State.score = 12;
            Ticks(35, PlayerAction.Seal, PlayerAction.Guard);
            Assert.That(game.IsFinished, Is.False);
            Ticks(31, PlayerAction.Seal, PlayerAction.Seal);
            Assert.That(game.State.phase, Is.EqualTo(GamePhase.Won));
        }
        [Test] public void TimeoutResolvesQuota()
        {
            game.State.gameTime = 89.98f; game.State.score = 12;
            Tick(PlayerAction.Idle, PlayerAction.Guard);
            Assert.That(game.State.phase, Is.EqualTo(GamePhase.Won));
            game.Start(5); Tick(PlayerAction.Idle, PlayerAction.Idle);
            game.State.gameTime = 89.98f;
            Tick(PlayerAction.Idle, PlayerAction.Guard);
            Assert.That(game.State.phase, Is.EqualTo(GamePhase.Lost));
        }
        [Test] public void ItemsExpireAndGoldIsEveryFourthSpawn()
        {
            var c = new GameConfig { countdownSeconds = 0 }; c.items.spawnEverySeconds = .2f;
            c.items.maxAlive = 6; c.items.regular.lifetimeSeconds = 2;
            game = new GameRules(c); game.Start(1); Tick(PlayerAction.Idle, PlayerAction.Idle);
            Ticks(34); Assert.That(game.State.items[3].gold, Is.True);
            int id = game.State.items[0].id; Ticks(50, PlayerAction.Guard, PlayerAction.Idle);
            Assert.That(game.FindItem(id), Is.Null);
        }
        [Test] public void SeparateArOriginsGiveSameBoardPosition()
        {
            var p = GameRules.PositionForSocket(2);
            var a = Matrix4x4.TRS(new Vector3(3, 1, 2), Quaternion.Euler(0, 75, 0), Vector3.one);
            var b = Matrix4x4.TRS(new Vector3(-8, .5f, 4), Quaternion.Euler(0, -30, 0), Vector3.one);
            Assert.That(Vector3.Distance(a.inverse.MultiplyPoint3x4(a.MultiplyPoint3x4(p)), b.inverse.MultiplyPoint3x4(b.MultiplyPoint3x4(p))), Is.LessThan(.0001f));
        }
    }
}
