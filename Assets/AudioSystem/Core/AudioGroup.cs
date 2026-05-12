
namespace AudioSystem
{
    /// <summary>
    /// 音频分组枚举，支持按位掩码扩展
    /// </summary>
    public enum EAudioGroup
    {
        Master      = 0,   // 主音量
        BGM         = 1,   // 背景音乐
        Battle      = 2,   // 战斗音效
        UI          = 3,   // UI音效
        Environment = 4,   // 环境音效
        Dialogue    = 5,   // 对话音效
    }
}