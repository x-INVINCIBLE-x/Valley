using System.Collections;
using UnityEngine;

public class ObjectScaler : MonoBehaviour
{
    [Header("Auto Scale")]
    [SerializeField] private bool scaleOnEnable = true;

    [Header("Animation")]
    [SerializeField] private Vector3 startScale = Vector3.zero;

    [SerializeField] private bool useCurrentScaleAsTarget = true;
    [SerializeField] private Vector3 targetScale = Vector3.one;

    [SerializeField] private float duration = 0.5f;

    private Vector3 originalScale;
    private Coroutine scaleCoroutine;

    private Vector3 TargetScale => useCurrentScaleAsTarget ? originalScale : targetScale;

    private void Awake()
    {
        originalScale = transform.localScale;
    }

    private void OnEnable()
    {
        if (scaleOnEnable)
        {
            Scale(startScale, TargetScale, duration);
        }
    }

    /// <summary>
    /// Scales from one scale to another over the specified duration.
    /// </summary>
    public void Scale(Vector3 from, Vector3 to, float time)
    {
        if (scaleCoroutine != null)
            StopCoroutine(scaleCoroutine);

        scaleCoroutine = StartCoroutine(ScaleRoutine(from, to, time));
    }

    /// <summary>
    /// Scales from the current scale to the specified target.
    /// </summary>
    public void ScaleTo(Vector3 to, float time)
    {
        Scale(transform.localScale, to, time);
    }

    /// <summary>
    /// Scales from the current scale to the original scale captured in Awake.
    /// </summary>
    public void ScaleToOriginal(float time)
    {
        Scale(transform.localScale, originalScale, time);
    }

    private IEnumerator ScaleRoutine(Vector3 from, Vector3 to, float time)
    {
        transform.localScale = from;

        if (time <= 0f)
        {
            transform.localScale = to;
            scaleCoroutine = null;
            yield break;
        }

        float elapsed = 0f;

        while (elapsed < time)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / time);

            transform.localScale = Vector3.LerpUnclamped(from, to, t);

            yield return null;
        }

        transform.localScale = to;
        scaleCoroutine = null;
    }
}