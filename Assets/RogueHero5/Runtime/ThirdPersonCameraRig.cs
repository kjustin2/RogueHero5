using UnityEngine;
using UnityEngine.InputSystem;

namespace RogueHero5
{
    public sealed class ThirdPersonCameraRig : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private Transform secondaryTarget;
        [SerializeField] private float distance = 8f;
        [SerializeField] private float height = 3.2f;
        [SerializeField] private float pitch = 28f;
        [SerializeField] private float yaw;
        [SerializeField] private float mouseSensitivity = 0.12f;
        [SerializeField] private float smoothTime = 0.08f;

        private Vector3 positionVelocity;
        private float shakeRemaining;
        private float shakeStrength;
        private float fovKick;
        private Camera cachedCamera;
        private float baseFov = 60f;

        public Vector3 ForwardOnPlane
        {
            get
            {
                Vector3 forward = transform.forward;
                forward.y = 0f;
                return forward.sqrMagnitude > 0.001f ? forward.normalized : Vector3.forward;
            }
        }

        public Vector3 RightOnPlane
        {
            get
            {
                Vector3 right = transform.right;
                right.y = 0f;
                return right.sqrMagnitude > 0.001f ? right.normalized : Vector3.right;
            }
        }

        public void Configure(Transform newTarget)
        {
            target = newTarget;
            cachedCamera = GetComponent<Camera>();
            if (cachedCamera != null)
            {
                baseFov = cachedCamera.fieldOfView;
            }
        }

        public void SetSecondaryTarget(Transform newSecondaryTarget)
        {
            secondaryTarget = newSecondaryTarget;
        }

        public void AddImpulse(float shake, float newFovKick)
        {
            shakeStrength = Mathf.Max(shakeStrength, shake);
            shakeRemaining = Mathf.Max(shakeRemaining, 0.18f);
            fovKick = Mathf.Max(fovKick, newFovKick);
        }

        private void LateUpdate()
        {
            if (target == null)
            {
                return;
            }

            if (Mouse.current != null && Cursor.lockState == CursorLockMode.Locked)
            {
                Vector2 mouseDelta = Mouse.current.delta.ReadValue();
                yaw += mouseDelta.x * mouseSensitivity;
                pitch = Mathf.Clamp(pitch - mouseDelta.y * mouseSensitivity, 12f, 55f);
            }

            Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
            Vector3 focus = GetFocusPoint();
            float dynamicDistance = GetDynamicDistance();
            Vector3 desiredPosition = focus + rotation * new Vector3(0f, height * 0.15f, -dynamicDistance);
            transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref positionVelocity, smoothTime);

            if (shakeRemaining > 0f)
            {
                shakeRemaining -= Time.unscaledDeltaTime;
                transform.position += Random.insideUnitSphere * shakeStrength * Mathf.Clamp01(shakeRemaining / 0.18f);
            }

            transform.rotation = Quaternion.LookRotation(focus - transform.position, Vector3.up);

            if (cachedCamera == null)
            {
                cachedCamera = GetComponent<Camera>();
            }

            if (cachedCamera != null)
            {
                fovKick = Mathf.MoveTowards(fovKick, 0f, Time.unscaledDeltaTime * 8f);
                cachedCamera.fieldOfView = Mathf.Lerp(cachedCamera.fieldOfView, baseFov + fovKick, Time.unscaledDeltaTime * 10f);
            }
        }

        private Vector3 GetFocusPoint()
        {
            if (secondaryTarget == null)
            {
                return target.position + Vector3.up * 1.2f;
            }

            Vector3 midpoint = Vector3.Lerp(target.position, secondaryTarget.position, 0.45f);
            return midpoint + Vector3.up * 1.25f;
        }

        private float GetDynamicDistance()
        {
            if (secondaryTarget == null)
            {
                return distance;
            }

            float targetDistance = Vector3.Distance(target.position, secondaryTarget.position);
            return Mathf.Clamp(distance + targetDistance * 0.24f, distance, distance + 3f);
        }
    }
}
