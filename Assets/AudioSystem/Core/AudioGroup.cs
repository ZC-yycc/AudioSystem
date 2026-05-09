namespace AudioSystem
{
    /// <summary>
    /// 音频分组枚举，支持按位掩码扩展
    /// </summary>
    [System.Flags]
    public enum AudioGroup
    {
        Master      = 1 << 0,   // 主音量
        BGM         = 1 << 1,   // 背景音乐
        Battle      = 1 << 2,   // 战斗音效
        UI          = 1 << 3,   // UI音效
        Environment = 1 << 4,   // 环境音效
        Dialogue    = 1 << 5,   // 对话音效
        // 预留扩展：All = ~0
    }
}