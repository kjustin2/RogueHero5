using UnityEngine;

namespace RogueHero5
{
    public sealed class ArcaneArenaPulse : MonoBehaviour
    {
        [SerializeField] private Renderer targetRenderer;
        [SerializeField] private Color baseColor = new Color(0.12f, 0.20f, 0.28f, 1f);
        [SerializeField] private Color pulseColor = new Color(0.22f, 0.62f, 1f, 1f);
        [SerializeField] private float pulseSpeed = 1.5f;

        private void Awake()
        {
            if (targetRenderer == null)
            {
                targetRenderer = GetComponent<Renderer>();
            }
        }

        private void Update()
        {
            if (targetRenderer == null)
            {
                return;
            }

            float t = 0.5f + Mathf.Sin(Time.time * pulseSpeed) * 0.5f;
            targetRenderer.material.color = Color.Lerp(baseColor, pulseColor, t * 0.35f);
        }
    }
}
