// 职责：二维移动和动画输入；剧情弹窗与终局时停止移动。
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace DeathBySeventhsDawn.Player
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerController2D : MonoBehaviour
    {
        [SerializeField] private float moveSpeed = 5.5f;
        private Rigidbody2D rb;
        private Vector2 moveInput;
        private readonly ContactPoint2D[] contacts = new ContactPoint2D[12];
        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
        }
        private void Update()
        {
#if ENABLE_INPUT_SYSTEM
            var keyboard = Keyboard.current;
            moveInput = keyboard == null ? Vector2.zero : new Vector2(
                (keyboard.dKey.isPressed ? 1 : 0) -
                (keyboard.aKey.isPressed ? 1 : 0),
                (keyboard.wKey.isPressed ? 1 : 0) -
                (keyboard.sKey.isPressed ? 1 : 0));
#elif ENABLE_LEGACY_INPUT_MANAGER
            moveInput = new Vector2((Input.GetKey(KeyCode.D)?1:0)-(Input.GetKey(KeyCode.A)?1:0),(Input.GetKey(KeyCode.W)?1:0)-(Input.GetKey(KeyCode.S)?1:0));
#else
            moveInput = Vector2.zero;
#endif
            moveInput = Vector2.ClampMagnitude(moveInput, 1f);
        }
        private void FixedUpdate()
        {
            if ((Core.GameManager.Instance != null && Core.GameManager.Instance.HasEnded) || DeathBySeventhsDawn.Interaction.StoryJournal.WorldBlocked)
            {
                rb.linearVelocity = Vector2.zero;
                return;
            }
            Vector2 velocity = moveInput * moveSpeed;
            int count = rb.GetContacts(contacts);
            for (int i = 0; i < count; i++)
            {
                float intoSurface = Vector2.Dot(velocity, contacts[i].normal);
                if (intoSurface < 0f)
                    velocity -= contacts[i].normal * intoSurface;
            }
            // Keep diagonal input responsive while sliding along a solid edge.
            if (velocity.sqrMagnitude > .001f)
                velocity = velocity.normalized * moveInput.magnitude * moveSpeed;
            rb.linearVelocity = velocity;
        }
        private void OnDisable()
        {
            moveInput = Vector2.zero;
            if (rb != null)
                rb.linearVelocity = Vector2.zero;
        }
    }
}
