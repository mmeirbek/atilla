using UnityEngine;

namespace ATTILA.CameraSystem
{
    [RequireComponent(typeof(Camera))]
    public sealed class CameraController : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private float followSmoothTime = .18f;
        [SerializeField] private float orthographicSize = 10f;
        private Vector3 followVelocity;
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
            if (target != null) transform.position = DesiredPosition;
        }
        private void LateUpdate()
        {
            if (target == null) return;
            transform.position = Vector3.SmoothDamp(transform.position, DesiredPosition, ref followVelocity, followSmoothTime);
            transform.rotation = ViewRotation;
        }
    }
}
