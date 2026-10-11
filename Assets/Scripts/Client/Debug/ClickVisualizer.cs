using UnityEngine;
using UnityEngine.InputSystem;

public class ClickVisualizer : MonoBehaviour
{
    public GameObject clickEffectPrefab;
    public LayerMask groundLayer;

    void Update()
    {
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            SpawnClickEffect();
        }
    }

    private void SpawnClickEffect()
    {
        if (Mouse.current == null) return;

        Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
        if (Physics.Raycast(ray, out RaycastHit hit, 100f, groundLayer))
        {
            if (clickEffectPrefab != null)
            {
                Instantiate(clickEffectPrefab, hit.point + Vector3.up * 0.1f, Quaternion.identity);
            }
        }
    }
}
