using System.Collections;
using UnityEngine;

public class MaterialAlphaTrigger : MonoBehaviour
{
    [SerializeField] private Renderer targetRenderer;

    [Header("Alpha")]
    [SerializeField, Range(0f, 1f)] private float targetAlpha = 0.25f;
    [SerializeField] private float fadeDuration = 0.25f;

    private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");

    private Material material;
    private Color originalColor;
    private Coroutine alphaRoutine;

    private void Awake()
    {
        if (targetRenderer == null)
            targetRenderer = GetComponent<Renderer>();

        material = targetRenderer.material;
        originalColor = material.GetColor(BaseColor);
    }

    public void StartTrigger()
    {
        if (alphaRoutine != null)
            StopCoroutine(alphaRoutine);

        alphaRoutine = StartCoroutine(AnimateTrigger());
    }

    private IEnumerator AnimateTrigger()
    {
        yield return AnimateAlpha(targetAlpha);

        yield return AnimateAlpha(originalColor.a);

        alphaRoutine = null;
    }

    public void StartFadeOut()
    {
        StartAlphaAnimation(targetAlpha);
    }

    public void StartFadeIn()
    {
        StartAlphaAnimation(originalColor.a);
    }

    private void StartAlphaAnimation(float target)
    {
        if (alphaRoutine != null)
            StopCoroutine(alphaRoutine);

        alphaRoutine = StartCoroutine(AnimateAlpha(target));
    }

    private IEnumerator AnimateAlpha(float target)
    {
        float startAlpha = material.GetColor(BaseColor).a;
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;

            float t = Mathf.Clamp01(elapsed / fadeDuration);
            t = Mathf.SmoothStep(0f, 1f, t);

            SetAlpha(Mathf.Lerp(startAlpha, target, t));

            yield return null;
        }

        SetAlpha(target);
        alphaRoutine = null;
    }

    private void SetAlpha(float alpha)
    {
        Color color = material.GetColor(BaseColor);
        color.a = alpha;
        material.SetColor(BaseColor, color);
    }

    private void OnDisable()
    {
        if (alphaRoutine != null)
        {
            StopCoroutine(alphaRoutine);
            alphaRoutine = null;
        }

        if (material != null)
            material.SetColor(BaseColor, originalColor);
    }
}