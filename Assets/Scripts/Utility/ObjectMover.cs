using System.Collections;
using UnityEngine;

public class ObjectMover : MonoBehaviour
{
    [Header("Auto Move")]
    [SerializeField] private bool moveOnEnable = true;

    [Header("Animation")]
    [Tooltip("Applied relative to the cached position.")]
    [SerializeField] private Vector3 startOffset = new Vector3(0f, -100f, 0f);

    [SerializeField] private bool useCachedPositionAsTarget = true;
    [SerializeField] private Vector3 targetPosition;

    [SerializeField] private float duration = 0.5f;

    private Vector3 cachedPosition;
    private Coroutine moveCoroutine;

    private Vector3 TargetPosition => useCachedPositionAsTarget ? cachedPosition : targetPosition;

    private void OnEnable()
    {
        Invoke(nameof(CachePosition), 0.01f);
    }

    private void CachePosition()
    {
        cachedPosition = transform.localPosition;

        if (moveOnEnable)
        {
            MoveFromOffset(startOffset, TargetPosition, duration);
        }
    }

    /// <summary>
    /// Moves from an offset relative to the target position.
    /// </summary>
    public void MoveFromOffset(Vector3 offset, Vector3 target, float time)
    {
        Vector3 start = target + offset;
        Move(start, target, time);
    }

    /// <summary>
    /// Moves from one position to another.
    /// </summary>
    public void Move(Vector3 from, Vector3 to, float time)
    {
        if (moveCoroutine != null)
            StopCoroutine(moveCoroutine);

        moveCoroutine = StartCoroutine(MoveRoutine(from, to, time));
    }

    /// <summary>
    /// Moves from the current position to the specified target.
    /// </summary>
    public void MoveTo(Vector3 to, float time)
    {
        Move(transform.localPosition, to, time);
    }

    /// <summary>
    /// Moves from the current position back to the cached position.
    /// </summary>
    public void MoveToCached(float time)
    {
        Move(transform.localPosition, cachedPosition, time);
    }

    private IEnumerator MoveRoutine(Vector3 from, Vector3 to, float time)
    {
        transform.localPosition = from;

        if (time <= 0f)
        {
            transform.localPosition = to;
            moveCoroutine = null;
            yield break;
        }

        float elapsed = 0f;

        while (elapsed < time)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / time);

            transform.localPosition = Vector3.LerpUnclamped(from, to, t);

            yield return null;
        }

        transform.localPosition = to;
        moveCoroutine = null;
    }
}