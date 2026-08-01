using System;

namespace Valley.Core
{
    public static class WorldShiftEvents
    {
        public static event Action<float> OnWorldShiftedX;

        public static void RaiseWorldShiftedX(float amountSubtractedFromWorld)
        {
            OnWorldShiftedX?.Invoke(amountSubtractedFromWorld);
        }
    }
}