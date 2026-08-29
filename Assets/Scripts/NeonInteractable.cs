using UnityEngine;
using System.Collections;

public class NeonInteractable : MonoBehaviour
{
    [Header("Player Detection")]
    [SerializeField] private string playerTag = "Player";

    [Header("Neon Material")]
    [SerializeField] private Renderer neonRenderer;
    [SerializeField] private Color emissionColor = Color.cyan;

    [Header("Blink Settings")]
    [SerializeField] private float maxEmission = 8f;
    [SerializeField] private float glowInTime = 0.08f;
    [SerializeField] private float holdTime = 0.05f;
    [SerializeField] private float glowOutTime = 0.2f;

    private Material neonMaterial;
    private Coroutine glowCoroutine;

    private void Awake()
    {
        if (neonRenderer == null)
            neonRenderer = GetComponent<Renderer>();

        // Creates an instance so we don't modify the shared material.
        neonMaterial = neonRenderer.material;

        // Make sure emission is enabled.
        neonMaterial.EnableKeyword("_EMISSION");

        // Start with emission off.
        SetEmission(0f);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!collision.gameObject.CompareTag(playerTag))
            return;

        TriggerGlow();
    }

    public void TriggerGlow()
    {
        if (glowCoroutine != null)
            StopCoroutine(glowCoroutine);

        glowCoroutine = StartCoroutine(GlowEffect());
    }

    private IEnumerator GlowEffect()
    {
        // POP IN
        yield return StartCoroutine(
            ChangeEmission(0f, maxEmission, glowInTime)
        );

        // Small hold at maximum brightness
        yield return new WaitForSeconds(holdTime);

        // POP OUT
        yield return StartCoroutine(
            ChangeEmission(maxEmission, 0f, glowOutTime)
        );

        glowCoroutine = null;
    }

    private IEnumerator ChangeEmission(float start, float end, float duration)
    {
        if (duration <= 0f)
        {
            SetEmission(end);
            yield break;
        }

        float time = 0f;

        while (time < duration)
        {
            time += Time.deltaTime;

            float t = time / duration;

            // Smooth but quick transition
            t = Mathf.SmoothStep(0f, 1f, t);

            float intensity = Mathf.Lerp(start, end, t);

            SetEmission(intensity);

            yield return null;
        }

        SetEmission(end);
    }

    private void SetEmission(float intensity)
    {
        neonMaterial.SetColor(
            "_EmissionColor",
            emissionColor * intensity
        );
    }

    private void OnDestroy()
    {
        if (neonMaterial != null)
            Destroy(neonMaterial);
    }
}