using System;
using UnityEngine;

namespace PaketPanik
{
    public enum GamePhase { Lobby, Countdown, Playing, Paused, Recovery, Won, Lost, Aborted }
    public enum PlayerAction { Idle, Guard, Loot, Seal }

    [Serializable] public class GameConfig
    {
        public string specVersion = "1.0.0";
        public int players = 2, tickRate = 20, targetScore = 12;
        public float roundSeconds = 90, countdownSeconds = 3, sealHoldSeconds = 1.5f;
        public AngerConfig anger = new AngerConfig();
        public BatteryConfig battery = new BatteryConfig();
        public ItemsConfig items = new ItemsConfig();
        public float[] sneezeAt = { 30, 60 };
        public float sneezeWarningSeconds = .7f;
        public BiteConfig bite = new BiteConfig();
        public TrackingConfig tracking = new TrackingConfig();
        public AimConfig aim = new AimConfig();
        public NetConfig network = new NetConfig();
    }
    [Serializable] public class AngerConfig { public float initial = 20, maximum = 100, idlePerSecond = 6, finalPerSecond = 10, finalStartsAt = 70, perActiveLooterPerSecond = 16, guardReductionPerSecond = 26, afterBite = 30, sneezeIncrease = 25; public int maxEffectiveGuards = 1; }
    [Serializable] public class BatteryConfig { public float capacity = 100, drainPerSecond = 25, rechargePerSecond = 25, minimumToStart = 10; }
    [Serializable] public class ItemsConfig { public float firstSpawnAt = 1, spawnEverySeconds = 5, socketRadiusMeters = .23f; public int maxAlive = 3, socketCount = 6, goldEveryNthSpawn = 4; public ItemConfig regular = new ItemConfig(); public ItemConfig gold = new ItemConfig { score = 3, holdSeconds = 1.8f, angerOnCollect = 20, lifetimeSeconds = 10 }; }
    [Serializable] public class ItemConfig { public int score = 1; public float holdSeconds = 1.2f, angerOnCollect = 8, lifetimeSeconds = 12; }
    [Serializable] public class BiteConfig { public int scorePenalty = 3, maximumBites = 3; public float recoveryWallSeconds = 1.5f; }
    [Serializable] public class TrackingConfig { public float stableSeconds = .5f, staleIntentSeconds = .25f, pauseAfterSeconds = .75f, abortAfterSeconds = 15; }
    [Serializable] public class AimConfig { public float jitterGraceSeconds = .15f, minDistanceMeters = .25f, maxDistanceMeters = 1.2f; }
    [Serializable] public class NetConfig { public int port = 7777, intentRate = 10, maxIntentRate = 20, maxRttMilliseconds = 150; public float joinTimeoutSeconds = 10; }

    [Serializable] public class PlayerState
    {
        public float battery = 100, progress, guardSeconds;
        public int collected, blamed, target = -1;
        public bool tracking, ready;
        public PlayerAction action;
    }
    [Serializable] public class LootState
    {
        public int id, socket, owner = -1;
        public bool gold;
        public float expiresAt;
    }
    [Serializable] public class GameEvent
    {
        public int id, actor = -1, item = -1;
        public string type;
        public float time;
    }
    [Serializable] public class GameState
    {
        public int roundId, revision, score, bites;
        public float gameTime, anger, phaseRemaining, sealProgress;
        public GamePhase phase = GamePhase.Lobby;
        public string reason = "Hubungkan dua pemain";
        public PlayerState[] players = { new PlayerState(), new PlayerState() };
        public LootState[] items = new LootState[0];
        public GameEvent[] events = new GameEvent[0];
    }

