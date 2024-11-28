using System;
using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;

public class TimeController : MonoBehaviour
{

    private static TimeController s_instance;

    private delegate IEnumerator CallbackCoroutine(float s);

    private int m_activeModifiers;

    private void Awake()
    {
        s_instance = this;
        this.m_activeModifiers = 0;
    }

    public static void StopTimeFor(float seconds, UnityAction callback = null)
    {
        s_instance.m_activeModifiers++;
        Time.timeScale = 0f;
        if (callback == null)
        {
            s_instance.StartCoroutine(s_instance.RestoreTimeScaleAfter(seconds));
            return;
        }
        s_instance.StartCoroutine(s_instance.RestoreTimeScaleAfter(seconds, callback));
    }

    public static void StopTimeForWithDelay(float secondsToStop, float startDelay)
    {
        s_instance.ModifyTimeScaleAfter(0f, startDelay, secondsToStop, s_instance.RestoreTimeScaleAfter);
    }

    public static void SlowTimeFor(float timeScale, float seconds)
    {
        Time.timeScale = timeScale;
        s_instance.StartCoroutine(s_instance.RestoreTimeScaleAfter(seconds));
    }

    private IEnumerator RestoreTimeScaleAfter(float seconds)
    {
        yield return new WaitForSecondsRealtime(seconds);
        this.m_activeModifiers--;
        if (this.m_activeModifiers <= 0)
        {
            Time.timeScale = 1f;
        }
    }

    private IEnumerator RestoreTimeScaleAfter(float seconds, UnityAction callback)
    {
        yield return new WaitForSecondsRealtime(seconds);
        callback();
        this.m_activeModifiers--;
        if (this.m_activeModifiers <= 0)
        {
            Time.timeScale = 1f;
        }
    }

    private IEnumerator ModifyTimeScaleAfter(float timeScale, float secondsBeforeMod, float secondsToHold, CallbackCoroutine restoreTimeCoroutine)
    {
        yield return new WaitForSecondsRealtime(secondsBeforeMod);
        Time.timeScale = timeScale;
        this.StartCoroutine(restoreTimeCoroutine(secondsToHold));
    }

}