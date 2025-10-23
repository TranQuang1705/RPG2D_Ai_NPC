using System;
using UnityEngine;

public static class GamePause
{
    public static bool IsPaused { get; private set; }
    public static event Action<bool> OnPauseChanged;

    public static void SetPaused(bool paused)
    {
        if (IsPaused == paused) return;
        IsPaused = paused;

        // Dừng tất cả hệ thống dùng thời gian "scaled"
        Time.timeScale = paused ? 0f : 1f;

        OnPauseChanged?.Invoke(paused);
    }
}