    // A fixed-step host model. AR and networking supply validated intents, never scores.
    public sealed class GameRules
    {
        public GameConfig Config { get; }
        public GameState State { get; private set; } = new GameState();
        private readonly double[] lastSeen = { -100, -100 };
        private readonly int[] sequences = { -1, -1 };
        private readonly PlayerAction[] intents = new PlayerAction[2];
        private readonly int[] targets = { -1, -1 };
        private readonly bool[] needsRelease = new bool[2];
        private readonly double[] lootStarted = new double[2];
        private readonly bool[] wasGuarding = new bool[2];
        private double wallTime, pausedAt;
        private float nextSpawn;
        private int itemSerial, eventSerial, sneezeIndex;
        private uint random;
        private GamePhase beforePause;

        public GameRules(GameConfig config = null) { Config = config ?? new GameConfig(); }

        public void Start(uint seed)
        {
            State = new GameState { roundId = State.roundId + 1, phase = GamePhase.Countdown, phaseRemaining = Config.countdownSeconds, anger = Config.anger.initial, reason = "Satu jaga, satu ambil. Gantian!" };
            wallTime = 0; nextSpawn = Config.items.firstSpawnAt;
            itemSerial = eventSerial = sneezeIndex = 0; random = seed == 0 ? 1u : seed;
            for (int i = 0; i < 2; i++)
            {
                State.players[i].battery = Config.battery.capacity;
                State.players[i].tracking = true;
                lastSeen[i] = 0; sequences[i] = -1; targets[i] = -1;
                intents[i] = PlayerAction.Idle; needsRelease[i] = wasGuarding[i] = false;
            }
            Emit("Start");
        }

        public bool SetIntent(int player, int roundId, int sequence, PlayerAction action, int target, bool tracking)
        {
            if (player < 0 || player >= 2 || roundId != State.roundId || sequence <= sequences[player] || (int)action < 0 || (int)action > 3) return false;
            sequences[player] = sequence; lastSeen[player] = wallTime;
            State.players[player].tracking = tracking;
            if (action != PlayerAction.Guard) needsRelease[player] = false;
            if (intents[player] != PlayerAction.Loot && action == PlayerAction.Loot) lootStarted[player] = wallTime;
            intents[player] = tracking ? action : PlayerAction.Idle;
            targets[player] = target;
            return true;
        }

        public void SetReady(int player, bool ready)
        {
            if (player >= 0 && player < 2) State.players[player].ready = ready;
        }

        public void Abort(string reason)
        {
            if (State.phase == GamePhase.Aborted) return;
            CancelActions(); State.phase = GamePhase.Aborted; State.reason = reason; Emit("Abort");
        }

