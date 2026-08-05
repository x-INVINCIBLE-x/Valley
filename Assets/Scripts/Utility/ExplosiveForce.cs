using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class ExplosiveForce : MonoBehaviour
{
    [SerializeField] private bool explodeOnStart = false;

    [SerializeField] private float explosionForce = 10f;
    [SerializeField] private float explosionRadius = 5f;
    [SerializeField] private float upwardsModifier = 5f;
    [SerializeField] private Transform explosionCentre = null;

    private Rigidbody rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void Start()
    {
        if (explodeOnStart)
        {
            Explode();
        }
    }

    public void Explode()
    {
        rb.AddExplosionForce(explosionForce,
                             explosionCentre.position,
                             explosionRadius,
                             upwardsModifier,
                             ForceMode.Impulse);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(explosionCentre.position, explosionRadius);
    }
}
