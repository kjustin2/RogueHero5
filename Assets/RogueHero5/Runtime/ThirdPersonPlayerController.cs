using UnityEngine;
using UnityEngine.InputSystem;

namespace RogueHero5
{
    [DisallowMultipleComponent]
    public sealed class ThirdPersonPlayerController : MonoBehaviour
    {
        [SerializeField] private FighterActor actor;
        [SerializeField] private MoveRunner moveRunner;
        [SerializeField] private ThirdPersonCameraRig cameraRig;
        [SerializeField] private Camera sceneCamera;
        [SerializeField] private float moveSpeed = 6.2f;
        [SerializeField] private float arenaRadius = 12f;

        private bool inputLockedByFight;
        private MoveSlot bufferedSlot;
        private Vector3 bufferedAimDirection;
        private float bufferRemaining;

        private void Awake()
        {
            if (actor == null)
            {
                actor = GetComponent<FighterActor>();
            }

            if (moveRunner == null)
            {
                moveRunner = GetComponent<MoveRunner>();
            }

            if (sceneCamera == null)
            {
                sceneCamera = Camera.main;
            }
        }

        private void Start()
        {
            LockCursor();
        }

        public void Configure(FighterActor newActor, MoveRunner newMoveRunner, ThirdPersonCameraRig newCameraRig, Camera newSceneCamera)
        {
            actor = newActor;
            moveRunner = newMoveRunner;
            cameraRig = newCameraRig;
            sceneCamera = newSceneCamera;
        }

        public void SetInputLocked(bool locked)
        {
            inputLockedByFight = locked;
        }

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }

            if (inputLockedByFight || actor == null || !actor.IsAlive)
            {
                return;
            }

            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame && Cursor.lockState != CursorLockMode.Locked)
            {
                LockCursor();
                return;
            }

            Vector3 moveDirection = ReadMoveDirection();
            if (moveDirection.sqrMagnitude > 0.001f && (moveRunner == null || !moveRunner.BlocksMovement))
            {
                transform.position += moveDirection * moveSpeed * Time.deltaTime;
                transform.forward = moveDirection;
                ClampToArena();
            }

            Vector3 aimDirection = GetAimDirection(moveDirection);
            TryReadMoveInputs(aimDirection);
            TryResolveBufferedInput();
        }

        private Vector3 ReadMoveDirection()
        {
            if (Keyboard.current == null)
            {
                return Vector3.zero;
            }

            Vector2 input = Vector2.zero;
            if (Keyboard.current.wKey.isPressed)
            {
                input.y += 1f;
            }
            if (Keyboard.current.sKey.isPressed)
            {
                input.y -= 1f;
            }
            if (Keyboard.current.dKey.isPressed)
            {
                input.x += 1f;
            }
            if (Keyboard.current.aKey.isPressed)
            {
                input.x -= 1f;
            }

            input = Vector2.ClampMagnitude(input, 1f);
            Vector3 forward = cameraRig != null ? cameraRig.ForwardOnPlane : Vector3.forward;
            Vector3 right = cameraRig != null ? cameraRig.RightOnPlane : Vector3.right;
            Vector3 direction = forward * input.y + right * input.x;
            return direction.sqrMagnitude > 0.001f ? direction.normalized : Vector3.zero;
        }

        private Vector3 GetAimDirection(Vector3 moveDirection)
        {
            if (sceneCamera != null)
            {
                Vector3 aim = sceneCamera.transform.forward;
                aim.y = 0f;
                if (aim.sqrMagnitude > 0.001f)
                {
                    return aim.normalized;
                }
            }

            return moveDirection.sqrMagnitude > 0.001f ? moveDirection : transform.forward;
        }

        private void TryReadMoveInputs(Vector3 aimDirection)
        {
            if (moveRunner == null)
            {
                return;
            }

            if (Mouse.current != null && Cursor.lockState == CursorLockMode.Locked)
            {
                if (Mouse.current.leftButton.wasPressedThisFrame)
                {
                    TryExecuteOrBuffer(MoveSlot.Primary, aimDirection);
                }

                if (Mouse.current.rightButton.wasPressedThisFrame)
                {
                    TryExecuteOrBuffer(MoveSlot.Secondary, aimDirection);
                }
            }

            if (Keyboard.current == null)
            {
                return;
            }

            if (Keyboard.current.spaceKey.wasPressedThisFrame)
            {
                TryExecuteOrBuffer(MoveSlot.Mobility, aimDirection);
            }

            if (Keyboard.current.eKey.wasPressedThisFrame)
            {
                TryExecuteOrBuffer(MoveSlot.Defensive, aimDirection);
            }

            if (Keyboard.current.rKey.wasPressedThisFrame)
            {
                TryExecuteOrBuffer(MoveSlot.Ultimate, aimDirection);
            }
        }

        private void TryExecuteOrBuffer(MoveSlot slot, Vector3 aimDirection)
        {
            if (moveRunner.TryExecute(slot, aimDirection))
            {
                bufferRemaining = 0f;
                return;
            }

            bufferedSlot = slot;
            bufferedAimDirection = aimDirection;
            bufferRemaining = 0.16f;
        }

        private void TryResolveBufferedInput()
        {
            if (moveRunner == null || bufferRemaining <= 0f)
            {
                return;
            }

            bufferRemaining -= Time.deltaTime;
            if (moveRunner.TryExecute(bufferedSlot, bufferedAimDirection))
            {
                bufferRemaining = 0f;
            }
        }

        private void ClampToArena()
        {
            Vector3 position = transform.position;
            Vector2 planar = new Vector2(position.x, position.z);
            if (planar.magnitude <= arenaRadius)
            {
                return;
            }

            planar = planar.normalized * arenaRadius;
            transform.position = new Vector3(planar.x, position.y, planar.y);
        }

        private static void LockCursor()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }
}
