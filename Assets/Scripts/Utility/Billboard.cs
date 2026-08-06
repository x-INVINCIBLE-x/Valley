using UnityEngine;

public class Billboard : MonoBehaviour
{
    private Camera mainCamera;

    private void Awake()
    {
        mainCamera = Camera.main;
    }

    private void LateUpdate()
    {
        transform.forward = mainCamera.transform.forward;
    }
}