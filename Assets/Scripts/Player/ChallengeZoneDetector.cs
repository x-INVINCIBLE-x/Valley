using System;
using System.Collections.Generic;
using UnityEngine;

public class ChallengeZoneDetector : MonoBehaviour
{
    [SerializeField] private string challengeTagName = "ChallengeZone";

    public event Action<bool> ChallengeZoneState;

    private TagHandle challengeTagHandle;

    private readonly HashSet<GameObject> activeChallengeZones = new();

    private void Start()
    {
        challengeTagHandle = TagHandle.GetExistingTag(challengeTagName);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(challengeTagHandle))
            return;

        if (activeChallengeZones.Add(other.gameObject))
        { 
            ChallengeZoneState?.Invoke(true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag(challengeTagHandle))
            return;

        ChallengeZoneState?.Invoke(false);
    }
}