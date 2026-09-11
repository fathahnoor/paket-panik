using System;
using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using Unity.Collections;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

namespace PaketPanik
{
    [Serializable] public class PlayerIntent
    {
        public int round, sequence, action, target = -1;
        public bool tracking, ready;
        public Vector3 origin, direction;
    }
    [Serializable] public class RoomSnapshot
    {
        public int count;
        public GameState game;
    }

    public sealed class LanSession : MonoBehaviour
    {
        public GameConfig Config { get; private set; }
        public GameState State => manager != null && manager.IsHost ? rules.State : remoteState;
        public int LocalSlot => manager != null && manager.IsHost ? 0 : 1;
        public bool Hosting => manager != null && manager.IsHost;
        public bool Connected => manager != null && manager.IsConnectedClient;
        public bool Connecting => connecting;
        public int PlayerCount => Hosting ? manager.ConnectedClientsIds.Count : remoteCount;
        public string Status { get; private set; } = "Buat meja atau gabung teman.";
        public string Pin { get; private set; } = "";
        public string MarkerHash { get; private set; } = "";
        public bool LocalReady { get; set; }
        public Func<PlayerIntent> GetLocalIntent;
        public event Action<GameEvent> GameEventReceived;
        private NetworkManager manager;
        private UnityTransport transport;
        private GameRules rules;
        private GameState remoteState = new GameState();
        private int remoteCount, sequence, observedRound = -1, observedEvent;
        private float accumulator, sendClock, sinceSnapshot, connectingTime;
        private bool handlersRegistered, connecting, intentionalStop;
        private readonly Dictionary<ulong, float> inputTimes = new Dictionary<ulong, float>();
        private const string InputName = "pp.input.v1", StateName = "pp.state.v1";

        private void Awake()
        {
            var config = Resources.Load<TextAsset>("PaketPanik/balance");
            Config = config ? JsonUtility.FromJson<GameConfig>(config.text) : new GameConfig();
            var markerHash = Resources.Load<TextAsset>("PaketPanik/marker-hash");
            MarkerHash = markerHash ? markerHash.text.Trim() : "marker-not-built";
            rules = new GameRules(Config);
            manager = GetComponent<NetworkManager>();
            if (!manager) manager = gameObject.AddComponent<NetworkManager>();
            transport = GetComponent<UnityTransport>();
            if (!transport) transport = gameObject.AddComponent<UnityTransport>();
            manager.NetworkConfig = new NetworkConfig { NetworkTransport = transport, EnableSceneManagement = false, ConnectionApproval = true, TickRate = (uint)Config.tickRate };
            manager.ConnectionApprovalCallback = Approve;
            manager.OnClientConnectedCallback += OnConnected;
            manager.OnClientDisconnectCallback += OnDisconnected;
        }

        public bool Host()
        {
            if (manager.IsListening) return false;
            using (var rng = RandomNumberGenerator.Create())
            {
                byte[] bytes = new byte[4]; rng.GetBytes(bytes);
                Pin = (100000 + BitConverter.ToUInt32(bytes, 0) % 900000).ToString();
            }
            Prepare(); transport.SetConnectionData("127.0.0.1", (ushort)Config.network.port, "0.0.0.0");
            manager.NetworkConfig.ConnectionData = Encoding.UTF8.GetBytes("pp1|" + Pin + "|" + MarkerHash);
            bool ok = manager.StartHost();
            if (ok) { RegisterHandlers(); Status = "Meja dibuat. Bagikan IP dan PIN kepada teman."; }
            else Status = "Meja gagal dibuat. Port mungkin sedang dipakai.";
            return ok;
        }

        public bool Join(string address, string pin)
        {
            if (manager.IsListening) return false;
            if (!IsLocalAddress(address) || pin == null || pin.Length != 6 || !int.TryParse(pin, out _))
            { Status = "Isi IPv4 Wi-Fi teman dan PIN enam angka."; return false; }
            Prepare(); Pin = pin;
            transport.SetConnectionData(address.Trim(), (ushort)Config.network.port);
            manager.NetworkConfig.ConnectionData = Encoding.UTF8.GetBytes("pp1|" + Pin + "|" + MarkerHash);
            bool ok = manager.StartClient();
            if (ok) { RegisterHandlers(); connecting = true; Status = "Menghubungkan ke meja teman..."; }
            else Status = "Koneksi gagal dimulai.";
            return ok;
        }

