using UnityEngine;

namespace Valley.Level.Environment
{
    public class SkyLanternMover : MonoBehaviour
    {
        [Header("Vertical Movement")]
        [SerializeField] private float minRiseSpeed = 0.8f;
        [SerializeField] private float maxRiseSpeed = 1.5f;

        [Header("Horizontal Drift")]
        [SerializeField] private float minDriftSpeed = 0.1f;
        [SerializeField] private float maxDriftSpeed = 0.5f;

        [Header("Sway")]
        [SerializeField] private float minSwayAmount = 0.2f;
        [SerializeField] private float maxSwayAmount = 0.8f;

        [SerializeField] private float minSwayFrequency = 0.2f;
        [SerializeField] private float maxSwayFrequency = 0.6f;

        [Header("Rotation")]
        [SerializeField] private float maxTilt = 8f;
        [SerializeField] private float rotationSpeed = 2f;

        private float lifetime;
        private float age;

        private float riseSpeed;
        private float driftSpeed;

        private float swayAmount;
        private float swayFrequency;

        private float noiseOffsetX;
        private float noiseOffsetZ;

        private Vector3 driftDirection;
        private Vector3 initialEulerAngles;

        private System.Action<SkyLanternMover> releaseCallback;

        public void Initialize(
            float lifetime,
            System.Action<SkyLanternMover> releaseCallback)
        {
            this.lifetime = lifetime;
            this.releaseCallback = releaseCallback;

            age = 0f;

            riseSpeed = Random.Range(minRiseSpeed, maxRiseSpeed);
            driftSpeed = Random.Range(minDriftSpeed, maxDriftSpeed);

            swayAmount = Random.Range(minSwayAmount, maxSwayAmount);
            swayFrequency = Random.Range(
                minSwayFrequency,
                maxSwayFrequency);

            noiseOffsetX = Random.Range(0f, 1000f);
            noiseOffsetZ = Random.Range(0f, 1000f);

            // Random horizontal direction.
            Vector2 randomDirection = Random.insideUnitCircle.normalized;

            driftDirection = new Vector3(
                randomDirection.x,
                0f,
                randomDirection.y);

            if (driftDirection.sqrMagnitude < 0.01f)
                driftDirection = Vector3.forward;

            initialEulerAngles = transform.eulerAngles;
        }

        private void Update()
        {
            age += Time.deltaTime;

            Move();

            if (age >= lifetime)
                Release();
        }

        private void Move()
        {
            float time = Time.time;

            // Smooth Perlin-noise based movement.
            float noiseX =
                Mathf.PerlinNoise(
                    noiseOffsetX,
                    time * swayFrequency);

            float noiseZ =
                Mathf.PerlinNoise(
                    noiseOffsetZ,
                    time * swayFrequency);

            noiseX = noiseX * 2f - 1f;
            noiseZ = noiseZ * 2f - 1f;

            Vector3 horizontalDrift =
                driftDirection * driftSpeed;

            Vector3 sway = new Vector3(
                noiseX * swayAmount,
                0f,
                noiseZ * swayAmount);

            Vector3 movement =
                Vector3.up * riseSpeed +
                horizontalDrift +
                sway;

            transform.position += movement * Time.deltaTime;

            ApplyRotation(noiseX, noiseZ);
        }

        private void ApplyRotation(float noiseX, float noiseZ)
        {
            float targetTiltX = -noiseZ * maxTilt;
            float targetTiltZ = noiseX * maxTilt;

            Quaternion targetRotation =
                Quaternion.Euler(
                    initialEulerAngles.x + targetTiltX,
                    initialEulerAngles.y,
                    initialEulerAngles.z + targetTiltZ);

            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                rotationSpeed * Time.deltaTime);
        }

        private void Release()
        {
            releaseCallback?.Invoke(this);
        }
    }
}