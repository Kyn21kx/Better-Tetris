using UnityEngine;
using UnityEngine.Assertions;

[RequireComponent(typeof(CameraShake))]
public class CameraActions : MonoBehaviour
{
    private static CameraActions s_instance;

    private CameraShake m_shakeController;

    private void Start()
    {
        this.m_shakeController = GetComponent<CameraShake>();
        s_instance = this;
    }

    public static void SendCameraShake(float shakeAmount, float shakeDuration)
    {
        Assert.IsNotNull(s_instance, "Camera actions has not been initialized! Make sure to have added the component to an existing game object!");
        s_instance.m_shakeController.shakeAmount = shakeAmount;
        s_instance.m_shakeController.Shake(shakeDuration);
    }
}