        public void Leave()
        {
            intentionalStop = true; connecting = false;
            if (manager.IsListening) manager.Shutdown();
            handlersRegistered = false; LocalReady = false;
            remoteCount = 0; remoteState = new GameState(); rules = new GameRules(Config);
            Status = "Keluar dari meja. Buat atau gabung lagi.";
        }
        public bool StartRound()
        {
            if (!Hosting || PlayerCount != 2 || !rules.State.players[0].ready || !rules.State.players[1].ready) return false;
            if (rules.State.phase != GamePhase.Lobby && !rules.IsFinished) return false;
            rules.Start((uint)Environment.TickCount); LocalReady = false; SendSnapshot(); return true;
        }
        private void Prepare()
        {
            intentionalStop = false; connectingTime = sinceSnapshot = accumulator = sendClock = 0;
            LocalReady = false; sequence = 0; observedRound = -1; observedEvent = 0;
            rules = new GameRules(Config); remoteState = new GameState(); inputTimes.Clear();
        }
        private void RegisterHandlers()
        {
            if (handlersRegistered) return;
            manager.CustomMessagingManager.RegisterNamedMessageHandler(InputName, ReceiveInput);
            manager.CustomMessagingManager.RegisterNamedMessageHandler(StateName, ReceiveSnapshot);
            handlersRegistered = true;
        }
        private void Approve(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
        {
            bool validPayload = request.Payload != null && request.Payload.Length <= 160 && Encoding.UTF8.GetString(request.Payload) == "pp1|" + Pin + "|" + MarkerHash;
            bool open = rules.State.phase == GamePhase.Lobby || rules.IsFinished;
            response.Approved = validPayload && open && manager.ConnectedClientsIds.Count < 2;
            response.CreatePlayerObject = false; response.Pending = false;
            response.Reason = response.Approved ? "" : "PIN/versi tidak cocok, meja penuh, atau ronde berlangsung.";
        }
        private void OnConnected(ulong client)
        {
            RegisterHandlers(); connecting = false; Status = "Tersambung. Pindai kartu bersama.";
            if (Hosting) SendSnapshot();
        }
        private void OnDisconnected(ulong client)
        {
            if (intentionalStop) return;
            connecting = false; LocalReady = false;
            Status = "Koneksi terputus. Periksa Wi-Fi dan PIN, lalu coba lagi.";
            if (Hosting && client != NetworkManager.ServerClientId) { rules.Abort("Teman terputus. Mulai ulang meja."); SendSnapshot(); }
            else { remoteState.phase = GamePhase.Aborted; remoteState.reason = Status; }
        }
        private void Update()
        {
            float dt = Mathf.Min(Time.unscaledDeltaTime, .25f);
            if (connecting)
            {
                connectingTime += dt;
                if (connectingTime > Config.network.joinTimeoutSeconds)
                { Leave(); Status = "Tidak tersambung. Pastikan Wi-Fi sama dan isolasi perangkat tidak aktif."; }
            }
            if (!Connected) return;
            sendClock += dt; sinceSnapshot += dt;
            if (sendClock >= 1f / Config.network.intentRate)
            {
                sendClock = 0;
                var input = GetLocalIntent?.Invoke() ?? new PlayerIntent();
                input.round = State.roundId; input.sequence = ++sequence; input.ready = LocalReady;
                if (Hosting) ApplyInput(0, input);
                else Send(InputName, NetworkManager.ServerClientId, JsonUtility.ToJson(input), NetworkDelivery.UnreliableSequenced);
            }
            if (Hosting)
            {
                accumulator += dt;
                float step = 1f / Config.tickRate;
                while (accumulator >= step) { rules.Step(step); accumulator -= step; }
                if (sinceSnapshot >= .1f) { sinceSnapshot = 0; SendSnapshot(); }
                ObserveEvents(rules.State);
            }
            else if (sinceSnapshot > Config.tracking.pauseAfterSeconds && !IsTerminal(remoteState.phase))
            { remoteState.phase = GamePhase.Paused; remoteState.reason = "Menunggu host..."; }
        }

        private void ReceiveInput(ulong sender, FastBufferReader reader)
        {
            if (!Hosting || sender == NetworkManager.ServerClientId || !manager.ConnectedClients.ContainsKey(sender)) return;
            float now = Time.unscaledTime;
            if (inputTimes.TryGetValue(sender, out float previous) && now - previous < 1f / Config.network.maxIntentRate) return;
            inputTimes[sender] = now;
            string json = ReadPacket(reader, 1024);
            if (json == null) return;
            try { ApplyInput(1, JsonUtility.FromJson<PlayerIntent>(json)); }
            catch (ArgumentException) { /* Untrusted malformed input is ignored. */ }
        }
        private void ApplyInput(int slot, PlayerIntent input)
        {
            if (input == null || !Finite(input.origin) || !Finite(input.direction) || input.action < 0 || input.action > 3) return;
            var action = (PlayerAction)input.action;
            if (action == PlayerAction.Guard || action == PlayerAction.Loot)
            {
                Vector3 point = new Vector3(0, .11f, 0); float radius = .14f;
                if (action == PlayerAction.Loot)
                {
                    var item = rules.FindItem(input.target);
                    if (item == null) action = PlayerAction.Idle;
                    else { point = rules.SocketPosition(item.socket); radius = .045f; }
                }
                Vector3 offset = point - input.origin;
                float distance = offset.magnitude, along = Vector3.Dot(offset, input.direction.normalized);
                if (distance < Config.aim.minDistanceMeters || distance > Config.aim.maxDistanceMeters || input.direction.sqrMagnitude < .5f || along < 0 || (offset - along * input.direction.normalized).magnitude > radius) action = PlayerAction.Idle;
            }
            if (rules.SetIntent(slot, input.round, input.sequence, action, input.target, input.tracking)) rules.SetReady(slot, input.ready && input.tracking);
        }
        private void SendSnapshot()
        {
            if (!Hosting || !handlersRegistered) return;
            string json = JsonUtility.ToJson(new RoomSnapshot { count = PlayerCount, game = rules.State });
            foreach (ulong client in manager.ConnectedClientsIds)
                if (client != NetworkManager.ServerClientId) Send(StateName, client, json, NetworkDelivery.ReliableSequenced);
        }
        private void ReceiveSnapshot(ulong sender, FastBufferReader reader)
        {
            if (Hosting || sender != NetworkManager.ServerClientId) return;
            string json = ReadPacket(reader, 16384);
            if (json == null) return;
            try
            {
                var snapshot = JsonUtility.FromJson<RoomSnapshot>(json);
                if (snapshot?.game?.players == null || snapshot.game.players.Length != 2 || snapshot.game.items == null || snapshot.game.items.Length > 6 || snapshot.game.events == null || snapshot.game.events.Length > 12) return;
                if (snapshot.game.roundId < remoteState.roundId || (snapshot.game.roundId == remoteState.roundId && snapshot.game.revision < remoteState.revision)) return;
                remoteCount = Mathf.Clamp(snapshot.count, 0, 2); remoteState = snapshot.game; sinceSnapshot = 0;
                ObserveEvents(remoteState);
            }
            catch (ArgumentException) { }
        }
        private void ObserveEvents(GameState state)
        {
            if (observedRound != state.roundId) { observedRound = state.roundId; observedEvent = 0; LocalReady = false; }
            foreach (var ev in state.events)
                if (ev.id > observedEvent) { observedEvent = ev.id; if (ev.type == "Pause") LocalReady = false; GameEventReceived?.Invoke(ev); }
        }
        private void Send(string name, ulong target, string json, NetworkDelivery delivery)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(json);
            using (var writer = new FastBufferWriter(bytes.Length + 4, Allocator.Temp))
            {
                writer.WriteValueSafe(bytes.Length); writer.WriteBytesSafe(bytes);
                manager.CustomMessagingManager.SendNamedMessage(name, target, writer, delivery);
            }
        }
        private static string ReadPacket(FastBufferReader reader, int maximum)
        {
            if (reader.Length < 4 || reader.Length > maximum + 4) return null;
            reader.ReadValueSafe(out int size);
            if (size < 0 || size > maximum || size != reader.Length - reader.Position) return null;
            byte[] bytes = new byte[size]; reader.ReadBytesSafe(ref bytes, size);
            return Encoding.UTF8.GetString(bytes);
        }
        public static bool IsLocalAddress(string value)
        {
            if (!IPAddress.TryParse(value?.Trim(), out IPAddress ip) || ip.AddressFamily != AddressFamily.InterNetwork) return false;
            byte[] b = ip.GetAddressBytes();
            return b[0] == 10 || (b[0] == 172 && b[1] >= 16 && b[1] <= 31) || (b[0] == 192 && b[1] == 168) || (b[0] == 169 && b[1] == 254) || (Application.isEditor && b[0] == 127);
        }
        public static string LocalAddresses()
        {
            var result = new List<string>();
            try
            {
                foreach (var adapter in NetworkInterface.GetAllNetworkInterfaces())
                {
                    if (adapter.OperationalStatus != OperationalStatus.Up) continue;
                    foreach (var entry in adapter.GetIPProperties().UnicastAddresses)
                        if (entry.Address.AddressFamily == AddressFamily.InterNetwork && IsLocalAddress(entry.Address.ToString()) && !IPAddress.IsLoopback(entry.Address)) result.Add(entry.Address.ToString());
                }
            }
            catch (NetworkInformationException) { }
            return result.Count > 0 ? string.Join(" / ", result) : "Lihat alamat IP pada pengaturan Wi-Fi";
        }
        private static bool Finite(Vector3 v) => !(float.IsNaN(v.x) || float.IsNaN(v.y) || float.IsNaN(v.z) || float.IsInfinity(v.x) || float.IsInfinity(v.y) || float.IsInfinity(v.z)) && v.sqrMagnitude < 10000;
        private static bool IsTerminal(GamePhase phase) => phase == GamePhase.Lobby || phase == GamePhase.Won || phase == GamePhase.Lost || phase == GamePhase.Aborted;
        private void OnApplicationPause(bool paused) { if (paused && manager != null && manager.IsListening) Leave(); }
        private void OnDestroy()
        {
            if (manager == null) return;
            intentionalStop = true; manager.OnClientConnectedCallback -= OnConnected; manager.OnClientDisconnectCallback -= OnDisconnected;
            if (manager.IsListening) manager.Shutdown();
        }
    }
}
