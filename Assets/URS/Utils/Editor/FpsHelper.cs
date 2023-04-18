using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

public class FpsHelper : MonoBehaviour
{
    private static FpsHelper s_Instance;

    public static FpsHelper instance
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            if (s_Instance == null)
            {
                var go = new GameObject("FpsHelper");
                DontDestroyOnLoad(go);
                s_Instance = go.AddComponent<FpsHelper>();
            }

            return s_Instance;
        }
    }
    private float m_FrameStartTime = 0;
    private bool m_FrameStartTimeFlag = true;
    private float m_MinFps = 30;
    private float m_MaxFrameCostTime = 1;
    private bool m_MinFpsChanged = true;
    private float m_ThresholdRate = 0.8f;
    public float thresholdRate
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => m_ThresholdRate;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set => m_ThresholdRate = Mathf.Clamp01(value);
    }

    public float frameCostTime
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Time.realtimeSinceStartup - m_FrameStartTime;
    }

    public float minFps
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            if (m_MinFpsChanged)
                minFps = Application.targetFrameRate;
            return m_MinFps;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set
        {
            m_MinFps = Mathf.Clamp(value, 1.0f, 60.0f);
            m_MaxFrameCostTime = 1.0f / m_MinFps;
        }
    }

    public bool needWait
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => frameCostTime >= m_MaxFrameCostTime * m_ThresholdRate;
    }

    void Awake()
    {
        m_FrameStartTime = Time.realtimeSinceStartup;
        m_FrameStartTimeFlag = true;
        m_MinFpsChanged = true;
        GameSettings.onFrameRateChanged += OnFrameRateChanged;
    }

    private void OnDestroy()
    {
        GameSettings.onFrameRateChanged -= OnFrameRateChanged;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void OnFrameRateChanged(string value)
    {
        m_MinFpsChanged = true;
    }

    void FixedUpdate()
    {
        if (m_FrameStartTimeFlag)
        {
            m_FrameStartTimeFlag = false;
            m_FrameStartTime = Time.realtimeSinceStartup;
        }
    }

    void Update()
    {
        m_FrameStartTimeFlag = true;
    }
}
