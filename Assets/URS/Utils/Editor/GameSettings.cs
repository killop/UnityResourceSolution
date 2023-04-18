using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NinjaBeats;
using System;
using System.Runtime.CompilerServices;

public static class GameSettings
{
    
    // FrameRate Setting to int
    public static int? FrameRateInt
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => PlayerPrefs.HasKey("GAME_FRAMERATE") ? FrameRate?.ToInt32() : null;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set => FrameRate = value?.ToString();
    }

    public static Action<string> onFrameRateChanged;

    // FrameRate Setting
    public static string FrameRate
    {
        get => PlayerPrefs.GetString("GAME_FRAMERATE");
        set
        {
            PlayerPrefs.SetString("GAME_FRAMERATE", value);
            onFrameRateChanged?.Invoke(value);
        }
    }

 
}