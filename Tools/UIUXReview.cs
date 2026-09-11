using System;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using PaketPanik;

// Editor-only, executed through Unity CLI run_script. Never included in an APK.
public static class UIUXReview
{
    public static string Fixture(string phase)
    {
        if (!EditorApplication.isPlaying) return "PLAY_MODE_REQUIRED";
        var p = UnityEngine.Object.FindAnyObjectByType<PanicPresentation>();
        var b = p.board; var s = p.session;
        if (!s.Connected && !s.Host()) return "HOST_FAILED";
        p.enabled = s.enabled = b.enabled = false;
        b.simulator = true;
        b.boardRoot.gameObject.SetActive(true);
        foreach (var driver in b.arCamera.GetComponents<Behaviour>())
            if (driver.GetType().Name == "TrackedPoseDriver" || driver.GetType().Name == "ARCameraBackground") driver.enabled = false;
        b.arCamera.transform.position = b.boardRoot.TransformPoint(new Vector3(0, .6f, -.7f));
        b.arCamera.transform.LookAt(b.boardRoot.TransformPoint(new Vector3(0, .04f, 0)));
        var state = s.State;
        state.phase = (GamePhase)Enum.Parse(typeof(GamePhase), phase);
        state.score = phase == "Won" ? 15 : 8; state.gameTime = 48; state.anger = 64; state.bites = 1;
        state.phaseRemaining = 3;
        state.players[0].battery = 48; state.players[0].collected = 5; state.players[1].collected = 3;
        state.players[1].action = PlayerAction.Guard;
        state.players[0].action = PlayerAction.Loot; state.players[0].target = 7; state.players[0].progress = .6f;
        state.items = new[] { new LootState { id = 7, socket = 1 }, new LootState { id = 8, socket = 5, gold = true } };
        state.reason = phase == "Paused" ? "Tracking melemah. Pindai ulang lalu siap." : phase == "Aborted" ? "Teman terputus. Mulai ulang meja." : "Satu jaga, satu ambil.";
        p.GetType().GetMethod("UpdateWorld", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(p, new object[] { state });
        var ui = p.GetComponent<PanicInterface>(); ui.Render(state, PlayerAction.Loot, 7);
        // Redact room identifiers in saved review images; these are fixtures, not connection proof.
        Field<Text>(ui, "roomAddress").text = "192.168.1.xxx";
        Field<Text>(ui, "roomPin").text = "PIN MEJA   ******";
        return "EDITOR FIXTURE ONLY: " + phase + "\n" + Inspect();
    }

    private static T Field<T>(object obj, string name) =>
        (T)obj.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(obj);

    public static async System.Threading.Tasks.Task<string> ConnectionReview()
    {
        var p = UnityEngine.Object.FindAnyObjectByType<PanicPresentation>(); var s = p.session;
        var ui = p.GetComponent<PanicInterface>();
        var report = new System.Text.StringBuilder();
        Action<bool, string> check = (ok, name) => { report.AppendLine((ok ? "PASS " : "FAIL ") + name); if (!ok) throw new Exception(report.ToString()); };
        check(p.interfaceFont && p.interfaceFont.name.Contains("Inter"), "scene carries Inter font into builds");
        var button = Field<Button>(ui, "hostButton");
        var pointer = new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current)
            { position = RectTransformUtility.WorldToScreenPoint(null, button.transform.position) };
        var hits = new System.Collections.Generic.List<UnityEngine.EventSystems.RaycastResult>();
        UnityEngine.EventSystems.EventSystem.current.RaycastAll(pointer, hits);
        check(hits.Count > 0 && hits[0].gameObject.GetComponentInParent<Button>() == button, "real graphic raycast reaches primary button");
        Click("Gabung meja teman     >"); ui.Render(s.State, PlayerAction.Idle, -1);
        Field<InputField>(ui, "addressInput").text = "127.0.0.1";
        var pin = Field<InputField>(ui, "pinInput"); pin.text = "123456";
        check(pin.onValidateInput("", 0, '-') == '\0' && pin.onValidateInput("", 0, '4') == '4', "PIN validator accepts digits only");
        Click("Gabung sekarang     >"); ui.Render(s.State, PlayerAction.Idle, -1);
        check(s.Connecting && !Field<Button>(ui, "joinSubmit").interactable && !pin.interactable, "joining locks duplicate submission and fields");
        check(Field<Text>(ui, "joinSubmitText").text == "Menghubungkan...", "joining has visible pending feedback");
        Click("Batal / kembali"); ui.Render(s.State, PlayerAction.Idle, -1);
        check(!s.Connecting && Field<GameObject>(ui, "home").activeSelf, "cancel pending connection returns home");
        for (int i = 0; i < 50 && s.Closing; i++) await System.Threading.Tasks.Task.Delay(100);
        ui.Render(s.State, PlayerAction.Idle, -1);
        check(button.interactable && !s.Closing, "connection actions re-enable after shutdown");
        report.AppendLine("Localhost pending/cancel path only, not a two-device network test.");
        File.WriteAllText("Evidence/UIUX/connection-smoke.txt", report.ToString());
        return report.ToString();
    }

