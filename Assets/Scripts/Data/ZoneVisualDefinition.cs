using System;
using System.Collections.Generic;
using UnityEngine;

namespace IdleOnLike.Data
{
    public enum ZoneVisualSlotType
    {
        Background,
        LowerGround,
        UpperPlatform,
        Rope,
        Portal,
        QuestNpc,
        Merchant,
        Anvil,
        TreeResource,
        RockResource
    }

    [Serializable]
    public sealed class ZoneVisualSlot
    {
        public ZoneVisualSlotType slotType;
        public Sprite sprite;
        public AnimationClip idleClip;
        public float scale = 1f;
        public Vector2 sizeScale = Vector2.one;
        public float yOffset;
    }

    [CreateAssetMenu(menuName = "IdleOn Like/Data/Zone Visual", fileName = "ZoneVisual_")]
    public sealed class ZoneVisualDefinition : ScriptableObject
    {
        [SerializeField] private ZoneTilemapDefinition tilemapDefinition;
        [SerializeField] private float groundYOffset;
        [SerializeField] private List<ZoneVisualSlot> visuals = new List<ZoneVisualSlot>();

        public ZoneTilemapDefinition TilemapDefinition => tilemapDefinition;
        public float GroundYOffset => groundYOffset;

        public Sprite GetSprite(ZoneVisualSlotType slotType)
        {
            foreach (var visual in visuals)
            {
                if (visual != null && visual.slotType == slotType)
                {
                    return visual.sprite;
                }
            }

            return null;
        }

        public AnimationClip GetIdleClip(ZoneVisualSlotType slotType)
        {
            foreach (var visual in visuals)
            {
                if (visual != null && visual.slotType == slotType)
                {
                    return visual.idleClip;
                }
            }

            return null;
        }

        public float GetScale(ZoneVisualSlotType slotType)
        {
            foreach (var visual in visuals)
            {
                if (visual != null && visual.slotType == slotType)
                {
                    return visual.scale;
                }
            }

            return 1f;
        }

        public Vector2 GetSizeScale(ZoneVisualSlotType slotType)
        {
            foreach (var visual in visuals)
            {
                if (visual != null && visual.slotType == slotType)
                {
                    return visual.sizeScale == Vector2.zero ? Vector2.one : visual.sizeScale;
                }
            }

            return Vector2.one;
        }

        public float GetYOffset(ZoneVisualSlotType slotType)
        {
            foreach (var visual in visuals)
            {
                if (visual != null && visual.slotType == slotType)
                {
                    return visual.yOffset;
                }
            }

            return 0f;
        }
    }
}
