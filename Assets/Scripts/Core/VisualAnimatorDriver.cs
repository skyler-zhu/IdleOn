using IdleOnLike.Data;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace IdleOnLike.Core
{
    public sealed class VisualAnimatorDriver
    {
        private readonly float visualScale;
        private readonly VisualAnimationClips animationClips;

        private PlayableGraph graph;
        private AnimationClipPlayable currentPlayable;
        private AnimationClip currentClip;
        private string currentState;
        private float moveAmount;
        private float oneShotRemainingSeconds;
        private bool oneShotReturnsToLocomotion;
        private bool currentLoops;
        private float facingSign = 1f;

        public VisualAnimatorDriver(GameObject root, Sprite fallbackSprite, VisualAnimationClips animationClips, float visualScale, int sortingOrder, Color32 fallbackColor)
        {
            Root = root;
            this.animationClips = animationClips;
            this.visualScale = Mathf.Max(0.1f, visualScale);

            Renderer = root.GetComponent<SpriteRenderer>();
            if (Renderer == null)
            {
                Renderer = root.AddComponent<SpriteRenderer>();
            }

            Renderer.sprite = fallbackSprite != null ? fallbackSprite : CreateSolidSprite(fallbackColor);
            Renderer.sortingOrder = sortingOrder;
            Renderer.color = Color.white;
            ApplyScale();

            if (animationClips != null && animationClips.HasAnyClip)
            {
                Animator = root.GetComponent<Animator>();
                if (Animator == null)
                {
                    Animator = root.AddComponent<Animator>();
                }

                graph = PlayableGraph.Create($"{root.name} Visual Animation Graph");
                graph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);
                var output = AnimationPlayableOutput.Create(graph, "Animation", Animator);
                var idleClip = GetIdleClip();
                if (idleClip != null)
                {
                    currentPlayable = CreateClipPlayable(idleClip, true);
                    currentClip = idleClip;
                    currentLoops = true;
                    output.SetSourcePlayable(currentPlayable);
                }

                graph.Play();
                currentState = "Idle";
            }
            else
            {
                Animator = root.GetComponent<Animator>();
                if (Animator != null)
                {
                    Object.Destroy(Animator);
                    Animator = null;
                }
            }
        }

        public GameObject Root { get; }
        public Transform Transform => Root.transform;
        public SpriteRenderer Renderer { get; }
        public Animator Animator { get; private set; }
        public bool HasAnimator => graph.IsValid();
        public bool HasAttackAnimation => animationClips != null && animationClips.Attack != null;

        public void SetFacing(float facingSign)
        {
            if (Mathf.Abs(facingSign) > 0.01f)
            {
                this.facingSign = facingSign < 0f ? -1f : 1f;
            }

            ApplyScale();
        }

        private void ApplyScale()
        {
            Transform.localScale = new Vector3(facingSign * visualScale, visualScale, 1f);
        }

        public void SetMoveAmount(float amount)
        {
            moveAmount = Mathf.Abs(amount);
            RefreshLocomotion();
        }

        public void Tick(float deltaTime)
        {
            if (currentLoops && currentPlayable.IsValid() && currentClip != null && currentClip.length > 0f && currentPlayable.GetTime() >= currentClip.length)
            {
                currentPlayable.SetTime(0f);
            }

            if (oneShotRemainingSeconds > 0f)
            {
                oneShotRemainingSeconds = Mathf.Max(0f, oneShotRemainingSeconds - deltaTime);
                if (oneShotRemainingSeconds <= 0f && oneShotReturnsToLocomotion)
                {
                    oneShotReturnsToLocomotion = false;
                    RefreshLocomotion(true);
                }
            }

            ApplyScale();
        }

        public void PlayJump()
        {
            PlayOneShot(animationClips != null ? animationClips.Jump : null, "Jump", true);
        }

        public void PlayAttack()
        {
            PlayOneShot(animationClips != null ? animationClips.Attack : null, "Attack", true);
        }

        public void PlayGather()
        {
            PlayOneShot(animationClips != null ? animationClips.Gather : null, "Gather", true);
        }

        public void PlayHit()
        {
            PlayOneShot(animationClips != null ? animationClips.Hit : null, "Hit", true);
        }

        public void PlayDeath()
        {
            PlayOneShot(animationClips != null ? animationClips.Death : null, "Death", false);
        }

        public void SetColor(Color color)
        {
            if (Renderer != null)
            {
                Renderer.color = color;
            }
        }

        public void Dispose()
        {
            if (graph.IsValid())
            {
                graph.Destroy();
            }
        }

        private void RefreshLocomotion(bool force = false)
        {
            if (!graph.IsValid() || oneShotReturnsToLocomotion)
            {
                return;
            }

            var walkClip = animationClips != null ? animationClips.Walk : null;
            var idleClip = GetIdleClip();
            var nextClip = moveAmount > 0.01f && walkClip != null ? walkClip : idleClip;
            var nextState = nextClip == walkClip ? "Walk" : "Idle";
            PlayLoop(nextClip, nextState, force);
        }

        private AnimationClip GetIdleClip()
        {
            if (animationClips != null && animationClips.Idle != null)
            {
                return animationClips.Idle;
            }

            return animationClips != null ? animationClips.Walk : null;
        }

        private void PlayLoop(AnimationClip clip, string stateName, bool force)
        {
            if (!force && currentState == stateName)
            {
                return;
            }

            ReplacePlayable(clip, true);
            currentState = stateName;
        }

        private void PlayOneShot(AnimationClip clip, string stateName, bool returnToLocomotion)
        {
            if (clip == null || !graph.IsValid())
            {
                return;
            }

            ReplacePlayable(clip, false);
            currentState = stateName;
            oneShotRemainingSeconds = Mathf.Max(0.05f, clip.length);
            oneShotReturnsToLocomotion = returnToLocomotion;
        }

        private AnimationClipPlayable CreateClipPlayable(AnimationClip clip, bool loop)
        {
            if (clip == null)
            {
                return default;
            }

            var playable = AnimationClipPlayable.Create(graph, clip);
            playable.SetApplyFootIK(false);
            playable.SetDuration(loop ? double.PositiveInfinity : clip.length);
            playable.SetTime(0f);
            playable.SetSpeed(1f);
            return playable;
        }

        private void ReplacePlayable(AnimationClip clip, bool loop)
        {
            if (!graph.IsValid() || clip == null)
            {
                return;
            }

            if (currentPlayable.IsValid())
            {
                currentPlayable.Destroy();
            }

            currentPlayable = CreateClipPlayable(clip, loop);
            currentClip = clip;
            currentLoops = loop;
            var output = (AnimationPlayableOutput)graph.GetOutput(0);
            output.SetSourcePlayable(currentPlayable);
        }

        private static Sprite CreateSolidSprite(Color32 color)
        {
            var texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            texture.SetPixel(0, 0, color);
            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
        }
    }
}
