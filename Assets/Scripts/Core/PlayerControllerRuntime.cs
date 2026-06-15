using UnityEngine;

namespace IdleOnLike.Core
{
    public sealed class PlayerControllerRuntime
    {
        private readonly Transform transform;
        private readonly VisualAnimatorDriver visual;
        private readonly GameObject attackFlash;
        private readonly float jumpDuration;
        private readonly float jumpHeight;
        private readonly float attackSeconds;
        private readonly float attackCooldownSeconds;

        private float jumpRemainingSeconds;
        private float attackRemainingSeconds;
        private float nextAttackTime;

        public PlayerControllerRuntime(
            Transform transform,
            VisualAnimatorDriver visual,
            GameObject attackFlash,
            float jumpDuration,
            float jumpHeight,
            float attackSeconds,
            float attackCooldownSeconds)
        {
            this.transform = transform;
            this.visual = visual;
            this.attackFlash = attackFlash;
            this.jumpDuration = jumpDuration;
            this.jumpHeight = jumpHeight;
            this.attackSeconds = attackSeconds;
            this.attackCooldownSeconds = Mathf.Max(0f, attackCooldownSeconds);
        }

        public bool IsJumping => jumpRemainingSeconds > 0f;
        public bool IsAttacking => attackRemainingSeconds > 0f;
        public bool CanAttack => Time.time >= nextAttackTime;

        public void MoveHorizontal(float horizontal, float speed, float minX, float maxX, float deltaTime)
        {
            if (IsAttacking)
            {
                StopMoving();
                return;
            }

            if (Mathf.Abs(horizontal) <= 0.01f)
            {
                StopMoving();
                return;
            }

            var position = transform.position;
            position.x = Mathf.Clamp(position.x + horizontal * speed * deltaTime, minX, maxX);
            transform.position = position;
            FaceFromDelta(horizontal);
            visual.SetMoveAmount(horizontal);
        }

        public float MoveTowardX(float targetX, float speed, float deltaTime)
        {
            if (IsAttacking)
            {
                StopMoving();
                return 0f;
            }

            var position = transform.position;
            var previousX = position.x;
            position.x = Mathf.MoveTowards(position.x, targetX, speed * deltaTime);
            transform.position = position;
            var deltaX = position.x - previousX;
            FaceFromDelta(deltaX);
            visual.SetMoveAmount(Mathf.Abs(deltaX) > 0.001f ? 1f : 0f);
            return deltaX;
        }

        public void FaceFromDelta(float deltaX)
        {
            if (Mathf.Abs(deltaX) <= 0.01f)
            {
                return;
            }

            visual.SetFacing(deltaX < 0f ? -1f : 1f);
        }

        public void StopMoving()
        {
            visual.SetMoveAmount(0f);
        }

        public bool TryJump(bool blocked)
        {
            if (blocked || IsJumping || IsAttacking)
            {
                return false;
            }

            jumpRemainingSeconds = jumpDuration;
            visual.PlayJump();
            return true;
        }

        public bool TryAttack(bool blocked)
        {
            if (blocked || !CanAttack)
            {
                return false;
            }

            nextAttackTime = Time.time + attackCooldownSeconds;
            attackRemainingSeconds = attackSeconds;
            visual.PlayAttack();
            if (attackFlash != null && !visual.HasAttackAnimation)
            {
                attackFlash.SetActive(true);
            }

            return true;
        }

        public void ResetActionState()
        {
            jumpRemainingSeconds = 0f;
            attackRemainingSeconds = 0f;
            if (attackFlash != null)
            {
                attackFlash.SetActive(false);
            }

            StopMoving();
        }

        public void TickVisuals(float baseY, float deltaTime)
        {
            if (jumpRemainingSeconds > 0f)
            {
                jumpRemainingSeconds = Mathf.Max(0f, jumpRemainingSeconds - deltaTime);
            }

            if (attackRemainingSeconds > 0f)
            {
                attackRemainingSeconds = Mathf.Max(0f, attackRemainingSeconds - deltaTime);
            }

            visual.Tick(deltaTime);

            if (attackFlash != null && !visual.HasAttackAnimation)
            {
                attackFlash.SetActive(attackRemainingSeconds > 0f);
            }

            var position = transform.position;
            var jumpProgress = jumpRemainingSeconds > 0f ? Mathf.Clamp01(1f - jumpRemainingSeconds / jumpDuration) : 0f;
            position.y = baseY + Mathf.Sin(jumpProgress * Mathf.PI) * jumpHeight;
            transform.position = position;
        }
    }
}
