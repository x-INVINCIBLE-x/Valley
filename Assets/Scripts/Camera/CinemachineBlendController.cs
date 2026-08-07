using Unity.Cinemachine;
using UnityEngine;

public class CinemachineBlendController : MonoBehaviour
{
    [SerializeField] private CinemachineBrain brain;

    public void SetBlendDuration(float duration)
    {
        var blend = brain.DefaultBlend;
        blend.Time = duration;
        brain.DefaultBlend = blend;
    }
}