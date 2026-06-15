using System;
using UnityEngine;

namespace IdleOnLike.Data
{
    [Serializable]
    public sealed class VisualAnimationClips
    {
        [SerializeField] private AnimationClip idle;
        [SerializeField] private AnimationClip walk;
        [SerializeField] private AnimationClip attack;
        [SerializeField] private AnimationClip gather;
        [SerializeField] private AnimationClip jump;
        [SerializeField] private AnimationClip hit;
        [SerializeField] private AnimationClip death;

        public VisualAnimationClips()
        {
        }

        public VisualAnimationClips(AnimationClip idle)
        {
            this.idle = idle;
        }

        public AnimationClip Idle => idle;
        public AnimationClip Walk => walk;
        public AnimationClip Attack => attack;
        public AnimationClip Gather => gather;
        public AnimationClip Jump => jump;
        public AnimationClip Hit => hit;
        public AnimationClip Death => death;

        public bool HasAnyClip =>
            idle != null ||
            walk != null ||
            attack != null ||
            gather != null ||
            jump != null ||
            hit != null ||
            death != null;
    }
}
