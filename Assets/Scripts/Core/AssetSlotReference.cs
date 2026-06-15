using UnityEngine;

namespace IdleOnLike.Core
{
    public sealed class AssetSlotReference : MonoBehaviour
    {
        [Header("Inspector Art Slots")]
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Animator animator;

        public SpriteRenderer SpriteRenderer => spriteRenderer;
        public Animator Animator => animator;

        public void Apply(Sprite sprite)
        {
            if (spriteRenderer != null)
            {
                spriteRenderer.sprite = sprite;
            }
        }
    }
}
