using UnityEngine;

namespace Overhaul.Game
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Camera))]
    public sealed class ThirdPersonDriveCamera : MonoBehaviour
    {
        [SerializeField] private Vector3 chaseOffset = new Vector3(0f, 4.2f, -7f);
        [SerializeField] private Vector3 lookOffset = new Vector3(0f, 1.1f, 1.7f);
        [SerializeField] private float positionSharpness = 7f;
        [SerializeField] private float rotationSharpness = 10f;
        [SerializeField] private float driveFieldOfView = 55f;

        private Camera _camera;
        private Transform _target;
        private Vector3 _restPosition;
        private Quaternion _restRotation;
        private float _restFieldOfView;

        public bool IsFollowing => _target != null;

        private void Awake()
        {
            _camera = GetComponent<Camera>();
            RememberRestPose();
        }

        public void Follow(Transform target)
        {
            if (_target == null) RememberRestPose();
            _target = target;
            if (_camera != null) _camera.fieldOfView = driveFieldOfView;
        }

        public void StopFollowing()
        {
            _target = null;
            transform.SetPositionAndRotation(_restPosition, _restRotation);
            if (_camera != null) _camera.fieldOfView = _restFieldOfView;
        }

        private void LateUpdate()
        {
            if (_target == null) return;

            Vector3 desiredPosition = _target.TransformPoint(chaseOffset);
            Vector3 lookPoint = _target.TransformPoint(lookOffset);
            float positionT = 1f - Mathf.Exp(-positionSharpness * Time.deltaTime);
            float rotationT = 1f - Mathf.Exp(-rotationSharpness * Time.deltaTime);
            transform.position = Vector3.Lerp(transform.position, desiredPosition, positionT);
            var desiredRotation = Quaternion.LookRotation(lookPoint - transform.position, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, desiredRotation, rotationT);
        }

        private void RememberRestPose()
        {
            _restPosition = transform.position;
            _restRotation = transform.rotation;
            if (_camera == null) _camera = GetComponent<Camera>();
            _restFieldOfView = _camera.fieldOfView;
        }
    }
}
