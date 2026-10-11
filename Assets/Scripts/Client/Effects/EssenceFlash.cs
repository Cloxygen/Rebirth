using UnityEngine;

[RequireComponent(typeof(Light))]
public class EssenceFlash : MonoBehaviour
{
    [SerializeField] private float duration = 0.3f;

    private Light flashLight;
    private float startIntensity;
    private float timer;

    private void Awake()
    {
        flashLight = GetComponent<Light>();
        startIntensity = flashLight.intensity;
    }

    private void Update()
    {
        timer += Time.deltaTime;
        flashLight.intensity = startIntensity * (1.0f - Mathf.Clamp01(timer / duration));
    }
}
