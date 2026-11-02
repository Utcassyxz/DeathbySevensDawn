using UnityEngine;
using DeathBySeventhsDawn.Core;

namespace DeathBySeventhsDawn.Interaction
{
    public sealed class BedroomCurtains : MonoBehaviour
    {
        GameObject curtain;
        Texture2D pixel;
        Sprite square;
        void Awake()
        {
            pixel = new Texture2D(1, 1);
            pixel.SetPixel(0, 0, Color.white);
            pixel.Apply();
            square = Sprite.Create(pixel, new Rect(0, 0, 1, 1), Vector2.one * .5f, 1);
            curtain = new GameObject("Bedroom drawn curtains");
            curtain.transform.SetParent(transform, false);
            // The interaction point sits below the glass; anchor the cloth to the art itself.
            var window = GameObject.Find("Large story window")?.GetComponent<SpriteRenderer>();
            curtain.transform.position = window != null ? window.bounds.center : transform.position + Vector3.up * .56f;
            for (int i = 0; i < 12; i++)
            {
                var g = new GameObject("Curtain pleat");
                g.transform.SetParent(curtain.transform, false);
                g.transform.localPosition = new Vector2(-.80f + i * .145f, 0);
                g.transform.localScale = new Vector3(.15f, .82f, 1);
                var r = g.AddComponent<SpriteRenderer>();
                r.sprite = square;
                r.sortingOrder = 28;
                r.color = i % 3 == 0 ? new Color(.24f, .24f, .30f) : new Color(.40f, .36f, .39f);
            }
        }

        void LateUpdate()
        {
            if (curtain != null)
                curtain.SetActive(GameManager.Instance != null && GameManager.Instance.State.bedroomCurtainsClosed);
        }

        void OnDestroy()
        {
            if (square != null)
                Destroy(square);
            if (pixel != null)
                Destroy(pixel);
        }
    }
}
