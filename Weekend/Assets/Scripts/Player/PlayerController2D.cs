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
        private Sprite apartmentVisual;
        private readonly ContactPoint2D[] contacts = new ContactPoint2D[12];
        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            InstallApartmentVisual();
        }
        void InstallApartmentVisual()
        {
            var renderer=GetComponent<SpriteRenderer>();
            // Replace only the authored prototype square. City/neighbor sprites and cutscene poses have their own owners.
            if(renderer==null||renderer.sprite==null||renderer.sprite.texture.name!="Square")return;
            var art=gameObject.AddComponent<DeathBySeventhsDawn.Interaction.PocketSceneArt>();
            var source=art.Person();float scale=Mathf.Max(.01f,Mathf.Abs(transform.lossyScale.y));
            float localHeight=1.25f/scale;var box=GetComponent<BoxCollider2D>();
            float bottom=box!=null?box.offset.y-box.size.y*.5f:-.5f;
            var frame=new Rect(source.rect.x+1,source.rect.y,source.rect.width-2,source.rect.height);
            apartmentVisual=Sprite.Create(source.texture,frame,new Vector2(.5f,Mathf.Clamp01(-bottom/localHeight)),source.rect.height/localHeight,0,SpriteMeshType.FullRect);
            apartmentVisual.name="Apartment Hugo";renderer.sprite=apartmentVisual;renderer.color=Color.white;renderer.sortingOrder=Mathf.Max(renderer.sortingOrder,20);
        }
        private void OnDestroy(){if(apartmentVisual!=null)Destroy(apartmentVisual);}
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
            if ((Core.GameManager.Instance != null && Core.GameManager.Instance.HasEnded) || (DeathBySeventhsDawn.Interaction.StoryJournal.WorldBlocked || DeathBySeventhsDawn.Progression.CognitiveRupture.IsActive))
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