        public void Step(float dt)
        {
            if (dt <= 0 || dt > .25f || float.IsNaN(dt)) return;
            wallTime += dt;
            State.revision++;
            if (State.phase == GamePhase.Lobby || IsFinished) return;
            bool healthy = true;
            for (int i = 0; i < 2; i++)
                if (!State.players[i].tracking || wallTime - lastSeen[i] >= Config.tracking.pauseAfterSeconds) healthy = false;

            if (State.phase == GamePhase.Paused)
            {
                if (wallTime - pausedAt >= Config.tracking.abortAfterSeconds) { Abort("Tracking terputus. Kembali ke meja."); return; }
                if (healthy && State.players[0].ready && State.players[1].ready)
                {
                    State.phase = beforePause; State.reason = "Lanjut!"; Emit("Resume");
                }
                return;
            }
            if (!healthy)
            {
                beforePause = State.phase; State.phase = GamePhase.Paused; pausedAt = wallTime;
                CancelActions();
                foreach (var p in State.players) p.ready = false;
                State.reason = "Tracking atau koneksi terputus. Pindai ulang lalu siap."; Emit("Pause"); return;
            }

            if (State.phase == GamePhase.Countdown || State.phase == GamePhase.Recovery)
            {
                foreach (var p in State.players) p.battery = Mathf.Min(Config.battery.capacity, p.battery + Config.battery.rechargePerSecond * dt);
                State.phaseRemaining -= dt;
                if (State.phaseRemaining <= 0)
                {
                    if (State.bites >= Config.bite.maximumBites) Finish(false);
                    else { State.phase = GamePhase.Playing; State.reason = "Jaga paketnya. Curi isinya."; }
                }
                return;
            }

            bool sealing = State.score >= Config.targetScore && LiveAction(0) == PlayerAction.Seal && LiveAction(1) == PlayerAction.Seal;
            if (sealing)
            {
                CancelActions(false); State.sealProgress += dt;
                if (State.sealProgress + .0001f >= Config.sealHoldSeconds) Finish(true);
                return;
            }
            State.sealProgress = 0;
            State.gameTime += dt;
            int guardCount = 0, looterCount = 0, lastCollector = -1;
            for (int i = 0; i < 2; i++)
            {
                var p = State.players[i]; var action = LiveAction(i);
                bool guard = action == PlayerAction.Guard && !needsRelease[i] && (wasGuarding[i] || p.battery >= Config.battery.minimumToStart);
                if (guard)
                {
                    p.battery = Mathf.Max(0, p.battery - Config.battery.drainPerSecond * dt);
                    if (p.battery <= 0) { guard = false; needsRelease[i] = true; }
                }
                else p.battery = Mathf.Min(Config.battery.capacity, p.battery + Config.battery.rechargePerSecond * dt);
                wasGuarding[i] = guard;
                p.action = guard ? PlayerAction.Guard : action == PlayerAction.Guard ? PlayerAction.Idle : action;
                if (guard) { guardCount++; p.guardSeconds += dt; }
                if (p.action != PlayerAction.Loot || p.target != targets[i]) ReleaseLoot(i);
            }

            for (int i = State.items.Length - 1; i >= 0; i--)
                if (State.items[i].expiresAt <= State.gameTime) RemoveItem(State.items[i].id);
            if (State.gameTime + .0001f >= nextSpawn)
            {
                nextSpawn += Config.items.spawnEverySeconds;
                if (State.items.Length < Config.items.maxAlive) SpawnItem();
            }
            bool sneezed = sneezeIndex < Config.sneezeAt.Length && State.gameTime + .0001f >= Config.sneezeAt[sneezeIndex];
            if (sneezed)
            {
                sneezeIndex++; State.anger += Config.anger.sneezeIncrease;
                for (int i = 0; i < 2; i++) ReleaseLoot(i);
                Emit("Sneeze");
            }
            for (int i = 0; i < 2; i++)
            {
                var p = State.players[i];
                if (p.action != PlayerAction.Loot || sneezed) continue;
                var item = FindItem(targets[i]);
                if (item == null || (item.owner >= 0 && item.owner != i)) { p.action = PlayerAction.Idle; ReleaseLoot(i); continue; }
                item.owner = i; p.target = item.id; looterCount++; p.progress += dt;
                var definition = item.gold ? Config.items.gold : Config.items.regular;
                if (p.progress + .0001f >= definition.holdSeconds)
                {
                    State.score += definition.score; State.anger += definition.angerOnCollect;
                    p.collected++; lastCollector = i; int id = item.id;
                    RemoveItem(id); Emit("Collect", i, id);
                }
            }
            float baseline = State.gameTime >= Config.anger.finalStartsAt ? Config.anger.finalPerSecond : Config.anger.idlePerSecond;
            State.anger = Mathf.Clamp(State.anger + dt * (baseline + Config.anger.perActiveLooterPerSecond * looterCount - Config.anger.guardReductionPerSecond * Mathf.Min(Config.anger.maxEffectiveGuards, guardCount)), 0, Config.anger.maximum);
            if (State.anger >= Config.anger.maximum) Bite(lastCollector);
            else if (State.gameTime + .0001f >= Config.roundSeconds) Finish(State.score >= Config.targetScore);
        }

