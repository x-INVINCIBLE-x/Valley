using UnityEngine;
using Valley.Combat;

namespace Valley.Combat
{
    public class DamageOnTouch : MonoBehaviour
    {
        [SerializeField] private bool instantKill = true;
        [SerializeField] private float damage = 10f;
        [SerializeField] private LayerMask targetMask;
        [SerializeField] private bool destroySelfOnHit = false;

        private void OnCollisionEnter(Collision collision) => TryDamage(collision.gameObject);
        private void OnTriggerEnter(Collider other) => TryDamage(other.gameObject);

        private void TryDamage(GameObject target)
        {
            if (!IsInLayerMask(target.layer, targetMask)) return;

            var damageable = target.GetComponentInParent<IDamageable>();
            if (damageable == null) return;

            damageable.ApplyDamage(instantKill ? float.MaxValue : damage, gameObject);

            if (destroySelfOnHit) Destroy(gameObject);
        }

        private static bool IsInLayerMask(int layer, LayerMask mask) => (mask.value & (1 << layer)) != 0;
    }
}