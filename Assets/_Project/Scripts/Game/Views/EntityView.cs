using UnityEngine;

namespace Reforged.Game
{
    /// <summary>Interpolated transform for projectiles and interactive debris.</summary>
    public sealed class EntityView : MonoBehaviour
    {
        [SerializeField] float spinDegreesPerSecond = 720f;
        Vector3 previous, current;
        bool initialized;

        public void PushTick(Vector3 position)
        {
            previous = initialized ? current : position;
            current = position;
            initialized = true;
        }

        public void SetScale(Vector3 scale) => transform.localScale = scale;

        public void Render(float alpha)
        {
            transform.position = Vector3.LerpUnclamped(previous, current, alpha);
            if ((current - previous).sqrMagnitude > 0.0001f)
                transform.Rotate(0f, 0f, -spinDegreesPerSecond * Time.deltaTime, Space.Self);
        }
    }
}
