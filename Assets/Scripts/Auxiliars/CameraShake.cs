using UnityEngine;

public class CameraShake : MonoBehaviour
{

    [SerializeField]
    private bool m_stopOnPause;

    // How long the object should shake for.
    public float shakeDuration = 0f;

    // Amplitude of the shake. A larger value shakes the camera harder.
    public float shakeAmount = 0.7f;
    public float decreaseFactor = 1.0f;

    Vector3 originalPos;

    private void OnEnable()
    {
        originalPos = transform.localPosition;
    }

    private void Update()
    {
        if (this.m_stopOnPause && Time.timeScale == 0f)
        {
            // We're in a pause
            return;
        }
        if (shakeDuration > 0)
        {
            transform.localPosition = originalPos + Random.insideUnitSphere * shakeAmount;

            shakeDuration -= Time.deltaTime * decreaseFactor;
        }
        else
        {
            shakeDuration = 0f;
        }
    }

    public void Shake(float duration)
    {
        shakeDuration = duration;
        this.originalPos = transform.localPosition;
    }

}