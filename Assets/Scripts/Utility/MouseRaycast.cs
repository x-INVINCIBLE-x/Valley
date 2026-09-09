using UnityEngine;
using UnityEngine.InputSystem;

public class MouseRaycast : MonoBehaviour
{
    [SerializeField] private Camera mainCamera;
    [SerializeField] private LayerMask raycastLayers;

    public GameObject CurrentTarget { get; private set; }

    private void Update()
    {
        if (Mouse.current == null)
            return;

        Vector2 mousePosition = Mouse.current.position.ReadValue();

        Ray ray = mainCamera.ScreenPointToRay(mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, raycastLayers))
        {
            CurrentTarget = hit.collider.gameObject;

            Debug.Log("Mouse is hitting: " + CurrentTarget.name);
        }
        else
        {
            CurrentTarget = null;
        }
    }
}