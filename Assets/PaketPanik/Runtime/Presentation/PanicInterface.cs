using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using UnityEngine.Events;

namespace PaketPanik
{
    /// <summary>Screen-space presentation only. Rules, readiness and actions remain host-authoritative.</summary>
    public sealed class PanicInterface : MonoBehaviour
    {
        private static readonly Color Ink = C("101B2B"), Paper = C("F5F0E6"), Yellow = C("FFD15C"),
            Lime = C("D4F887"), Coral = C("FF8272"), Muted = C("B3BFCE"), Slate = C("22334B"), Purple = C("BFA9FF");
        private PanicPresentation presentation;
        private SharedBoard board;
        private LanSession session;
        private Font font;
        private Canvas canvas;
        private RectTransform safe, reticle;
        private GameObject home, join, lobby, hud, controls, results, modal, roomCard, scanCard, countdown, toast, backdrop;
        private Text homeStatus, joinStatus, roomAddress, roomPin, roomCount, scanTitle, scanBody, readiness, lobbyHint;
        private Text score, timer, angerText, batteryText, partnerText, actionTitle, actionHint, cue, phaseNumber, phaseText;
        private Text resultTitle, resultBody, resultScore, resultStats, resultHint, toastText, connectionStep;
        private Button joinSubmit, hostButton, readyButton, startButton, replayButton, actionButton, sealButton, helpButton;
        private Button soundButton, backButton, resultReady;
        private Text joinSubmitText, startText, readyText, replayText;
        private InputField addressInput, pinInput;
        private RectTransform angerFill, batteryFill, actionFill, sealFill;
        private readonly PanicSurface[] biteMarks = new PanicSurface[3];
        private HoldControl actionHold, sealHold;
        private bool joiningPage, wasConnected, soundOn, resultAcknowledged, joinAttempted;
        private GamePhase previousPhase = (GamePhase)(-1);
        private Rect lastSafe;
        private int lastWidth, lastHeight;
        private float toastUntil, roomRefreshAt;
        private string formError = "", lastSessionStatus = "";
        public bool ActionHeld => actionHold && actionHold.Held;
        public bool SealHeld => sealHold && sealHold.Held;
        public bool BlockingInput => modal != null;

        public void Initialize(PanicPresentation owner, SharedBoard sharedBoard, LanSession lan, Font interfaceFont)
        {
            presentation = owner; board = sharedBoard; session = lan;
            font = interfaceFont ? interfaceFont : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            soundOn = PlayerPrefs.GetInt("pp.sound", 1) == 1;
            presentation.SetMuted(!soundOn);
            Build();
            session.GameEventReceived += OnEvent;
        }

