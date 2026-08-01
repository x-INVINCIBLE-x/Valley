using System;
using System.Collections.Generic;
using UnityEngine;
using Valley.Core;

namespace Valley.Scoring
{
    public class DistanceScoreTracker : MonoBehaviour
    {
        [Header("Tracking")]
        [Tooltip("Usually the player. Score is derived from this transform's +X position.")]
        [SerializeField] private Transform target;
        [Tooltip("Multiplier applied when no contribution sources are active.")]
        [SerializeField] private float baseMultiplier = 1f;

        private readonly Dictionary<object, float> _multiplierContributions = new Dictionary<object, float>();
        
        private float _startX;
        private float _worldShiftOffset;
        private float _cachedMultiplier;

        // <LastMultiplier, NewMultiplier>
        public event Action<float, float> OnMultiplierUpdated;

        public float Score { get; private set; }
        public float Distance { get; private set; }
        public float CurrentMultiplier => _cachedMultiplier;

        private void Awake()
        {
            if (target != null) _startX = target.position.x;
            _cachedMultiplier = baseMultiplier;
        }

        private void OnEnable() => WorldShiftEvents.OnWorldShiftedX += HandleWorldShift;
        private void OnDisable() => WorldShiftEvents.OnWorldShiftedX -= HandleWorldShift;

        private void HandleWorldShift(float amountSubtractedFromWorld) => _worldShiftOffset += amountSubtractedFromWorld;

        private void Update()
        {
            if (target == null) return;

            Distance = (target.position.x + _worldShiftOffset) - _startX;
            Score = Distance * _cachedMultiplier;
        }

        public void SetMultiplierContribution(object source, float amount)
        {
            _multiplierContributions[source] = amount;
            RecalculateMultiplier();
        }

        public void ClearMultiplierContribution(object source)
        {
            _multiplierContributions.Remove(source);
            RecalculateMultiplier();
        }

        private void RecalculateMultiplier()
        {
            float total = baseMultiplier;
            foreach (float contribution in _multiplierContributions.Values)
            {
                total += contribution;
            }

            if (_cachedMultiplier != total)
            {
                float previousMultiplier = _cachedMultiplier;
                _cachedMultiplier = total;
                OnMultiplierUpdated?.Invoke(previousMultiplier, _cachedMultiplier);
            }
        }
    }
}