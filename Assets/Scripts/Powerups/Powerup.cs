using System;
using MoreMountains.Feedbacks;
using UnityEngine;

namespace Valley.Powerups
{
    [RequireComponent(typeof(Collider))]
    public class Powerup : MonoBehaviour
    {
        public static event Action<PowerupEffect, GameObject> OnPowerupCollected;

        [Header("Effect")]
        [Tooltip("The behavior this pickup grants. Add new powerup types by creating new PowerupEffect assets, not new scripts.")]
        [SerializeField] private PowerupEffect effect;

        [Header("Pickup")]
        [SerializeField] private LayerMask targetMask;
        [SerializeField] private bool destroyOnPickup = true;
        [SerializeField] private MMF_Player feedback;

        private void OnTriggerEnter(Collider other) => TryCollect(other.gameObject);
        private void OnCollisionEnter(Collision collision) => TryCollect(collision.gameObject);

        private void TryCollect(GameObject target)
        {
            if (effect == null) return;
            if (!IsInLayerMask(target.layer, targetMask)) return;

            effect.Apply(target, transform);
            OnPowerupCollected?.Invoke(effect, target);

            if(feedback)
                feedback.PlayFeedbacks();

            if (destroyOnPickup) Destroy(gameObject);
        }

        private static bool IsInLayerMask(int layer, LayerMask mask) => (mask.value & (1 << layer)) != 0;
    }
}