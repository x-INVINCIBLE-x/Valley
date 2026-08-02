using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valley.Powerups;

public class PowerupUI : MonoBehaviour
{
    [SerializeField] private Transform powerupContainer;
    [SerializeField] private RadialBarUI radialBarPrefab;
    [SerializeField] private int poolSize = 6;
    [SerializeField] private float displayDuration = 2f;

    private readonly Queue<RadialBarUI> _pool = new Queue<RadialBarUI>();
    private readonly Dictionary<PowerupEffect, RadialBarUI> _activeBars = new Dictionary<PowerupEffect, RadialBarUI>();
    private readonly Dictionary<PowerupEffect, Coroutine> _flashRoutines = new Dictionary<PowerupEffect, Coroutine>();

    private WaitForSeconds _waitForDisplayDuration;

    private void Awake()
    {
        _waitForDisplayDuration = new WaitForSeconds(displayDuration);

        for (int i = 0; i < poolSize; i++)
        {
            RadialBarUI bar = Instantiate(radialBarPrefab, powerupContainer);
            bar.gameObject.SetActive(false);
            _pool.Enqueue(bar);
        }
    }

    private void OnEnable()
    {
        PowerupReceiver.OnPowerupActivated += HandlePowerupActivated;
        PowerupReceiver.OnPowerupProgress += HandlePowerupProgress;
        PowerupReceiver.OnPowerupExpired += HandlePowerupExpired;
    }

    private void OnDisable()
    {
        PowerupReceiver.OnPowerupActivated -= HandlePowerupActivated;
        PowerupReceiver.OnPowerupProgress -= HandlePowerupProgress;
        PowerupReceiver.OnPowerupExpired -= HandlePowerupExpired;

        StopAllCoroutines();
        _flashRoutines.Clear();

        foreach (var bar in _activeBars.Values)
            ReturnToPool(bar);
        _activeBars.Clear();
    }

    private void HandlePowerupActivated(PowerupEffect effect)
    {
        if (effect.isTimed)
        {
            if (_activeBars.TryGetValue(effect, out RadialBarUI existing))
            {
                existing.UpdateIcon(effect.icon);
                existing.UpdateRadialBar(1f, 1f);
                return;
            }

            RadialBarUI bar = GetFromPool();
            if (bar == null) return;

            bar.UpdateIcon(effect.icon);
            bar.UpdateRadialBar(1f, 1f);
            bar.gameObject.SetActive(true);
            _activeBars[effect] = bar;
        }
        else
        {
            if (_flashRoutines.TryGetValue(effect, out Coroutine running))
                StopCoroutine(running);

            _flashRoutines[effect] = StartCoroutine(FlashRoutine(effect));
        }
    }

    private void HandlePowerupProgress(PowerupEffect effect, float normalizedRemaining)
    {
        if (_activeBars.TryGetValue(effect, out RadialBarUI bar))
        {
            bar.UpdateRadialBar(normalizedRemaining, 1f);
        }
    }

    private void HandlePowerupExpired(PowerupEffect effect)
    {
        if (_activeBars.TryGetValue(effect, out RadialBarUI bar))
        {
            _activeBars.Remove(effect);
            ReturnToPool(bar);
        }
    }

    private IEnumerator FlashRoutine(PowerupEffect effect)
    {
        RadialBarUI bar = GetFromPool();
        if (bar == null) yield break;

        bar.UpdateIcon(effect.icon);
        bar.UpdateRadialBar(1f, 1f);
        bar.gameObject.SetActive(true);

        yield return _waitForDisplayDuration;

        ReturnToPool(bar);
        _flashRoutines.Remove(effect);
    }

    private RadialBarUI GetFromPool()
    {
        if (_pool.Count == 0)
        {
            Debug.LogWarning("PowerupUI: pool exhausted, increase poolSize.", this);
            return null;
        }
        return _pool.Dequeue();
    }

    private void ReturnToPool(RadialBarUI bar)
    {
        bar.gameObject.SetActive(false);
        _pool.Enqueue(bar);
    }
}