using UnityEngine;

/// <summary>
/// Controls erratic fluorescent light flickering for the Stress Condition
/// to induce visual distraction and increase cognitive workload.
/// </summary>
public class LightFlicker : MonoBehaviour
{
    [Header("Target Component")]
    [SerializeField] private Light targetLight;

    [Header("Intensity Bounds")]
    [SerializeField] private float minIntensity = 0.1f;
    [SerializeField] private float maxIntensity = 1.8f;

    [Header("Flicker Rate")]
    [SerializeField] private float minWaitTime = 0.02f;
    [SerializeField] private float maxWaitTime = 0.12f;

    private float timer;

    void Start()
    {
        // Automatically fetch the Light component if not manually assigned in Inspector
        if (targetLight == null)
        {
            targetLight = GetComponent<Light>();
        }
    }

    void Update()
    {
        if (targetLight == null) return;

        timer -= Time.deltaTime;
        if (timer <= 0f)
        {
            // Randomize brightness to simulate an erratic fluorescent bulb
            targetLight.intensity = Random.Range(minIntensity, maxIntensity);
            
            // Set random delay before the next flicker
            timer = Random.Range(minWaitTime, maxWaitTime);
        }
    }
}