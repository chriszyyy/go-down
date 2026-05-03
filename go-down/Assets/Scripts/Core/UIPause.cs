using UnityEngine;

/// <summary>
/// UI 面板暂停游戏的引用计数器。
/// 任何"模态" UI 面板（StartMenu / Shop / Settings 等）在 <c>OnEnable</c> 调用 <see cref="Acquire"/>，
/// <c>OnDisable</c> 调用 <see cref="Release"/>。计数 &gt; 0 时游戏暂停（<c>Time.timeScale = 0</c>）。
///
/// 使用引用计数避免在面板间切换时（StartMenu 隐藏 → Shop 显示）出现一帧 timeScale=1 的间隙
/// 让 tower 物理偷跑或 OnMouseDown 穿透到方块。
///
/// 放在 <c>GoDown.Core</c> 下让 Managers / UI 都能访问，
/// <c>GameStateManager</c> 在 <c>Awake</c> 检查 <see cref="IsPaused"/> 决定是否覆盖 timeScale。
/// </summary>
public static class UIPause
{
    private static int _refCount;

    /// <summary>恢复到正常时间尺度时使用的值（默认 1）。</summary>
    public static float ResumeTimeScale = 1f;

    /// <summary>当前是否处于 UI 暂停状态。</summary>
    public static bool IsPaused => _refCount > 0;

    /// <summary>每次进入 Play 模式时复位静态计数（防止 Disable Domain Reload 下残留）。</summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetOnPlay()
    {
        _refCount = 0;
    }

    /// <summary>
    /// 获取一次暂停。每次都强制写入 <c>Time.timeScale = 0</c>，
    /// 避免被其他 Awake（例如 GameStateManager）覆盖回 1。
    /// </summary>
    public static void Acquire()
    {
        _refCount++;
        Time.timeScale = 0f;
    }

    /// <summary>释放一次暂停。计数回到 0 时把 <c>Time.timeScale</c> 恢复到 <see cref="ResumeTimeScale"/>。</summary>
    public static void Release()
    {
        if (_refCount <= 0) return;
        _refCount--;
        if (_refCount == 0)
        {
            Time.timeScale = ResumeTimeScale;
        }
    }

    /// <summary>清空计数（用于异常恢复 / 场景切换）。</summary>
    public static void ResetCounter()
    {
        _refCount = 0;
    }
}