    public static async System.Threading.Tasks.Task<string> ReviewStates()
    {
        var log = new System.Text.StringBuilder();
        foreach (string phase in new[] { "Lobby", "Countdown", "Playing", "Recovery", "Paused", "Won", "Lost", "Aborted" })
        {
            Fixture(phase);
            await System.Threading.Tasks.Task.Delay(180);
            Canvas.ForceUpdateCanvases();
            log.AppendLine(phase + ": " + Inspect());
            Capture("after-" + phase.ToLowerInvariant() + "-editor");
            await System.Threading.Tasks.Task.Delay(220);
        }
        var ui = UnityEngine.Object.FindAnyObjectByType<PanicInterface>();
        Click("?");
        await System.Threading.Tasks.Task.Delay(120);
        log.AppendLine("Guide: " + Inspect()); Capture("after-guide-top");
        await System.Threading.Tasks.Task.Delay(200);
        Field<GameObject>(ui, "modal").GetComponentInChildren<ScrollRect>().verticalNormalizedPosition = 0;
        await System.Threading.Tasks.Task.Delay(200);
        Capture("after-guide-bottom");
        File.WriteAllText("Evidence/UIUX/state-layouts.txt", log.ToString());
        return log.ToString();
    }

    public static async System.Threading.Tasks.Task<string> CompactReview()
    {
        var ui = UnityEngine.Object.FindAnyObjectByType<PanicInterface>();
        Click("Mengerti, ayo main");
        Setup(720, 960);
        await System.Threading.Tasks.Task.Delay(300);
        Fixture("Playing");
        await System.Threading.Tasks.Task.Delay(200);
        string log = "COMPACT PORTRAIT 720x960\n" + Inspect();
        Capture("compact-playing-editor");
        await System.Threading.Tasks.Task.Delay(200);
        Fixture("Lobby");
        await System.Threading.Tasks.Task.Delay(200);
        log += "\nCOMPACT LOBBY\n" + Inspect();
        Capture("compact-lobby-editor");
        await System.Threading.Tasks.Task.Delay(200);
        Setup(720, 1600);
        await System.Threading.Tasks.Task.Delay(300);
        Fixture("Playing");
        var safe = Field<RectTransform>(ui, "safe");
        safe.anchorMin = new Vector2(20f / 720, 60f / 1600);
        safe.anchorMax = new Vector2(700f / 720, 1520f / 1600);
        var canvas = GameObject.Find("PaketPanikUI").GetComponent<Canvas>();
        canvas.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1080f * 720 / 680, 1920);
        await System.Threading.Tasks.Task.Delay(250);
        Canvas.ForceUpdateCanvases();
        log += "\nEMULATED INSETS 20 sides, 80 top, 60 bottom at 720x1600\n" + Inspect();
        Vector3 point = Field<RectTransform>(ui, "reticle").position;
        log += "Reticle screen position=" + point + "\n";
        if (Mathf.Abs(point.x - 360) > 1 || Mathf.Abs(point.y - 800) > 1) throw new Exception("Reticle no longer matches camera centre");
        log += "PASS reticle stays at camera viewport centre with insets\n";
        Capture("inset-playing-editor");
        await System.Threading.Tasks.Task.Delay(200);
        File.WriteAllText("Evidence/UIUX/compact-layouts.txt", log);
        return log;
    }

    public static async System.Threading.Tasks.Task<string> Smoke()
    {
        var p = UnityEngine.Object.FindAnyObjectByType<PanicPresentation>();
        var ui = p.GetComponent<PanicInterface>(); var s = p.session;
        var report = new System.Text.StringBuilder();
        Action<bool, string> check = (ok, name) => { report.AppendLine((ok ? "PASS " : "FAIL ") + name); if (!ok) throw new Exception(report.ToString()); };
        Click("Gabung meja teman     >"); ui.Render(s.State, PlayerAction.Idle, -1);
        check(Field<GameObject>(ui, "join").activeSelf && !Field<GameObject>(ui, "home").activeSelf, "separate join page");
        Click("Gabung sekarang     >"); ui.Render(s.State, PlayerAction.Idle, -1);
        check(Field<Text>(ui, "joinStatus").text.StartsWith("IP belum"), "invalid IP has actionable inline error");
        Field<InputField>(ui, "addressInput").text = "192.168.1.8";
        Field<InputField>(ui, "pinInput").text = "123";
        Click("Gabung sekarang     >"); ui.Render(s.State, PlayerAction.Idle, -1);
        check(Field<Text>(ui, "joinStatus").text.StartsWith("PIN harus"), "invalid PIN has actionable inline error");
        Click("Kembali"); ui.Render(s.State, PlayerAction.Idle, -1);
        Click("?"); check(ui.BlockingInput, "guide blocks game input");
        var guide = Field<GameObject>(ui, "modal").GetComponentInChildren<ScrollRect>();
        check(guide.content.rect.height > guide.viewport.rect.height && !guide.horizontal, "guide scrolls vertically");
        Click("Mengerti, ayo main"); check(!ui.BlockingInput, "guide closes");
        Click("Buat meja baru     +"); ui.Render(s.State, PlayerAction.Idle, -1);
        check(s.Connected && s.Hosting, "real local Host opens");
        check(!Field<Button>(ui, "startButton").interactable, "cannot start with one player");
        check(!Field<Button>(ui, "readyButton").interactable || p.board.TrackingValid, "ready requires tracking");
        Fixture("Playing");
        check(Field<GameObject>(ui, "controls").activeSelf, "playing exposes thumb controls");
        var hold = Field<HoldControl>(ui, "actionHold");
        hold.OnPointerDown(new UnityEngine.EventSystems.PointerEventData(null) { pointerId = 1 });
        check(hold.Held, "hold starts on pointer down");
        hold.gameObject.SetActive(false);
        check(!hold.Held, "hide clears held input in Play Mode");
        hold.gameObject.SetActive(true);
        hold.OnPointerDown(new UnityEngine.EventSystems.PointerEventData(null) { pointerId = 1 });
        Click("Kembali");
        check(ui.BlockingInput && !hold.Held, "leave confirmation cancels held input");
        check(s.GetLocalIntent().action == (int)PlayerAction.Idle, "modal sends idle intent");
        Click("Tetap main");
        check(!ui.BlockingInput && s.Connected, "cancel leave keeps table");
        Fixture("Paused");
        check(!Field<GameObject>(ui, "controls").activeSelf && Field<GameObject>(ui, "lobby").activeSelf, "tracking pause hides actions and offers ready");
        Fixture("Won");
        check(Field<GameObject>(ui, "results").activeSelf && !Field<GameObject>(ui, "controls").activeSelf, "win shows receipt");
        check(!Field<Button>(ui, "replayButton").interactable, "replay still requires both players");
        Click("Kembali ke beranda");
        await System.Threading.Tasks.Task.Delay(200);
        ui.Render(s.State, PlayerAction.Idle, -1);
        check(Field<GameObject>(ui, "home").activeSelf, "leave returns to welcome");
        report.AppendLine("UI fixture coverage only. Two-device AR/network tests NOT RUN.");
        File.WriteAllText("Evidence/UIUX/play-smoke.txt", report.ToString());
        return report.ToString();
    }

    public static string Setup(int width = 1080, int height = 1920)
    {
        if (!EditorApplication.isPlaying)
        {
            var p = UnityEngine.Object.FindAnyObjectByType<PanicPresentation>();
            if (!p) return "Open PaketPanik scene first";
            p.interfaceFont = AssetDatabase.LoadAssetAtPath<Font>("Assets/MRTemplateAssets/Fonts/Inter/Inter-Regular.ttf");
            EditorUtility.SetDirty(p);
            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(p.gameObject.scene);
        }
        var assembly = typeof(Editor).Assembly;
        var sizesType = assembly.GetType("UnityEditor.GameViewSizes");
        var singleton = typeof(ScriptableSingleton<>).MakeGenericType(sizesType);
        var sizes = singleton.GetProperty("instance").GetValue(null);
        var groupType = assembly.GetType("UnityEditor.GameViewSizeGroupType");
        var group = sizesType.GetMethod("GetGroup").Invoke(sizes, new[] { Enum.Parse(groupType, "Android") });
        var sizeType = assembly.GetType("UnityEditor.GameViewSize");
        var kind = assembly.GetType("UnityEditor.GameViewSizeType");
        var size = Activator.CreateInstance(sizeType, new[] { Enum.Parse(kind, "FixedResolution"), (object)width, height, "Paket Panik UI QA" });
        group.GetType().GetMethod("AddCustomSize").Invoke(group, new[] { size });
        int count = (int)group.GetType().GetMethod("GetTotalCount").Invoke(group, null);
        var viewType = assembly.GetType("UnityEditor.GameView");
        var view = EditorWindow.GetWindow(viewType);
        viewType.GetProperty("selectedSizeIndex", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).SetValue(view, count - 1);
        view.Repaint();
        return "Game View: " + width + "x" + height;
    }

    public static string Inspect()
    {
        var ui = UnityEngine.Object.FindAnyObjectByType<PanicInterface>();
        if (!ui) return "NO_UI";
        var canvas = GameObject.Find("PaketPanikUI").GetComponent<Canvas>();
        int clipped = 0;
        var report = new System.Text.StringBuilder();
        report.AppendLine("screen=" + Screen.width + "x" + Screen.height + " safe=" + Screen.safeArea);
        foreach (var text in canvas.GetComponentsInChildren<Text>())
        {
            if (!text.isActiveAndEnabled || string.IsNullOrEmpty(text.text)) continue;
            if (text.preferredHeight > text.rectTransform.rect.height + 3)
            { clipped++; report.AppendLine("CLIPPED " + text.transform.parent.name + ": " + text.text.Replace("\n", " / ")); }
        }
        report.AppendLine("Text height overflows=" + clipped);
        report.AppendLine("NOTE: Editor UI evidence only; no physical AR/LAN validation.");
        return report.ToString();
    }

    public static string Click(string name)
    {
        var root = GameObject.Find("PaketPanikUI");
        foreach (var button in root.GetComponentsInChildren<Button>())
            if (button.gameObject.name == name && button.interactable) { button.onClick.Invoke(); return "clicked " + name; }
        return "No active interactable button: " + name;
    }

    public static string Capture(string name)
    {
        string path = Path.GetFullPath("Evidence/UIUX/" + name + ".png");
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        ScreenCapture.CaptureScreenshot(path);
        return path;
    }
}