        private void Build()
        {
            canvas = new GameObject("PaketPanikUI", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster)).GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvas.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920); scaler.matchWidthOrHeight = 0;
            backdrop = Box("Screen background", canvas.transform, Vector2.zero, Vector2.zero, Ink, 0);
            Stretch((RectTransform)backdrop.transform);
            if (!FindAnyObjectByType<EventSystem>()) new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            safe = Rect("SafeArea", canvas.transform); Stretch(safe);
            home = Page("Welcome"); BuildHome(home.transform);
            join = Page("Join"); BuildJoin(join.transform);
            lobby = Empty("Lobby", safe); Stretch((RectTransform)lobby.transform); BuildLobby();
            hud = Empty("HUD", safe); Stretch((RectTransform)hud.transform); BuildHUD();
            controls = Empty("Controls", safe); Stretch((RectTransform)controls.transform); BuildControls();
            results = Page("Results"); BuildResults(results.transform);
            // Reticle stays at the camera viewport centre, independently of notch insets.
            reticle = Rect("Reticle", canvas.transform); reticle.sizeDelta = new Vector2(74, 74);
            for (int i = 0; i < 4; i++)
            {
                float angle = i * Mathf.PI / 2;
                var tick = Box("Aim tick", reticle, new Vector2(i % 2 == 0 ? 15 : 5, i % 2 == 0 ? 5 : 15),
                    new Vector2(Mathf.Cos(angle) * 27, Mathf.Sin(angle) * 27), Paper, 2);
                tick.GetComponent<PanicSurface>().raycastTarget = false;
            }
            Box("Aim dot", reticle, new Vector2(6, 6), Vector2.zero, Paper, 3).GetComponent<PanicSurface>().raycastTarget = false;
            var header = Box("Header", safe, new Vector2(984, 96), new Vector2(0, -66), Ink, 24, new Vector2(.5f, 1));
            backButton = Btn(header.transform, "Kembali", new Vector2(178, 80), new Vector2(-390, 0), Slate, Paper, NavigateBack);
            Txt(header.transform, "PAKET / PANIK", 28, Paper, new Vector2(350, 64), Vector2.zero, true);
            helpButton = Btn(header.transform, "?", new Vector2(80, 80), new Vector2(328, 0), Slate, Paper, ShowHelp);
            soundButton = Btn(header.transform, soundOn ? "SFX" : "OFF", new Vector2(100, 80), new Vector2(429, 0), Slate, Paper, ToggleSound);
            toast = Box("Toast", safe, new Vector2(880, 100), new Vector2(0, -134), Lime, 24, new Vector2(.5f, 1));
            toastText = Txt(toast.transform, "", 28, Ink, new Vector2(820, 88), Vector2.zero, true);
            toast.SetActive(false);
            UpdateSafeArea();
        }

        private void BuildHome(Transform page)
        {
            var content = ScrollPage(page, 1600, 132, 18);
            Txt(content, "KERJA SAMA. SEDIKIT KACAU.", 25, Lime, new Vector2(940, 52), new Vector2(0, -35), true, true);
            var poster = Box("Parcel poster", content, new Vector2(940, 640), new Vector2(0, -395), Yellow, 36);
            Txt(poster.transform, "PAKET\nPANIK", 114, Ink, new Vector2(530, 280), new Vector2(-160, 160), true, false, TextAnchor.MiddleLeft);
            Txt(poster.transform, "JAGA PAKETNYA.\nCURI ISINYA.", 26, Ink, new Vector2(470, 80), new Vector2(-175, -72), true, false, TextAnchor.MiddleLeft);
            Mascot(poster.transform, new Vector2(175, -60), .88f);
            var stamp = Box("Handle stamp", poster.transform, new Vector2(220, 76), new Vector2(302, 234), Ink, 12);
            stamp.transform.localRotation = Quaternion.Euler(0, 0, -9);
            Txt(stamp.transform, "AWAS GIGIT!", 23, Yellow, new Vector2(200, 64), Vector2.zero, true);
            Box("Divider", poster.transform, new Vector2(844, 2), new Vector2(0, -213), new Color(.06f, .1f, .17f, .3f), 0);
            Txt(poster.transform, "02 PEMAIN   /   90 DETIK   /   01 MEJA", 25, Ink, new Vector2(840, 60), new Vector2(0, -264), true);
            Txt(content, "Paket lucu. Teman panik.", 46, Paper, new Vector2(940, 76), new Vector2(0, -787), true, true);
            Txt(content, "Satu menenangkan monster, satu mengambil camilan.\nGantian sebelum lampumu habis!", 29, Muted, new Vector2(940, 100), new Vector2(0, -870), false, true);
            var kit = Box("What you need", content, new Vector2(940, 116), new Vector2(0, -995), Slate, 22);
            Txt(kit.transform, "2 HP ARCore    +    Wi-Fi sama    +    kartu 20 cm", 27, Paper, new Vector2(884, 94), Vector2.zero, true);
            hostButton = Btn(content, "Buat meja baru     +", new Vector2(940, 124), new Vector2(0, -1145), Lime, Ink, CreateRoom);
            Btn(content, "Gabung meja teman     >", new Vector2(940, 116), new Vector2(0, -1283), Slate, Paper, () => SetJoin(true));
            Btn(content, "Pertama main? Baca panduan", new Vector2(940, 94), new Vector2(0, -1402), Ink, Yellow, ShowHelp);
            homeStatus = Txt(content, "", 27, Coral, new Vector2(920, 90), new Vector2(0, -1502));
            Txt(content, "Tanpa akun. Kamera tetap di HP kamu.", 23, Muted, new Vector2(940, 48), new Vector2(0, -1570));
        }

        private void BuildJoin(Transform page)
        {
            // Fields and submit are near the top so the Android keyboard leaves them reachable.
            var content = ScrollPage(page, 1250, 134, 24);
            Txt(content, "01 / SAMBUNGKAN", 25, Lime, new Vector2(940, 48), new Vector2(0, -30), true, true);
            Txt(content, "Temanmu sudah\nbuka meja?", 65, Paper, new Vector2(940, 176), new Vector2(0, -164), true, true);
            Txt(content, "Salin IP dan PIN dari layar teman.\nPastikan kedua HP memakai Wi-Fi yang sama.", 30, Muted, new Vector2(940, 108), new Vector2(0, -322), false, true);
            Txt(content, "ALAMAT IP TEMAN", 25, Paper, new Vector2(940, 50), new Vector2(0, -430), true, true);
            addressInput = Field(content, "192.168.1.8", new Vector2(0, -518), 45, 45);
            addressInput.keyboardType = TouchScreenKeyboardType.NumbersAndPunctuation;
            Txt(content, "PIN MEJA", 25, Paper, new Vector2(940, 50), new Vector2(0, -623), true, true);
            pinInput = Field(content, "6 angka", new Vector2(0, -710), 48, 6);
            pinInput.contentType = InputField.ContentType.IntegerNumber; pinInput.keyboardType = TouchScreenKeyboardType.NumberPad;
            pinInput.characterValidation = InputField.CharacterValidation.Digit;
            joinSubmit = Btn(content, "Gabung sekarang     >", new Vector2(940, 124), new Vector2(0, -861), Lime, Ink, JoinRoom);
            joinSubmitText = joinSubmit.GetComponentInChildren<Text>();
            joinStatus = Txt(content, "", 28, Coral, new Vector2(930, 150), new Vector2(0, -1015));
            Btn(content, "Batal / kembali", new Vector2(940, 98), new Vector2(0, -1160), Slate, Paper, () => { if (session.Connecting) session.Leave(); SetJoin(false); });
            addressInput.onValueChanged.AddListener(_ => formError = "");
            pinInput.onValueChanged.AddListener(_ => formError = "");
        }

        private void BuildLobby()
        {
            roomCard = Box("Room card", lobby.transform, new Vector2(984, 278), new Vector2(0, -278), Paper, 28, new Vector2(.5f, 1));
            connectionStep = Txt(roomCard.transform, "01 / AJAK TEMAN", 23, Ink, new Vector2(590, 42), new Vector2(-150, 93), true, false, TextAnchor.MiddleLeft);
            roomCount = Txt(roomCard.transform, "1 / 2", 26, Ink, new Vector2(176, 52), new Vector2(350, 94), true);
            roomAddress = Txt(roomCard.transform, "", 31, Ink, new Vector2(650, 84), new Vector2(-115, 24), true, false, TextAnchor.MiddleLeft);
            roomPin = Txt(roomCard.transform, "", 30, Ink, new Vector2(840, 60), new Vector2(-20, -59), true, false, TextAnchor.MiddleLeft);
            Btn(roomCard.transform, "Salin", new Vector2(142, 90), new Vector2(356, 4), Ink, Paper, () =>
            {
                GUIUtility.systemCopyBuffer = roomAddress.text + "\n" + roomPin.text;
                Notify("IP dan PIN disalin. Kirim hanya ke temanmu.");
            });
            scanCard = Box("Scan card", lobby.transform, new Vector2(984, 698), new Vector2(0, 374), Ink, 30, new Vector2(.5f, 0));
            scanTitle = Txt(scanCard.transform, "02 / PINDAI KARTU", 35, Yellow, new Vector2(888, 66), new Vector2(0, 276), true, false, TextAnchor.MiddleLeft);
            scanBody = Txt(scanCard.transform, "", 29, Paper, new Vector2(888, 110), new Vector2(0, 183), false, false, TextAnchor.MiddleLeft);
            readiness = Txt(scanCard.transform, "", 26, Muted, new Vector2(888, 52), new Vector2(0, 91), true);
            readyButton = Btn(scanCard.transform, "Posisi cocok. Saya siap", new Vector2(888, 108), new Vector2(0, -3), Lime, Ink, Ready);
            readyText = readyButton.GetComponentInChildren<Text>();
            startButton = Btn(scanCard.transform, "Mulai ronde     >", new Vector2(888, 100), new Vector2(0, -125), Yellow, Ink, () => session.StartRound());
            startText = startButton.GetComponentInChildren<Text>();
            lobbyHint = Txt(scanCard.transform, "", 24, Muted, new Vector2(888, 65), new Vector2(0, -213));
            Btn(scanCard.transform, "Pindai ulang", new Vector2(420, 78), new Vector2(-232, -291), Slate, Paper, Rescan);
            Btn(scanCard.transform, "Lihat panduan", new Vector2(420, 78), new Vector2(232, -291), Slate, Paper, ShowHelp);
        }

        private void BuildHUD()
        {
            var card = Box("Game telemetry", hud.transform, new Vector2(984, 266), new Vector2(0, -280), Ink, 28, new Vector2(.5f, 1));
            Txt(card.transform, "ISI TAS BERSAMA", 22, Muted, new Vector2(380, 38), new Vector2(-262, 95), true, false, TextAnchor.MiddleLeft);
            score = Txt(card.transform, "", 60, Paper, new Vector2(440, 86), new Vector2(-232, 33), true, false, TextAnchor.MiddleLeft);
            timer = Txt(card.transform, "", 55, Yellow, new Vector2(240, 86), new Vector2(313, 62), true);
            angerText = Txt(card.transform, "", 23, Coral, new Vector2(540, 40), new Vector2(-181, -39), true, false, TextAnchor.MiddleLeft);
            angerFill = Meter(card.transform, new Vector2(602, 16), new Vector2(-151, -83), Coral);
            for (int i = 0; i < 3; i++)
            {
                var tooth = Box("Bite " + (i + 1), card.transform, new Vector2(66, 42), new Vector2(249 + i * 74, -78), Slate, 8);
                biteMarks[i] = tooth.GetComponent<PanicSurface>();
                Txt(tooth.transform, (i + 1).ToString(), 22, Paper, new Vector2(60, 40), Vector2.zero, true);
            }
            cue = Txt(hud.transform, "", 30, Paper, new Vector2(860, 110), new Vector2(0, -485), true, true);
            var cueBack = Box("Cue background", hud.transform, new Vector2(940, 124), new Vector2(0, -485), Alpha(Ink, .9f), 22, new Vector2(.5f, 1));
            cue.rectTransform.SetParent(cueBack.transform, false); cue.rectTransform.anchorMin = cue.rectTransform.anchorMax = new Vector2(.5f, .5f); cue.rectTransform.anchoredPosition = Vector2.zero;
            countdown = Box("Phase callout", hud.transform, new Vector2(600, 310), Vector2.zero, Alpha(Ink, .95f), 36);
            phaseNumber = Txt(countdown.transform, "", 114, Yellow, new Vector2(560, 170), new Vector2(0, 40), true);
            phaseText = Txt(countdown.transform, "", 29, Paper, new Vector2(550, 104), new Vector2(0, -84), true);
        }

        private void BuildControls()
        {
            var dock = Box("Thumb dock", controls.transform, new Vector2(984, 330), new Vector2(0, 188), Ink, 32, new Vector2(.5f, 0));
            partnerText = Txt(dock.transform, "", 24, Lime, new Vector2(888, 44), new Vector2(0, 124), true, false, TextAnchor.MiddleLeft);
            batteryText = Txt(dock.transform, "", 24, Paper, new Vector2(888, 40), new Vector2(0, 75), true, false, TextAnchor.MiddleLeft);
            batteryFill = Meter(dock.transform, new Vector2(888, 12), new Vector2(0, 40), Purple);
            actionButton = Btn(dock.transform, "", new Vector2(888, 142), new Vector2(0, -66), Purple, Ink, () => { });
            actionHold = actionButton.gameObject.AddComponent<HoldControl>();
            actionTitle = actionButton.GetComponentInChildren<Text>();
            actionTitle.rectTransform.sizeDelta = new Vector2(844, 54); actionTitle.rectTransform.anchoredPosition = new Vector2(0, 24); actionTitle.fontSize = 35;
            actionHint = Txt(actionButton.transform, "", 24, Ink, new Vector2(844, 44), new Vector2(0, -23));
            actionFill = Meter(actionButton.transform, new Vector2(802, 8), new Vector2(0, -58), Ink, Alpha(Ink, .13f));
            sealButton = Btn(controls.transform, "Tahan bersama untuk SEGEL", new Vector2(984, 106), new Vector2(0, 421), Yellow, Ink, () => { }, new Vector2(.5f, 0));
            sealHold = sealButton.gameObject.AddComponent<HoldControl>();
            sealFill = Meter(sealButton.transform, new Vector2(878, 8), new Vector2(0, -38), Ink, Alpha(Ink, .15f));
        }

        private void BuildResults(Transform page)
        {
            var content = ScrollPage(page, 1530, 138, 20);
            Txt(content, "BUKTI PENGIRIMAN", 25, Lime, new Vector2(940, 60), new Vector2(0, -38), true, true);
            var receipt = Box("Result receipt", content, new Vector2(940, 862), new Vector2(0, -530), Paper, 30);
            resultTitle = Txt(receipt.transform, "", 68, Ink, new Vector2(824, 178), new Vector2(0, 291), true);
            resultBody = Txt(receipt.transform, "", 28, Ink, new Vector2(824, 98), new Vector2(0, 153));
            Box("Receipt rule", receipt.transform, new Vector2(824, 3), new Vector2(0, 76), Ink, 0);
            resultScore = Txt(receipt.transform, "", 115, Ink, new Vector2(824, 156), new Vector2(0, -18), true);
            Txt(receipt.transform, "POIN DISELAMATKAN", 23, Ink, new Vector2(824, 50), new Vector2(0, -108), true);
            resultStats = Txt(receipt.transform, "", 30, Ink, new Vector2(824, 154), new Vector2(0, -225));
            for (int i = 0; i < 32; i++)
                Box("Receipt barcode", receipt.transform, new Vector2(i % 3 == 0 ? 9 : 4, 42), new Vector2(-308 + i * 20, -359), Ink, 0);
            resultReady = Btn(content, "Saya siap main lagi", new Vector2(940, 118), new Vector2(0, -1055), Lime, Ink, () => { resultAcknowledged = true; Ready(); });
            replayButton = Btn(content, "Main lagi     >", new Vector2(940, 108), new Vector2(0, -1192), Yellow, Ink, () => session.StartRound());
            replayText = replayButton.GetComponentInChildren<Text>();
            resultHint = Txt(content, "", 27, Muted, new Vector2(940, 120), new Vector2(0, -1320));
            Btn(content, "Kembali ke beranda", new Vector2(940, 100), new Vector2(0, -1458), Slate, Paper, Leave);
        }

        public void Render(GameState state, PlayerAction aim, int target)
        {
            if (!canvas) return;
            UpdateSafeArea();
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) NavigateBack();
            bool connected = session.Connected, finished = IsFinished(state.phase);
            backdrop.SetActive(!connected || finished || modal);
            bool preparing = state.phase == GamePhase.Lobby || state.phase == GamePhase.Paused;
            if (connected != wasConnected) { CloseModal(); roomRefreshAt = 0; formError = ""; wasConnected = connected; }
            if (state.phase != previousPhase) { resultAcknowledged = false; previousPhase = state.phase; }
            home.SetActive(!connected && !joiningPage); join.SetActive(!connected && joiningPage);
            lobby.SetActive(connected && preparing); results.SetActive(connected && finished);
            hud.SetActive(connected && !preparing && !finished);
            controls.SetActive(connected && state.phase == GamePhase.Playing);
            reticle.gameObject.SetActive(connected && state.phase == GamePhase.Playing && board.TrackingValid);
            backButton.gameObject.SetActive(connected || joiningPage);
            backButton.GetComponentInChildren<Text>().text = connected ? "Keluar" : "Kembali";
            helpButton.gameObject.SetActive(!connected || preparing || finished);
            toast.SetActive(Time.unscaledTime < toastUntil);
            hostButton.interactable = !session.Connecting;
            joinSubmit.interactable = !session.Connecting;
            addressInput.interactable = pinInput.interactable = !session.Connecting;
            joinSubmitText.text = session.Connecting ? "Menghubungkan..." : "Gabung sekarang     >";
            if (lastSessionStatus != session.Status) { formError = ""; lastSessionStatus = session.Status; }
            joinStatus.text = formError.Length > 0 ? formError : joinAttempted ? session.Status : "";
            joinStatus.color = session.Connecting ? Muted : Coral;
            homeStatus.text = session.Status == "Buat meja atau gabung teman." ? "" : session.Status;
            if (!connected) return;

            var player = state.players[session.LocalSlot]; var friend = state.players[1 - session.LocalSlot];
            bool bothReady = session.PlayerCount == 2 && state.players[0].ready && state.players[1].ready;
            if (!board.TrackingValid) session.LocalReady = false;
            if (Time.unscaledTime >= roomRefreshAt)
            {
                roomRefreshAt = Time.unscaledTime + 5;
                roomAddress.text = session.Hosting ? LanSession.LocalAddresses() : "Tersambung ke meja teman";
                roomPin.text = "PIN MEJA   " + session.Pin;
            }
            roomCount.text = session.PlayerCount + " / 2";
            connectionStep.text = session.PlayerCount == 2 ? "01 / TEMAN TERSAMBUNG" : "01 / BAGIKAN IP DAN PIN";
            scanTitle.text = state.phase == GamePhase.Paused ? "RONDE DIJEDA" : board.TrackingValid ? "03 / COCOKKAN POSISI" : "02 / PINDAI KARTU";
            scanBody.text = board.TrackingValid ? "Pastikan panah dan keempat sudut paket berada di posisi yang sama pada kedua HP." : board.Status;
            if (state.phase == GamePhase.Paused) scanBody.text = state.reason + "\n" + (board.TrackingValid ? "Posisi pulih. Kedua pemain tekan Siap." : board.Status);
            readiness.text = "KAMU: " + (session.LocalReady ? "SIAP" : "BELUM SIAP") + "    /    TEMAN: " + (friend.ready ? "SIAP" : session.PlayerCount < 2 ? "BELUM MASUK" : "BELUM SIAP");
            readyButton.interactable = board.TrackingValid;
            readyText.text = session.LocalReady ? "Siap! Ketuk untuk membatalkan" : "Posisi cocok. Saya siap";
            startButton.gameObject.SetActive(state.phase != GamePhase.Paused);
            startButton.interactable = session.Hosting && bothReady;
            startText.text = !session.Hosting ? "Menunggu host memulai" : bothReady ? "Mulai ronde     >" : "Menunggu kedua pemain siap";
            lobbyHint.text = state.phase == GamePhase.Paused ? "Ronde berlanjut otomatis saat keduanya siap." : "Duduk nyaman. Kartu tetap datar di meja.";
            if (board.simulator) lobbyHint.text = "SIMULATOR EDITOR / BUKAN UJI AR PERANGKAT";
            UpdateGameHUD(state, player, friend, aim, target);
            if (finished) UpdateResults(state, bothReady);
        }

        private void UpdateGameHUD(GameState state, PlayerState player, PlayerState friend, PlayerAction aim, int target)
        {
            score.text = state.score + " / " + session.Config.targetScore;
            timer.text = FormatTime(session.Config.roundSeconds - state.gameTime);
            timer.color = session.Config.roundSeconds - state.gameTime <= 20 ? Coral : Yellow;
            Fill(angerFill, state.anger / session.Config.anger.maximum);
            Fill(batteryFill, player.battery / session.Config.battery.capacity);
            angerText.text = "EMOSI PAKET   " + Mathf.RoundToInt(state.anger) + "%";
            for (int i = 0; i < biteMarks.Length; i++) biteMarks[i].color = i < state.bites ? Coral : Slate;
            batteryText.text = "LAMPU KAMU  " + Mathf.RoundToInt(player.battery) + "%";
            partnerText.text = "TEMAN: " + (friend.action == PlayerAction.Guard ? "MENJAGA PAKET" : friend.action == PlayerAction.Loot ? "MENGAMBIL CAMILAN" : friend.action == PlayerAction.Seal ? "MENUNGGU SEGELMU" : "BERSIAP");
            cue.text = "Satu jaga paket. Satu ambil camilan.";
            if (!board.TrackingValid) cue.text = board.Status;
            else if (state.phase == GamePhase.Playing)
            {
                if (state.gameTime >= session.Config.anger.finalStartsAt) cue.text = "KURIR DATANG! Ambil lagi atau segel?";
                if (state.score >= session.Config.targetScore) cue.text = "Target tercapai! Tahan SEGEL bersama.";
                foreach (float sneeze in session.Config.sneezeAt)
                    if (sneeze > state.gameTime && sneeze - state.gameTime <= session.Config.sneezeWarningSeconds) cue.text = "AWAS, PAKET MAU BERSIN!";
                if (player.battery < 20) cue.text = "LAMPU MENIPIS! Lepaskan, lalu gantian.";
            }
            cue.color = player.battery < 20 || state.anger > 75 ? Coral : Paper;
            countdown.SetActive(state.phase == GamePhase.Countdown || state.phase == GamePhase.Recovery);
            phaseNumber.text = state.phase == GamePhase.Recovery ? "ADUH!" : Mathf.CeilToInt(state.phaseRemaining).ToString();
            phaseText.text = state.phase == GamePhase.Recovery ? "Kena gigit. Tenang, ganti penjaga!" : "Siapkan jempol. Satu jaga, satu ambil.";
            if (board.simulator) cue.text = "SIMULATOR / BUKAN AR PERANGKAT\n" + cue.text;
            sealButton.gameObject.SetActive(state.score >= session.Config.targetScore && state.phase == GamePhase.Playing);
            Fill(sealFill, state.sealProgress / session.Config.sealHoldSeconds);
            bool validAim = aim != PlayerAction.Idle && board.TrackingValid;
            actionButton.interactable = validAim;
            actionButton.GetComponent<PanicSurface>().color = aim == PlayerAction.Loot ? Yellow : Purple;
            float progress = LootFraction(player, state, session.Config, target);
            Fill(actionFill, aim == PlayerAction.Loot ? progress : 0);
            actionTitle.text = aim == PlayerAction.Guard ? (ActionHeld ? "MENENANGKAN..." : "TAHAN UNTUK MENJAGA") :
                aim == PlayerAction.Loot ? (ActionHeld ? "MENGAMBIL   " + Mathf.RoundToInt(progress * 100) + "%" : "TAHAN UNTUK MENGAMBIL") : "ARAHKAN BIDIKAN";
            actionHint.text = aim == PlayerAction.Guard ? "Lepas untuk mengisi lampu. Gantian, ya!" :
                aim == PlayerAction.Loot ? "Jaga bidikan pada camilan sampai penuh" : "Ke paket untuk jaga, ke camilan untuk ambil";
            if (aim == PlayerAction.Guard && player.battery < session.Config.battery.minimumToStart && !ActionHeld)
            { actionTitle.text = "LAMPU MENGISI..."; actionHint.text = "Minta teman menjaga sebentar"; }
        }

        private void UpdateResults(GameState state, bool bothReady)
        {
            resultTitle.text = state.phase == GamePhase.Won ? "PAKET\nDISELAMATKAN!" : state.phase == GamePhase.Lost ? "YAH,\nKENA GIGIT." : "PENGIRIMAN\nTERPUTUS.";
            resultBody.text = state.phase == GamePhase.Won ? "Kerja sama kalian boleh juga.\nBerani ambil lebih banyak di ronde berikutnya?" : state.reason;
            resultScore.text = state.score.ToString("00");
            resultStats.text = "KAMU  " + state.players[session.LocalSlot].collected + " poin     /     TEMAN  " + state.players[1 - session.LocalSlot].collected + " poin\nGIGITAN  " + state.bites + " / " + session.Config.bite.maximumBites;
            resultReady.interactable = board.TrackingValid && !session.LocalReady;
            resultReady.GetComponentInChildren<Text>().text = session.LocalReady ? "Kamu siap. Tunggu temanmu..." : board.TrackingValid ? "Saya siap main lagi" : "Pindai kartu dulu untuk main lagi";
            replayButton.interactable = session.Hosting && bothReady;
            replayText.text = session.Hosting ? "Main lagi     >" : "Menunggu host memulai";
            resultHint.text = session.PlayerCount < 2 ? "Teman terputus. Kembali ke beranda dan buat meja lagi." :
                !board.TrackingValid ? board.Status : resultAcknowledged ? "Kedua pemain harus siap sebelum ronde dimulai." : "Siap balas dendam? Pastikan posisi kartu masih cocok.";
        }

        public static string FormatTime(float seconds)
        {
            int total = Mathf.CeilToInt(Mathf.Max(0, seconds));
            return (total / 60).ToString("00") + ":" + (total % 60).ToString("00");
        }
        public static float LootFraction(PlayerState player, GameState state, GameConfig config, int target)
        {
            if (player.action != PlayerAction.Loot || player.target != target) return 0;
            foreach (var item in state.items)
                if (item.id == target) return Mathf.Clamp01(player.progress / Mathf.Max(.01f, item.gold ? config.items.gold.holdSeconds : config.items.regular.holdSeconds));
            return 0;
        }
        private void CreateRoom() { formError = ""; if (session.Host()) board.BeginCamera(); }
        private void SetJoin(bool value) { joiningPage = value; formError = ""; joinAttempted = false; }
        private void JoinRoom()
        {
            if (!LanSession.IsLocalAddress(addressInput.text)) { formError = "IP belum cocok. Salin alamat Wi-Fi dari layar host."; return; }
            if (pinInput.text.Length != 6) { formError = "PIN harus 6 angka. Lihat PIN di layar temanmu."; return; }
            EventSystem.current?.SetSelectedGameObject(null);
            joinAttempted = true;
            if (session.Join(addressInput.text.Trim(), pinInput.text)) board.BeginCamera();
        }
        private void Ready() { board.BeginCamera(); session.LocalReady = board.TrackingValid && !session.LocalReady; }
        private void Rescan() { session.LocalReady = false; board.Recalibrate(); board.BeginCamera(); }
        private void Leave() { CloseModal(); session.Leave(); joiningPage = false; }
        private void NavigateBack()
        {
            if (modal) { CloseModal(); return; }
            if (!session.Connected) { if (session.Connecting) session.Leave(); SetJoin(false); return; }
            ShowLeave();
        }
        private void ToggleSound()
        {
            soundOn = !soundOn; presentation.SetMuted(!soundOn);
            PlayerPrefs.SetInt("pp.sound", soundOn ? 1 : 0); PlayerPrefs.Save();
            soundButton.GetComponentInChildren<Text>().text = soundOn ? "SFX" : "OFF";
            Notify(soundOn ? "Suara aktif" : "Suara dimatikan");
        }
        private void Notify(string text) { toastText.text = text; toastUntil = Time.unscaledTime + 2.5f; toast.transform.SetAsLastSibling(); }
        private void OnEvent(GameEvent ev)
        {
            if (ev.type == "Collect") Notify(ev.actor == session.LocalSlot ? "Masuk tas! Kamu mengambil camilan." : "Temanmu mendapat camilan!");
            else if (ev.type == "Bite") Notify("Kena gigit! Ganti penjaga, coba lagi.");
        }
        private void ShowLeave()
        {
            actionHold.Release(); sealHold.Release();
            CloseModal(); modal = Page("Leave confirmation"); modal.transform.SetAsLastSibling();
            var content = ScrollPage(modal.transform, 850, 140, 40);
            Txt(content, "KELUAR DARI MEJA?", 53, Yellow, new Vector2(940, 160), new Vector2(0, -180), true);
            Txt(content, session.Hosting ? "Kamu adalah host. Keluar akan memutus meja teman juga.\n\nRonde tetap berjalan selama layar ini terbuka." : "Koneksi ke teman akan terputus.\n\nRonde tetap berjalan selama layar ini terbuka.", 32, Paper, new Vector2(900, 220), new Vector2(0, -387));
            Btn(content, "Tetap main", new Vector2(940, 124), new Vector2(0, -594), Lime, Ink, CloseModal);
            Btn(content, "Ya, keluar dari meja", new Vector2(940, 110), new Vector2(0, -735), Slate, Coral, Leave);
        }
        private void ShowHelp()
        {
            actionHold.Release(); sealHold.Release();
            CloseModal(); modal = Page("Guide"); modal.transform.SetAsLastSibling();
            var content = ScrollPage(modal.transform, 2390, 30, 146);
            Txt(content, "PANDUAN SINGKAT", 28, Lime, new Vector2(940, 60), new Vector2(0, -50), true, true);
            Txt(content, "Dua teman.\nSatu paket nakal.", 63, Paper, new Vector2(940, 180), new Vector2(0, -194), true, true);
            HelpStep(content, 1, "Siapkan meja", "Gunakan dua HP Android yang mendukung ARCore. Cetak kartu dari paket download selebar 20 cm, lalu taruh datar di meja terang.", 430);
            HelpStep(content, 2, "Buka meja, ajak teman", "Hubungkan ke Wi-Fi yang sama. Satu pemain memilih Buat meja. Temannya memilih Gabung, lalu mengisi IP dan PIN dari layar host.", 707);
            HelpStep(content, 3, "Pindai, cocokkan, siap", "Kedua HP memindai kartu yang sama. Cocokkan panah dan empat sudut. Tekan Siap di kedua HP, lalu host menekan Mulai ronde.", 984);
            HelpStep(content, 4, "Satu jaga, satu ambil", "Bidik paket lalu tahan untuk menenangkan. Teman membidik camilan lalu menahan sampai penuh. Lepas tombol dan gantian sebelum lampu habis.", 1261);
            HelpStep(content, 5, "Selamatkan isinya", "Camilan biasa 1 poin, emas 3 poin. Raih 12 poin lalu tahan Segel bersama. Tiga gigitan membuat kalian kalah. Tonton emosi dan sisa waktu!", 1538);
            Txt(content, "MAIN NYAMAN", 27, Yellow, new Vector2(940, 60), new Vector2(0, -1754), true, true);
            Txt(content, "Duduk saja, tidak perlu berlari atau mengguncang HP. Jauhkan minuman dari tepi meja. Berhenti jika tidak nyaman. Jika tracking hilang, arahkan ke kartu dan tekan Siap kembali.", 29, Paper, new Vector2(940, 174), new Vector2(0, -1870), false, true);
            Txt(content, "KAMERA DAN KONEKSI", 27, Yellow, new Vector2(940, 60), new Vector2(0, -2035), true, true);
            Txt(content, "Kamera dipakai untuk AR di HP dan tidak dikirim ke teman. Game mengirim status permainan melalui jaringan lokal. Tanpa akun, iklan, atau voice chat. Gunakan Wi-Fi tepercaya; PIN bukan enkripsi.\n\nKamera ditolak? Izinkan di pengaturan aplikasi lalu pindai lagi. Gagal gabung? Periksa IP, PIN, Wi-Fi dan isolasi perangkat. ARCore mungkin memerlukan Google Play Services for AR.", 28, Muted, new Vector2(940, 282), new Vector2(0, -2208), false, true);
            Btn(modal.transform, "Mengerti, ayo main", new Vector2(940, 112), new Vector2(0, 78), Lime, Ink, CloseModal, new Vector2(.5f, 0));
        }
        private void HelpStep(Transform parent, int number, string title, string body, float y)
        {
            var card = Box("Guide step " + number, parent, new Vector2(940, 252), new Vector2(0, -y), Slate, 24);
            Txt(card.transform, number.ToString("00"), 40, Lime, new Vector2(90, 70), new Vector2(-382, 59), true);
            Txt(card.transform, title, 36, Paper, new Vector2(706, 70), new Vector2(59, 59), true, false, TextAnchor.MiddleLeft);
            Txt(card.transform, body, 28, Muted, new Vector2(830, 124), new Vector2(0, -47), false, false, TextAnchor.MiddleLeft);
        }
        private void CloseModal() { if (modal) { modal.SetActive(false); Destroy(modal); modal = null; } }

        private void UpdateSafeArea()
        {
            if (Screen.width == lastWidth && Screen.height == lastHeight && lastSafe == Screen.safeArea) return;
            lastWidth = Screen.width; lastHeight = Screen.height; lastSafe = Screen.safeArea;
            Vector2 screen = new Vector2(Mathf.Max(1, Screen.width), Mathf.Max(1, Screen.height));
            safe.anchorMin = lastSafe.position / screen; safe.anchorMax = (lastSafe.position + lastSafe.size) / screen;
            safe.offsetMin = safe.offsetMax = Vector2.zero;
            // Keep reference-width controls inside horizontal safe insets as well as top/bottom notches.
            canvas.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1080 * screen.x / Mathf.Max(1, lastSafe.width), 1920);
        }

        private GameObject Page(string name)
        {
            var page = Empty(name, safe); Stretch((RectTransform)page.transform);
            var back = Box("Backdrop", page.transform, Vector2.zero, Vector2.zero, Ink, 0);
            Stretch((RectTransform)back.transform);
            return page;
        }
        private RectTransform ScrollPage(Transform parent, float height, float top, float bottom)
        {
            var view = Rect("Scroll viewport", parent); Stretch(view);
            view.offsetMin = new Vector2(30, bottom); view.offsetMax = new Vector2(-30, -top);
            view.gameObject.AddComponent<RectMask2D>();
            var scroll = view.gameObject.AddComponent<ScrollRect>();
            var hit = view.gameObject.AddComponent<Image>(); hit.color = Color.clear;
            var content = Rect("Content", view); content.anchorMin = new Vector2(0, 1); content.anchorMax = Vector2.one;
            content.pivot = new Vector2(.5f, 1); content.sizeDelta = new Vector2(0, height); content.anchoredPosition = Vector2.zero;
            scroll.content = content; scroll.viewport = view; scroll.horizontal = false; scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 45; scroll.decelerationRate = .09f;
            return content;
        }
        private RectTransform Rect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform)); go.transform.SetParent(parent, false);
            return go.GetComponent<RectTransform>();
        }
        private GameObject Empty(string name, Transform parent) => Rect(name, parent).gameObject;
        private static void Stretch(RectTransform rect)
        { rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero; }
        private GameObject Box(string name, Transform parent, Vector2 size, Vector2 position, Color color, float radius = 24, Vector2? anchor = null)
        {
            var rt = Rect(name, parent); rt.anchorMin = rt.anchorMax = anchor ?? (parent.name == "Content" ? new Vector2(.5f, 1) : new Vector2(.5f, .5f));
            rt.sizeDelta = size; rt.anchoredPosition = position;
            var graphic = rt.gameObject.AddComponent<PanicSurface>(); graphic.color = color; graphic.radius = radius;
            return rt.gameObject;
        }
        private Text Txt(Transform parent, string value, int size, Color color, Vector2 dimensions, Vector2 position, bool bold = false, bool top = false, TextAnchor alignment = TextAnchor.MiddleCenter)
        {
            var rt = Rect("Text", parent); rt.anchorMin = rt.anchorMax = top || parent.name == "Content" ? new Vector2(.5f, 1) : new Vector2(.5f, .5f);
            rt.sizeDelta = dimensions; rt.anchoredPosition = position;
            var text = rt.gameObject.AddComponent<Text>(); text.font = font; text.fontSize = size; text.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
            text.color = color; text.text = value; text.alignment = top ? TextAnchor.MiddleLeft : alignment; text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Wrap; text.verticalOverflow = VerticalWrapMode.Truncate;
            return text;
        }
        private Button Btn(Transform parent, string title, Vector2 size, Vector2 position, Color color, Color textColor, UnityAction callback, Vector2? anchor = null)
        {
            var go = Box(title.Length > 0 ? title : "Hold action", parent, size, position, color, 22, anchor);
            var button = go.AddComponent<Button>(); button.targetGraphic = go.GetComponent<PanicSurface>();
            var colors = button.colors; colors.normalColor = Color.white; colors.highlightedColor = new Color(.94f, .97f, 1);
            colors.pressedColor = new Color(.75f, .82f, .91f); colors.disabledColor = new Color(.52f, .56f, .62f); colors.fadeDuration = .08f; button.colors = colors;
            button.onClick.AddListener(callback);
            Txt(go.transform, title, size.x < 160 ? 24 : 32, textColor, size - new Vector2(44, 12), Vector2.zero, true);
            return button;
        }
        private InputField Field(Transform parent, string placeholder, Vector2 position, int size, int limit)
        {
            var go = Box("Input " + placeholder, parent, new Vector2(940, 122), position, Slate, 22);
            var input = go.AddComponent<InputField>(); input.targetGraphic = go.GetComponent<PanicSurface>();
            input.textComponent = Txt(go.transform, "", size, Paper, new Vector2(848, 98), Vector2.zero, false, false, TextAnchor.MiddleLeft);
            input.placeholder = Txt(go.transform, placeholder, 34, Muted, new Vector2(848, 98), Vector2.zero, false, false, TextAnchor.MiddleLeft);
            input.characterLimit = limit; input.caretColor = Lime; input.customCaretColor = true; input.caretWidth = 3;
            input.selectionColor = Alpha(Purple, .4f); input.textComponent.supportRichText = false;
            return input;
        }
        private RectTransform Meter(Transform parent, Vector2 size, Vector2 position, Color color, Color? background = null)
        {
            var back = Box("Meter track", parent, size, position, background ?? Slate, 4);
            back.GetComponent<PanicSurface>().raycastTarget = false;
            var fill = Box("Meter fill", back.transform, Vector2.zero, Vector2.zero, color, 4);
            var rect = (RectTransform)fill.transform; Stretch(rect); fill.GetComponent<PanicSurface>().raycastTarget = false;
            return rect;
        }
        private static void Fill(RectTransform fill, float fraction) { fill.anchorMax = new Vector2(Mathf.Clamp01(fraction), 1); }
        private void Mascot(Transform parent, Vector2 position, float scale)
        {
            var root = Rect("Original parcel mascot", parent); root.anchoredPosition = position; root.localScale = Vector3.one * scale; root.localRotation = Quaternion.Euler(0, 0, -9);
            Box("Shadow", root, new Vector2(342, 34), new Vector2(10, -172), Alpha(Ink, .16f), 17);
            Box("Box side", root, new Vector2(310, 250), new Vector2(22, -21), C("A36639"), 18);
            Box("Box front", root, new Vector2(286, 250), new Vector2(-10, 0), C("DE9B53"), 18);
            Box("Tape", root, new Vector2(56, 249), new Vector2(-10, 0), Purple, 0);
            Box("Lid shadow", root, new Vector2(330, 38), new Vector2(-6, 95), Ink, 6);
            var lid = Box("Lid", root, new Vector2(346, 60), new Vector2(-18, 136), C("EDB774"), 10);
            lid.transform.localRotation = Quaternion.Euler(0, 0, 8);
            for (int i = -1; i <= 1; i += 2)
            {
                Box("Eye", root, new Vector2(78, 89), new Vector2(i * 68 - 10, 52), Paper, 39);
                Box("Pupil", root, new Vector2(30, 42), new Vector2(i * 68, 47), Ink, 15);
            }
            Box("Grin", root, new Vector2(164, 52), new Vector2(-8, -32), Ink, 22);
            for (int i = 0; i < 3; i++) Box("Tooth", root, new Vector2(26, 27), new Vector2(-56 + i * 46, -13), Paper, 5);
            var label = Box("Shipping label", root, new Vector2(115, 61), new Vector2(65, -92), Paper, 4);
            Txt(label.transform, "ISI: RAHASIA", 14, Ink, new Vector2(110, 55), Vector2.zero, true);
            foreach (var graphic in root.GetComponentsInChildren<Graphic>()) graphic.raycastTarget = false;
        }
        private static bool IsFinished(GamePhase phase) => phase == GamePhase.Won || phase == GamePhase.Lost || phase == GamePhase.Aborted;
        private static Color C(string hex) { ColorUtility.TryParseHtmlString("#" + hex, out Color c); return c; }
        private static Color Alpha(Color color, float alpha) { color.a = alpha; return color; }
        private void OnDestroy() { if (session) session.GameEventReceived -= OnEvent; if (canvas) Destroy(canvas.gameObject); }
    }
}
