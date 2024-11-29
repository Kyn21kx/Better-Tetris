using Auxiliars;
using System.Security.Cryptography;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;

public class TimedObject : MonoBehaviour
{
    public static T InstantiateTimed<T>(T original, float lifeTime, Transform parent, UnityAction<TimedObject> onUpdate = null)
    where T : Object
    {
        T res = Instantiate(original, parent);
        InitializeTimedObject(res, lifeTime, onUpdate);
        return res;
    }

    public static T InstantiateTimed<T>(T original, float lifeTime, Vector3 position, Quaternion rotation, UnityAction<TimedObject> onUpdate = null)
        where T : Object
    {
        T res = Instantiate(original, position, rotation);
        InitializeTimedObject(res, lifeTime, onUpdate);
        return res;
    }

    public static T InstantiateTimed<T>(T original, float lifeTime, UnityAction<TimedObject> onUpdate = null) where T : Object
    {
        T res = Instantiate(original);
        InitializeTimedObject(res, lifeTime, onUpdate);
        return res;
    }

    private static void InitializeTimedObject<T>(T instance, float lifeTime, UnityAction<TimedObject> onUpdate) where T : Object
    {
        TimedObject timedComponent = instance.AddComponent<TimedObject>();
        timedComponent.m_lifeTime = lifeTime;
        timedComponent.m_lifeTimer = new SpartanTimer(TimeMode.Framed);
        timedComponent.m_onUpdate = onUpdate;
    }

    private float m_lifeTime;

    private SpartanTimer m_lifeTimer;

    private UnityAction<TimedObject> m_onUpdate;
    public float LifeTimeProgressPercentage => this.m_lifeTimer.CurrentTimeSeconds / this.m_lifeTime;

    private void Start()
    {
        this.m_lifeTimer.Start();
    }

    public void Update()
    {
        this.m_onUpdate(this);
        if (this.m_lifeTimer.CurrentTimeSeconds >= this.m_lifeTime)
        {
            Destroy(this.gameObject);
        }
    }

    public void ExecuteOnUpdate(UnityAction<TimedObject> action)
    {
        this.m_onUpdate = action;
    }

}