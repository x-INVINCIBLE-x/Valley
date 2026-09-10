using System.Collections;
using UnityEngine;

public class MaterialColorBoost : MonoBehaviour
{
    [SerializeField] private Renderer targetRenderer;
    [SerializeField] private int materialIndex = 0;

    [Header("Boost")]
    [SerializeField] private Color boostColor = Color.white;
    [SerializeField] private float emissionMultiplier = 3f;
    [SerializeField] private float duration = 1f;

    private Material targetMaterial;

    private Color originalColor;
    private Color originalEmission;

    private Coroutine boostCoroutine;

    private void Awake()
    {
        // Creates an instance of the renderer's material.
        targetMaterial = targetRenderer.materials[materialIndex];

        originalColor = targetMaterial.GetColor("_Color");
        originalEmission = targetMaterial.GetColor("_EmissionColor");
    }

    public void TriggerBoost()
    {
        if (boostCoroutine != null)
            StopCoroutine(boostCoroutine);

        boostCoroutine = StartCoroutine(BoostRoutine());
    }

    private IEnumerator BoostRoutine()
    {
        // Change ONLY the selected material.
        targetMaterial.SetColor("_Color", boostColor);

        targetMaterial.SetColor(
            "_EmissionColor",
            originalEmission * emissionMultiplier
        );

        yield return new WaitForSeconds(duration);

        // Restore exact original values.
        targetMaterial.SetColor("_Color", originalColor);
        targetMaterial.SetColor("_EmissionColor", originalEmission);

        boostCoroutine = null;
    }

    private void OnDisable()
    {
        if (targetMaterial != null)
        {
            targetMaterial.SetColor("_Color", originalColor);
            targetMaterial.SetColor("_EmissionColor", originalEmission);
        }
    }
}