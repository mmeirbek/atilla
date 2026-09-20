using UnityEngine;

namespace ATTILA.CameraSystem
{
    [RequireComponent(typeof(Camera))]
    public sealed class CameraController : MonoBehaviour
    {
        /// <summary>One world unit is 30 pixels, so the camera shows exactly PixelCamera.TargetHeight pixels.</summary>
        private const float PixelUnit = 1f / 30f;
        [SerializeField] private Transform target;
        [SerializeField] private float followSmoothTime = .18f;
        [SerializeField] private float orthographicSize = 9f;
        private Vector3 followVelocity;
        private Vector3 smoothed;
        private static readonly Quaternion ViewRotation = Quaternion.Euler(40f, 0f, 0f);
        private Vector3 DesiredPosition => target.position - ViewRotation * Vector3.forward * 24f;

        private void Awake()
        {
            var camera = GetComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = orthographicSize;
            camera.nearClipPlane = .1f;
            camera.farClipPlane = 100f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            transform.rotation = ViewRotation;
        }

        public void SetTarget(Transform value)
        {
            target = value;
            followVelocity = Vector3.zero;
            if (target == null) return;
            smoothed = DesiredPosition;
            transform.position = Snap(smoothed);
        }

        private void LateUpdate()
        {
            if (target == null) return;
            smoothed = Vector3.SmoothDamp(smoothed, DesiredPosition, ref followVelocity, followSmoothTime);
            transform.position = Snap(smoothed);
            transform.rotation = ViewRotation;
        }

        /// <summary>Keeps the camera on whole pixels of the low-resolution buffer, which stops textures shimmering.</summary>
        private static Vector3 Snap(Vector3 position)
        {
            var local = Quaternion.Inverse(ViewRotation) * position;
            local.x = Mathf.Round(local.x / PixelUnit) * PixelUnit;
            local.y = Mathf.Round(local.y / PixelUnit) * PixelUnit;
            return ViewRotation * local;
        }
    }
}
