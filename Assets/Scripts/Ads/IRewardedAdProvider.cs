using System;

namespace Valley.Revive
{
    public interface IRewardedAdProvider
    {
        void ShowRewardedAd(Action onRewardGranted, Action onAdUnavailableOrDeclined);
    }
}