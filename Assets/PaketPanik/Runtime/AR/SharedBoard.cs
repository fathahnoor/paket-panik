using System.Collections;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
#if UNITY_ANDROID
using UnityEngine.Android;
#endif

namespace PaketPanik
{
    public sealed class SharedBoard : MonoBehaviour
    {
        public Transform boardRoot;
        public Camera arCamera;
        public ARTrackedImageManager images;
        public ARAnchorManager anchors;
        public ARSession session;
        public bool simulator;
        public bool Calibrated { get; private set; }
        public bool TrackingValid => simulator || (Calibrated && ARSession.state == ARSessionState.SessionTracking && anchor != null && anchor.trackingState == TrackingState.Tracking);
        public string Status { get; private set; } = "Pilih Mulai kamera untuk memindai kartu.";
        public bool Supported { get; private set; }
        private ARAnchor anchor;
        private ARTrackedImage candidate;
        private float stableSince;
        private Vector3 lastPosition;
        private Quaternion lastRotation;
        private bool creatingAnchor, cameraStarted;

        private void Start()
        {
            if (simulator)
            {
                Calibrated = Supported = true; boardRoot.gameObject.SetActive(true);
                Status = "SIMULATOR EDITOR / BUKAN BUKTI AR PERANGKAT";
            }
            else boardRoot.gameObject.SetActive(false);
        }
        public void BeginCamera()
        {
            if (simulator || cameraStarted) return;
            cameraStarted = true; StartCoroutine(EnableCamera());
        }
        private IEnumerator EnableCamera()
        {
            Status = "Memeriksa dukungan ARCore...";
#if UNITY_ANDROID && !UNITY_EDITOR
            if (!Permission.HasUserAuthorizedPermission(Permission.Camera))
            {
                Permission.RequestUserPermission(Permission.Camera);
                float deadline = Time.realtimeSinceStartup + 15;
                while (!Permission.HasUserAuthorizedPermission(Permission.Camera) && Time.realtimeSinceStartup < deadline) yield return null;
                if (!Permission.HasUserAuthorizedPermission(Permission.Camera))
                { Status = "Izin kamera belum diberikan. Aktifkan pada pengaturan aplikasi lalu coba lagi."; cameraStarted = false; yield break; }
            }
#endif
            yield return ARSession.CheckAvailability();
            if (ARSession.state == ARSessionState.NeedsInstall)
            {
                Status = "Ikuti permintaan Google Play Services for AR.";
                yield return ARSession.Install();
            }
            if (ARSession.state == ARSessionState.Unsupported || ARSession.state == ARSessionState.NeedsInstall)
            { Status = "ARCore belum tersedia pada HP ini. Lihat panduan kompatibilitas."; cameraStarted = false; yield break; }
            Supported = true; session.enabled = true; images.enabled = true; anchors.enabled = true;
            var manager = arCamera.GetComponent<ARCameraManager>(); if (manager) manager.enabled = true;
            var background = arCamera.GetComponent<ARCameraBackground>(); if (background) background.enabled = true;
            Status = "Arahkan kamera ke kartu 20 cm, di meja terang dan datar.";
        }
        private void Update()
        {
            if (simulator || !cameraStarted || !Supported) return;
            ARTrackedImage visible = null;
            foreach (var item in images.trackables)
                if (item.trackingState == TrackingState.Tracking) { visible = item; break; }
            if (Calibrated)
            {
                if (!TrackingValid) Status = "Tracking melemah. Arahkan ke kartu lagi.";
                else Status = "Posisi terkunci. Pastikan kedua HP melihat sudut yang sama.";
                if (visible && anchor && (Vector3.Distance(visible.transform.position, anchor.transform.position) > .03f || Quaternion.Angle(visible.transform.rotation, anchor.transform.rotation) > 5))
                { Calibrated = false; Status = "Kartu bergeser. Kalibrasi ulang kedua HP."; }
                return;
            }
            if (!visible) { candidate = null; stableSince = Time.unscaledTime; return; }
            if (candidate != visible || Vector3.Distance(lastPosition, visible.transform.position) > .01f || Quaternion.Angle(lastRotation, visible.transform.rotation) > 3)
            { candidate = visible; stableSince = Time.unscaledTime; }
            lastPosition = visible.transform.position; lastRotation = visible.transform.rotation;
            if (!creatingAnchor && Time.unscaledTime - stableSince >= .5f) LockBoard(visible.transform.position, visible.transform.rotation);
        }
        private async void LockBoard(Vector3 position, Quaternion rotation)
        {
            creatingAnchor = true;
            try
            {
                var result = await anchors.TryAddAnchorAsync(new Pose(position, rotation));
                if (!this) return;
                if (result.status.IsSuccess())
                {
                    if (anchor) Destroy(anchor.gameObject);
                    anchor = result.value; boardRoot.SetParent(anchor.transform, false);
                    boardRoot.localPosition = Vector3.zero; boardRoot.localRotation = Quaternion.identity; boardRoot.localScale = Vector3.one;
                    Calibrated = true; boardRoot.gameObject.SetActive(true);
                    Status = "Posisi terkunci. Periksa panah dan empat sudut.";
                }
                else { Status = "Belum berhasil mengunci kartu. Tahan kamera sebentar."; stableSince = Time.unscaledTime; }
            }
            catch (System.Exception e) { Status = "Kalibrasi belum tersedia. Coba pindai ulang."; Debug.LogWarning("Board anchor: " + e.GetType().Name); }
            finally { creatingAnchor = false; }
        }
        public void Recalibrate()
        {
            if (simulator) return;
            Calibrated = false; candidate = null; stableSince = Time.unscaledTime;
            boardRoot.gameObject.SetActive(false); Status = "Pindai ulang kartu yang sama pada kedua HP.";
        }
        public Ray BoardRay()
        {
            var ray = arCamera.ViewportPointToRay(new Vector3(.5f, .5f));
            return new Ray(boardRoot.InverseTransformPoint(ray.origin), boardRoot.InverseTransformDirection(ray.direction).normalized);
        }
    }
}
