using Valley.Core;

namespace Valley.Level.Generation
{
    public static class LaunchReachability
    {
        public struct Envelope
        {
            /// <summary>Highest point reachable above the launch origin.</summary>
            public float maxUpwardHeight;
            /// <summary>Forward distance covered by the time that peak height is reached.</summary>
            public float maxForwardAtMaxHeight;
            /// <summary>Forward distance covered by the time the player returns to launch-origin height with no launches left.</summary>
            public float maxForwardDistance;
        }

        public static Envelope Calculate(float forwardSpeed, LaunchProfile launchProfile, float gravity, int maxLaunches, float timeStep = 0.02f, float maxSimTime = 8f)
        {
            float launchForce = launchProfile.EvaluateForce(1f);
            float retention = launchProfile.previousVelocityRetention;

            float velocityY = launchForce;
            float y = 0f, x = 0f, t = 0f;
            float maxHeight = 0f, forwardAtMaxHeight = 0f;
            int launchesUsed = 1;
            bool usedFinalLaunch = maxLaunches <= 1;

            while (t < maxSimTime)
            {
                velocityY -= gravity * timeStep;
                y += velocityY * timeStep;
                x += forwardSpeed * timeStep;
                t += timeStep;

                if (y > maxHeight)
                {
                    maxHeight = y;
                    forwardAtMaxHeight = x;
                }

                if (!usedFinalLaunch && launchesUsed < maxLaunches && velocityY <= 0f)
                {
                    velocityY = velocityY * retention + launchForce;
                    launchesUsed++;
                    usedFinalLaunch = launchesUsed >= maxLaunches;
                }
                else if (usedFinalLaunch && y <= 0f && t > timeStep * 2f)
                {
                    break; // back down to launch-origin height with nothing left in reserve
                }
            }

            return new Envelope
            {
                maxUpwardHeight = maxHeight,
                maxForwardAtMaxHeight = forwardAtMaxHeight,
                maxForwardDistance = x
            };
        }
    }
}