        public bool IsFinished => State.phase == GamePhase.Won || State.phase == GamePhase.Lost || State.phase == GamePhase.Aborted;
        public LootState FindItem(int id) { foreach (var item in State.items) if (item.id == id) return item; return null; }
        public Vector3 SocketPosition(int socket) => PositionForSocket(socket, Config.items.socketCount, Config.items.socketRadiusMeters);
        public static Vector3 PositionForSocket(int socket, int count = 6, float radius = .23f)
        {
            float a = socket * 2f * Mathf.PI / count;
            return new Vector3(Mathf.Cos(a) * radius, .055f, Mathf.Sin(a) * radius);
        }
        private PlayerAction LiveAction(int player) => wallTime - lastSeen[player] <= Config.tracking.staleIntentSeconds && State.players[player].tracking ? intents[player] : PlayerAction.Idle;
        private void ReleaseLoot(int player)
        {
            foreach (var item in State.items) if (item.owner == player) item.owner = -1;
            State.players[player].target = -1; State.players[player].progress = 0;
        }
        private void CancelActions(bool resetSeal = true)
        {
            for (int i = 0; i < 2; i++) { ReleaseLoot(i); State.players[i].action = PlayerAction.Idle; wasGuarding[i] = false; }
            if (resetSeal) State.sealProgress = 0;
        }
        private void SpawnItem()
        {
            random ^= random << 13; random ^= random >> 17; random ^= random << 5;
            int socket = (int)(random % (uint)Config.items.socketCount);
            for (int n = 0; n < Config.items.socketCount; n++)
            {
                bool occupied = false;
                foreach (var existing in State.items) if (existing.socket == socket) occupied = true;
                if (!occupied) break;
                socket = (socket + 1) % Config.items.socketCount;
            }
            int serial = ++itemSerial;
            bool gold = serial % Config.items.goldEveryNthSpawn == 0;
            var items = State.items; Array.Resize(ref items, items.Length + 1);
            items[items.Length - 1] = new LootState { id = serial, socket = socket, gold = gold, expiresAt = State.gameTime + (gold ? Config.items.gold.lifetimeSeconds : Config.items.regular.lifetimeSeconds) };
            State.items = items; Emit("Spawn", -1, serial);
        }
        private void RemoveItem(int id)
        {
            int index = Array.FindIndex(State.items, x => x.id == id);
            if (index < 0) return;
            for (int p = 0; p < 2; p++) if (State.players[p].target == id) ReleaseLoot(p);
            var items = new LootState[State.items.Length - 1];
            if (index > 0) Array.Copy(State.items, 0, items, 0, index);
            if (index < items.Length) Array.Copy(State.items, index + 1, items, index, items.Length - index);
            State.items = items;
        }
        private void Bite(int actor)
        {
            if (actor < 0)
            {
                for (int i = 0; i < 2; i++) if (State.players[i].action == PlayerAction.Loot && (actor < 0 || lootStarted[i] > lootStarted[actor])) actor = i;
                if (actor < 0) actor = State.players[0].guardSeconds <= State.players[1].guardSeconds ? 0 : 1;
            }
            State.players[actor].blamed++; State.bites++;
            State.score = Mathf.Max(0, State.score - Config.bite.scorePenalty);
            State.anger = Config.anger.afterBite; State.phase = GamePhase.Recovery;
            State.phaseRemaining = Config.bite.recoveryWallSeconds;
            State.reason = "KATANYA AMAN! Isi tas berkurang.";
            CancelActions(); Emit("Bite", actor);
        }
        private void Finish(bool won)
        {
            CancelActions(); State.phase = won ? GamePhase.Won : GamePhase.Lost;
            State.reason = won ? "PAKET BERHASIL DISEGEL!" : "DIKEMBALIKAN KE PENGIRIM";
            Emit(won ? "Win" : "Lose");
        }
        private void Emit(string type, int actor = -1, int item = -1)
        {
            var ev = new GameEvent { id = ++eventSerial, type = type, actor = actor, item = item, time = State.gameTime };
            int count = Mathf.Min(State.events.Length + 1, 12);
            var recent = new GameEvent[count];
            int oldCount = count - 1;
            if (oldCount > 0) Array.Copy(State.events, State.events.Length - oldCount, recent, 0, oldCount);
            recent[count - 1] = ev; State.events = recent;
        }
    }
}
