using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace PaketPanik
{
    public sealed class HoldControl : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        public bool Held { get; private set; }
        public void OnPointerDown(PointerEventData e) { Held = true; }
        public void OnPointerUp(PointerEventData e) { Held = false; }
        public void OnPointerExit(PointerEventData e) { Held = false; }
        private void OnDisable() { Held = false; }
    }
    public sealed class PanicPresentation : MonoBehaviour
    {
        public SharedBoard board;
        public LanSession session;
        private readonly Color ink = Hex("151721"), lilac = Hex("B49CFF"), gold = Hex("FFD367"), coral = Hex("FF6F77"), mint = Hex("76E0BA");
        private readonly Dictionary<int, Transform> loot = new Dictionary<int, Transform>();
        private Transform monster, lid, mouth;
        private Canvas canvas;
        private RectTransform safe;
        private GameObject menu, lobby, hud, controls;
        private Text status, roomInfo, score, clock, batteryLabel, actionLabel, readyLabel, resultLabel, menuStatus;
        private InputField ip, pin;
        private Image angerBar, batteryBar;
        private Button startButton;
        private GameObject leaveButton, muteButton;
        private HoldControl actionHold, sealHold;
        private GameObject sealButton;
        private Font font;
        private Material cardboard, purpleMaterial, candyMaterial;
        private AudioSource audioSource;
        private AudioClip collectSound, biteSound, sneezeSound, winSound;
        private float biteUntil, sneezeUntil, aimLostAt, fpsElapsed;
        private int target = -1, fpsFrames;
        private bool muted;
        private PlayerAction aimedAction;
        private Rect lastSafe;

        private void Start()
        {
            Application.targetFrameRate = 60; Screen.sleepTimeout = SleepTimeout.NeverSleep;
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            BuildWorld(); BuildUI(); BuildAudio();
            session.GetLocalIntent = MakeIntent; session.GameEventReceived += OnGameEvent;
        }
        private void BuildWorld()
        {
            cardboard = Material(Hex("CC995F")); purpleMaterial = Material(lilac); candyMaterial = Material(gold);
            monster = new GameObject("PaketMonster").transform; monster.SetParent(board.boardRoot, false);
            Primitive("Body", monster, new Vector3(0, .09f, 0), new Vector3(.22f, .18f, .20f), cardboard);
            Primitive("Tape", monster, new Vector3(0, .092f, -.101f), new Vector3(.04f, .175f, .003f), purpleMaterial);
            lid = new GameObject("LidPivot").transform; lid.SetParent(monster, false); lid.localPosition = new Vector3(0, .185f, .09f);
            Primitive("Lid", lid, new Vector3(0, 0, -.10f), new Vector3(.245f, .025f, .235f), cardboard);
            var black = Material(Hex("241C2C")); var white = Material(Hex("FFF1DB"));
            for (int i = -1; i <= 1; i += 2)
            {
                Primitive("Eye", monster, new Vector3(i * .057f, .132f, -.105f), new Vector3(.039f, .045f, .024f), white, PrimitiveType.Sphere);
                Primitive("Pupil", monster, new Vector3(i * .057f, .131f, -.118f), new Vector3(.017f, .025f, .013f), black, PrimitiveType.Sphere);
            }
            mouth = Primitive("Mouth", monster, new Vector3(0, .055f, -.104f), new Vector3(.11f, .02f, .008f), black).transform;
            for (int i = -1; i <= 1; i++) Primitive("Tooth", monster, new Vector3(i * .035f, .068f, -.11f), new Vector3(.017f, .021f, .012f), white);
            var cornerMat = Material(mint);
            for (int x = -1; x <= 1; x += 2)
                for (int z = -1; z <= 1; z += 2) Primitive("AlignmentCorner", board.boardRoot, new Vector3(x * .1f, .002f, z * .1f), new Vector3(.016f, .004f, .016f), cornerMat);
            Primitive("TopOfCard", board.boardRoot, new Vector3(0, .003f, .115f), new Vector3(.025f, .006f, .045f), purpleMaterial);
        }
        private void BuildUI()
        {
            canvas = new GameObject("PaketPanikUI", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster)).GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvas.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1080, 1920); scaler.matchWidthOrHeight = .5f;
            safe = new GameObject("SafeArea", typeof(RectTransform)).GetComponent<RectTransform>(); safe.SetParent(canvas.transform, false);
            if (!FindAnyObjectByType<EventSystem>()) new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            menu = Panel("Welcome", safe, new Vector2(920, 1450), new Vector2(.5f, .5f), Vector2.zero, ink);
            var layout = menu.AddComponent<VerticalLayoutGroup>(); layout.padding = new RectOffset(50, 50, 35, 35); layout.spacing = 16; layout.childControlHeight = true; layout.childForceExpandHeight = false;
            Line(menu.transform, "ANDROID AR / 2 PEMAIN", 25, lilac, 55);
            Line(menu.transform, "PAKET\nPANIK.", 104, Color.white, 250);
            Line(menu.transform, "Jaga paketnya. Curi isinya.\nSalahkan temanmu.", 38, gold, 130);
            Line(menu.transform, "Dua HP ARCore, Wi-Fi yang sama, dan kartu 20 cm di meja. Duduk dengan nyaman; tidak perlu berlari atau mengguncang HP.", 28, Color.white, 160);
            ip = Field(menu.transform, "IP teman, contoh 192.168.1.8", 75);
            pin = Field(menu.transform, "PIN enam angka", 75); pin.contentType = InputField.ContentType.IntegerNumber; pin.characterLimit = 6;
            MenuButton(menu.transform, "BUAT MEJA", () => { if (session.Host()) board.BeginCamera(); }, lilac);
            MenuButton(menu.transform, "GABUNG MEJA", () => { if (session.Join(ip.text, pin.text)) board.BeginCamera(); }, gold);
            MenuButton(menu.transform, "PANDUAN DAN PRIVASI", ShowHelp, new Color(.3f, .33f, .4f));
            menuStatus = Line(menu.transform, "", 26, coral, 120);
            Line(menu.transform, "Kamera dipakai untuk AR di HP. Game mengirim status permainan lewat jaringan lokal. Tanpa akun atau voice chat.", 22, new Color(.7f, .73f, .8f), 120);

            hud = Panel("Hud", safe, new Vector2(990, 240), new Vector2(.5f, 1), new Vector2(0, -130), new Color(ink.r, ink.g, ink.b, .90f));
            score = Label(hud.transform, "ISI TAS 0 / 12", 41, Color.white, new Vector2(700, 60), new Vector2(0, 72));
            clock = Label(hud.transform, "01:30", 30, gold, new Vector2(170, 60), new Vector2(370, 72));
            angerBar = Bar(hud.transform, new Vector2(0, 14), coral);
            batteryBar = Bar(hud.transform, new Vector2(0, -50), lilac);
            batteryLabel = Label(hud.transform, "Lampu kamu", 24, Color.white, new Vector2(900, 36), new Vector2(0, -95));
            var leave = ButtonAt(safe, "KELUAR", new Vector2(180, 70), new Vector2(.15f, .77f), Vector2.zero, ink); leave.onClick.AddListener(session.Leave);
            leaveButton = leave.gameObject;
            var mute = ButtonAt(safe, "SUARA: ON", new Vector2(225, 70), new Vector2(.81f, .77f), Vector2.zero, ink);
            mute.onClick.AddListener(() => { muted = !muted; audioSource.mute = muted; mute.GetComponentInChildren<Text>().text = muted ? "SUARA: OFF" : "SUARA: ON"; });
            muteButton = mute.gameObject;

            lobby = Panel("Lobby", safe, new Vector2(950, 690), new Vector2(.5f, .25f), Vector2.zero, new Color(ink.r, ink.g, ink.b, .94f));
            roomInfo = Label(lobby.transform, "", 29, gold, new Vector2(850, 160), new Vector2(0, 240));
            resultLabel = Label(lobby.transform, "Pindai kartu bersama", 34, Color.white, new Vector2(850, 110), new Vector2(0, 95));
            var ready = ButtonAt(lobby.transform, "POSISI COCOK / SIAP", new Vector2(820, 90), new Vector2(.5f, .5f), new Vector2(0, -40), mint);
            readyLabel = ready.GetComponentInChildren<Text>(); ready.onClick.AddListener(() => { board.BeginCamera(); session.LocalReady = board.TrackingValid; });
            startButton = ButtonAt(lobby.transform, "MULAI PAKET", new Vector2(820, 90), new Vector2(.5f, .5f), new Vector2(0, -150), lilac);
            startButton.onClick.AddListener(() => session.StartRound());
            var scan = ButtonAt(lobby.transform, "PINDAI ULANG KARTU", new Vector2(820, 80), new Vector2(.5f, .5f), new Vector2(0, -250), new Color(.32f, .35f, .42f));
            scan.onClick.AddListener(() => { session.LocalReady = false; board.Recalibrate(); board.BeginCamera(); });

            controls = new GameObject("GameControls", typeof(RectTransform)); controls.transform.SetParent(safe, false);
            var cr = controls.GetComponent<RectTransform>(); cr.anchorMin = Vector2.zero; cr.anchorMax = Vector2.one; cr.offsetMin = cr.offsetMax = Vector2.zero;
            Label(controls.transform, "+", 50, Color.white, new Vector2(65, 65), Vector2.zero);
            var action = ButtonAt(controls.transform, "TAHAN: TENANGKAN", new Vector2(910, 125), new Vector2(.5f, .09f), Vector2.zero, lilac);
            actionHold = action.gameObject.AddComponent<HoldControl>(); actionLabel = action.GetComponentInChildren<Text>();
            var seal = ButtonAt(controls.transform, "TAHAN BERSAMA: SEGEL", new Vector2(910, 88), new Vector2(.5f, .025f), Vector2.zero, gold);
            sealHold = seal.gameObject.AddComponent<HoldControl>(); sealButton = seal.gameObject;
            status = Label(safe, "", 31, Color.white, new Vector2(900, 160), new Vector2(0, -430));
        }
        private void ShowHelp()
        {
            var popup = Panel("Help", safe, new Vector2(960, 1600), new Vector2(.5f, .5f), Vector2.zero, ink);
            Label(popup.transform, "CARA MAIN", 65, lilac, new Vector2(850, 100), new Vector2(0, 665));
            Label(popup.transform, "1. Cetak kartu marker selebar 20 cm dari paket download. Letakkan datar di meja terang.\n\n2. Hubungkan dua HP ke Wi-Fi yang sama. Satu membuat meja; teman memasukkan IP dan PIN.\n\n3. Pindai kartu yang sama. Periksa empat sudut dan panah, lalu kedua pemain menekan Siap.\n\n4. Arahkan reticle ke paket dan tahan untuk menjaga. Teman arahkan ke camilan dan tahan untuk mengambil. Ganti penjaga sebelum lampu habis.\n\n5. Kumpulkan 12 poin. Tahan Segel bersama untuk menang, atau ambil risiko demi skor lebih tinggi. Tiga gigitan membuat kalian kalah.\n\nTidak perlu bergerak cepat. Jauhkan minuman dari tepi meja. Hentikan jika tidak nyaman.\n\nKamera tidak dikirim ke pemain lain. Tidak ada akun, iklan atau voice chat. ARCore dapat memerlukan Google Play Services for AR. Gunakan jaringan tepercaya; PIN bukan enkripsi jaringan.", 30, Color.white, new Vector2(830, 1210), new Vector2(0, -15));
            var close = ButtonAt(popup.transform, "MENGERTI", new Vector2(820, 90), new Vector2(.5f, .5f), new Vector2(0, -690), lilac); close.onClick.AddListener(() => Destroy(popup));
        }
        private void Update()
        {
            if (!canvas || !session) return;
            if (lastSafe != Screen.safeArea)
            {
                lastSafe = Screen.safeArea; safe.anchorMin = lastSafe.position / new Vector2(Screen.width, Screen.height); safe.anchorMax = (lastSafe.position + lastSafe.size) / new Vector2(Screen.width, Screen.height); safe.offsetMin = safe.offsetMax = Vector2.zero;
            }
            var state = session.State; bool connected = session.Connected;
            bool inLobby = state.phase == GamePhase.Lobby || state.phase == GamePhase.Won || state.phase == GamePhase.Lost || state.phase == GamePhase.Aborted || state.phase == GamePhase.Paused;
            menu.SetActive(!connected); hud.SetActive(connected); lobby.SetActive(connected && inLobby); controls.SetActive(connected && !inLobby);
            if (leaveButton) leaveButton.SetActive(connected);
            if (muteButton) muteButton.SetActive(connected);
            menuStatus.text = session.Status;
            if (!connected) return;
            var player = state.players[session.LocalSlot];
            score.text = "ISI TAS  " + state.score + " / " + session.Config.targetScore;
            float left = Mathf.Max(0, session.Config.roundSeconds - state.gameTime);
            clock.text = Mathf.CeilToInt(left / 60f).ToString("00") + ":" + Mathf.FloorToInt(left % 60).ToString("00");
            angerBar.fillAmount = state.anger / 100f; batteryBar.fillAmount = player.battery / 100f;
            batteryLabel.text = "LAMPU " + Mathf.RoundToInt(player.battery) + "%    /    GIGITAN " + state.bites + "/3";
            roomInfo.text = (session.Hosting ? "HOST  " + LanSession.LocalAddresses() + "\nPIN  " + session.Pin : "TERSAMBUNG KE TEMAN") + "\n" + session.PlayerCount + "/2 pemain";
            readyLabel.text = session.LocalReady ? "SIAP. MENUNGGU TEMAN..." : "POSISI COCOK / SIAP";
            startButton.gameObject.SetActive(session.Hosting && state.phase != GamePhase.Paused);
            startButton.interactable = session.PlayerCount == 2 && state.players[0].ready && state.players[1].ready;
            resultLabel.text = state.phase == GamePhase.Lobby ? board.Status : state.reason;
            status.text = !board.TrackingValid ? board.Status : state.phase == GamePhase.Countdown ? "SIAP... " + Mathf.CeilToInt(state.phaseRemaining) : state.reason;
            if (state.phase == GamePhase.Playing)
            {
                if (state.gameTime >= 70) status.text = "KURIR DATANG! Segel atau ambil lagi?";
                foreach (float sneeze in session.Config.sneezeAt) if (sneeze > state.gameTime && sneeze - state.gameTime <= session.Config.sneezeWarningSeconds) status.text = "AWAS... PAKET MAU BERSIN!";
                if (player.battery < 20) status.text = "LAMPU HABIS. GANTIAN!";
            }
            sealButton.SetActive(state.score >= session.Config.targetScore && state.phase == GamePhase.Playing);
            UpdateAim(); UpdateWorld(state);
            actionLabel.text = aimedAction == PlayerAction.Guard ? "TAHAN: TENANGKAN" : aimedAction == PlayerAction.Loot ? "TAHAN: AMBIL  " + Mathf.RoundToInt(player.progress * 100) + "%" : "ARAHKAN KE PAKET / CAMILAN";
            if (board.simulator) status.text = "SIMULATOR DESKTOP / BUKAN AR PERANGKAT\n" + status.text;
            fpsFrames++; fpsElapsed += Time.unscaledDeltaTime;
            if (fpsElapsed >= 10) { Debug.Log("PAKET_PERF fps=" + (fpsFrames / fpsElapsed).ToString("F1")); fpsFrames = 0; fpsElapsed = 0; }
        }
        private void UpdateAim()
        {
            var ray = board.BoardRay(); int newTarget = -1; PlayerAction newAction = PlayerAction.Idle;
            if (Hits(ray, new Vector3(0, .11f, 0), .14f)) newAction = PlayerAction.Guard;
            foreach (var item in session.State.items)
                if (Hits(ray, GameRules.PositionForSocket(item.socket), .045f)) { newTarget = item.id; newAction = PlayerAction.Loot; break; }
            if (newAction != PlayerAction.Idle) { target = newTarget; aimedAction = newAction; aimLostAt = Time.unscaledTime; }
            else if (Time.unscaledTime - aimLostAt > session.Config.aim.jitterGraceSeconds) { target = -1; aimedAction = PlayerAction.Idle; }
        }
        private PlayerIntent MakeIntent()
        {
            var ray = board.BoardRay();
            return new PlayerIntent { action = (int)(sealHold && sealHold.Held ? PlayerAction.Seal : actionHold && actionHold.Held ? aimedAction : PlayerAction.Idle), target = target, tracking = board.TrackingValid, origin = ray.origin, direction = ray.direction };
        }
        private static bool Hits(Ray ray, Vector3 point, float radius)
        {
            var offset = point - ray.origin; float along = Vector3.Dot(offset, ray.direction);
            return along > 0 && (offset - along * ray.direction).magnitude < radius;
        }
        private void UpdateWorld(GameState state)
        {
            bool biting = Time.unscaledTime < biteUntil, sneezing = Time.unscaledTime < sneezeUntil;
            monster.localPosition = new Vector3(0, .002f * Mathf.Sin(Time.unscaledTime * 3), 0);
            monster.localRotation = Quaternion.Euler(0, 0, biting ? Mathf.Sin(Time.unscaledTime * 24) * 6 : 0);
            monster.localScale = Vector3.one * (biting ? 1.12f : 1f);
            lid.localRotation = Quaternion.Euler(biting ? -65 : sneezing ? -35 : -state.anger * .13f, 0, 0);
            mouth.localScale = new Vector3(.11f, biting ? .065f : .018f + state.anger * .0002f, .008f);
            var remove = new List<int>();
            foreach (var pair in loot)
            {
                bool exists = false; foreach (var item in state.items) if (item.id == pair.Key) exists = true;
                if (!exists) { Destroy(pair.Value.gameObject); remove.Add(pair.Key); }
            }
            foreach (int id in remove) loot.Remove(id);
            foreach (var item in state.items)
            {
                if (!loot.TryGetValue(item.id, out Transform t))
                {
                    t = Primitive("Candy_" + item.id, board.boardRoot, GameRules.PositionForSocket(item.socket), new Vector3(.045f, .045f, .045f), item.gold ? candyMaterial : purpleMaterial, PrimitiveType.Sphere).transform;
                    loot[item.id] = t;
                }
                t.localPosition = GameRules.PositionForSocket(item.socket) + Vector3.up * (Mathf.Sin(Time.unscaledTime * 3 + item.id) * .005f);
                t.localScale = Vector3.one * (item.gold ? .052f : .043f);
            }
        }
        private void BuildAudio()
        {
            audioSource = gameObject.AddComponent<AudioSource>(); audioSource.playOnAwake = false; audioSource.volume = .24f;
            collectSound = Tone("Collect", 660, 990, .12f); biteSound = Tone("Bite", 150, 65, .3f); sneezeSound = Tone("Sneeze", 320, 100, .25f); winSound = Tone("Win", 520, 1040, .5f);
        }
        private void OnGameEvent(GameEvent ev)
        {
            if (ev.type == "Bite") { biteUntil = Time.unscaledTime + 1.2f; audioSource.PlayOneShot(biteSound); }
            else if (ev.type == "Sneeze") { sneezeUntil = Time.unscaledTime + .5f; audioSource.PlayOneShot(sneezeSound); }
            else if (ev.type == "Collect") audioSource.PlayOneShot(collectSound);
            else if (ev.type == "Win") audioSource.PlayOneShot(winSound);
        }
        private static AudioClip Tone(string name, float start, float end, float seconds)
        {
            const int rate = 22050; var samples = new float[(int)(seconds * rate)]; float phase = 0;
            for (int i = 0; i < samples.Length; i++) { float p = (float)i / samples.Length; phase += 2 * Mathf.PI * Mathf.Lerp(start, end, p) / rate; samples[i] = Mathf.Sin(phase) * Mathf.Sin(Mathf.PI * p) * .5f; }
            var clip = AudioClip.Create(name, samples.Length, 1, rate, false); clip.SetData(samples, 0); return clip;
        }
        private Material Material(Color color) { var m = new Material(Shader.Find("Universal Render Pipeline/Lit")); m.color = color; m.SetFloat("_Smoothness", .18f); return m; }
        private static GameObject Primitive(string name, Transform parent, Vector3 position, Vector3 scale, Material material, PrimitiveType type = PrimitiveType.Cube)
        {
            var go = GameObject.CreatePrimitive(type); go.name = name; go.transform.SetParent(parent, false); go.transform.localPosition = position; go.transform.localScale = scale; go.GetComponent<Renderer>().sharedMaterial = material; Destroy(go.GetComponent<Collider>()); return go;
        }
        private GameObject Panel(string name, Transform parent, Vector2 size, Vector2 anchor, Vector2 position, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image)); go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>(); rt.anchorMin = rt.anchorMax = anchor; rt.sizeDelta = size; rt.anchoredPosition = position; go.GetComponent<Image>().color = color; return go;
        }
        private Text Label(Transform parent, string value, int size, Color color, Vector2 dimensions, Vector2 position)
        {
            var go = new GameObject("Label", typeof(RectTransform), typeof(Text)); go.transform.SetParent(parent, false); var rt = go.GetComponent<RectTransform>(); rt.sizeDelta = dimensions; rt.anchoredPosition = position;
            var text = go.GetComponent<Text>(); text.font = font; text.fontSize = size; text.text = value; text.color = color; text.alignment = TextAnchor.MiddleCenter; text.raycastTarget = false; text.horizontalOverflow = HorizontalWrapMode.Wrap; return text;
        }
        private Text Line(Transform parent, string value, int size, Color color, float height)
        {
            var text = Label(parent, value, size, color, new Vector2(820, height), Vector2.zero); text.gameObject.AddComponent<LayoutElement>().preferredHeight = height; return text;
        }
        private Button ButtonAt(Transform parent, string title, Vector2 size, Vector2 anchor, Vector2 position, Color color)
        {
            var go = Panel(title, parent, size, anchor, position, color); var button = go.AddComponent<Button>();
            Label(go.transform, title, 31, color == ink ? Color.white : ink, size - new Vector2(20, 8), Vector2.zero); return button;
        }
        private void MenuButton(Transform parent, string title, UnityEngine.Events.UnityAction callback, Color color)
        {
            var button = ButtonAt(parent, title, new Vector2(820, 95), new Vector2(.5f, .5f), Vector2.zero, color); button.gameObject.AddComponent<LayoutElement>().preferredHeight = 95; button.onClick.AddListener(callback);
        }
        private InputField Field(Transform parent, string placeholder, float height)
        {
            var go = Panel("Input", parent, new Vector2(820, height), new Vector2(.5f, .5f), Vector2.zero, new Color(.24f, .26f, .32f));
            go.AddComponent<LayoutElement>().preferredHeight = height;
            var input = go.AddComponent<InputField>(); input.textComponent = Label(go.transform, "", 31, Color.white, new Vector2(790, height - 5), Vector2.zero); input.placeholder = Label(go.transform, placeholder, 27, new Color(.7f, .73f, .8f), new Vector2(790, height - 5), Vector2.zero); input.characterLimit = 32; return input;
        }
        private Image Bar(Transform parent, Vector2 position, Color color)
        {
            var back = Panel("Meter", parent, new Vector2(900, 19), new Vector2(.5f, .5f), position, new Color(.3f, .32f, .38f));
            var fill = Panel("Fill", back.transform, new Vector2(900, 19), new Vector2(.5f, .5f), Vector2.zero, color).GetComponent<Image>();
            fill.type = Image.Type.Filled; fill.fillMethod = Image.FillMethod.Horizontal; return fill;
        }
        private static Color Hex(string value) { ColorUtility.TryParseHtmlString("#" + value, out Color color); return color; }
        private void OnDestroy() { if (session) session.GameEventReceived -= OnGameEvent; }
    }
}
