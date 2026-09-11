using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace PaketPanik
{
    public sealed class HoldControl : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        public bool Held { get; private set; }
        private int pointer;
        public void OnPointerDown(PointerEventData e) { if (!Held) { Held = true; pointer = e.pointerId; } }
        public void OnPointerUp(PointerEventData e) { if (e.pointerId == pointer) Release(); }
        public void OnPointerExit(PointerEventData e) { if (e.pointerId == pointer) Release(); }
        public void Release() { Held = false; }
        private void OnDisable() { Release(); }
    }
    public sealed class PanicPresentation : MonoBehaviour
    {
        public SharedBoard board;
        public LanSession session;
        private readonly Color ink = Hex("151721"), lilac = Hex("B49CFF"), gold = Hex("FFD367"), coral = Hex("FF6F77"), mint = Hex("76E0BA");
        private readonly Dictionary<int, Transform> loot = new Dictionary<int, Transform>();
        private Transform monster, lid, mouth;
        public Font interfaceFont;
        private PanicInterface ui;
        private Material cardboard, purpleMaterial, candyMaterial;
        private AudioSource audioSource;
        private AudioClip collectSound, biteSound, sneezeSound, winSound;
        private float biteUntil, sneezeUntil, aimLostAt, fpsElapsed;
        private int target = -1, fpsFrames;
        private bool muted;
        private PlayerAction aimedAction;

        private void Start()
        {
            Application.targetFrameRate = 60; Screen.sleepTimeout = SleepTimeout.NeverSleep;
            BuildWorld(); BuildAudio();
            ui = gameObject.AddComponent<PanicInterface>();
            ui.Initialize(this, board, session, interfaceFont);
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
        private void Update()
        {
            if (!ui || !session) return;
            if (session.Connected) { UpdateAim(); UpdateWorld(session.State); }
            ui.Render(session.State, aimedAction, target);
            if (!session.Connected) return;
            fpsFrames++; fpsElapsed += Time.unscaledDeltaTime;
            if (fpsElapsed >= 10) { Debug.Log("PAKET_PERF fps=" + (fpsFrames / fpsElapsed).ToString("F1")); fpsFrames = 0; fpsElapsed = 0; }
        }
        public void SetMuted(bool value) { muted = value; if (audioSource) audioSource.mute = value; }
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
            return new PlayerIntent { action = (int)(ui && !ui.BlockingInput && session.State.phase == GamePhase.Playing ? (ui.SealHeld ? PlayerAction.Seal : ui.ActionHeld ? aimedAction : PlayerAction.Idle) : PlayerAction.Idle), target = target, tracking = board.TrackingValid, origin = ray.origin, direction = ray.direction };
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
        private static Color Hex(string value) { ColorUtility.TryParseHtmlString("#" + value, out Color color); return color; }
        private void OnDestroy() { if (session) { session.GameEventReceived -= OnGameEvent; session.GetLocalIntent = null; } }
    }
}
