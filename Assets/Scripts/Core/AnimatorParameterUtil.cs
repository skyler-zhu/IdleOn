using UnityEngine;

namespace IdleOnLike.Core
{
    public static class AnimatorParameterUtil
    {
        public static bool HasController(Animator animator)
        {
            return animator != null && animator.runtimeAnimatorController != null;
        }

        public static void SetFloat(Animator animator, string parameterName, float value)
        {
            if (HasParameter(animator, parameterName, AnimatorControllerParameterType.Float))
            {
                animator.SetFloat(parameterName, value);
            }
        }

        public static void SetTrigger(Animator animator, string parameterName)
        {
            if (HasParameter(animator, parameterName, AnimatorControllerParameterType.Trigger))
            {
                animator.SetTrigger(parameterName);
            }
        }

        private static bool HasParameter(Animator animator, string parameterName, AnimatorControllerParameterType type)
        {
            if (!HasController(animator) || string.IsNullOrEmpty(parameterName))
            {
                return false;
            }

            foreach (var parameter in animator.parameters)
            {
                if (parameter.type == type && parameter.name == parameterName)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
