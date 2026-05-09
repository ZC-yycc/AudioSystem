# AudioSystem - Unity 音频管理框架

一个为 Unity 项目设计的完整音频管理系统，提供分组音量控制、对象池化 AudioSource、AudioMixer 集成、ID 驱动播放、3D/2D 音效支持、音量持久化等功能。

## 特性

- 🎚️ **音频分组**：Master / BGM / Battle / UI / Environment / Dialogue 六个独立分组
- 🏷️ **ID 驱动播放**：通过 `AudioClipDataSO` 配置音频资源表，代码中使用 `audio_id` 播放，解耦资源引用
- 🎛️ **AudioMixer 集成**：优先使用 AudioMixer 进行音量管理和音效路由，支持添加效果器（EQ、混响等）
- 🔄 **双轨音量**：有 AudioMixer 时通过 exposed parameters 控制；无 Mixer 时自动退回代码方案
- 🏊 **对象池化**：AudioSource 对象池，避免频繁创建销毁，支持容量上限和自动回收
- 🎵 **完整播放 API**：2D/3D 播放、循环、跟随、条目级音调随机范围（Vector2）
- 💾 **音量持久化**：通过 PlayerPrefs 跨场景保存/恢复音量设置
- 🧩 **便捷扩展**：MonoBehaviour 和 AudioManager 扩展方法
- 📦 **Editor 工具**：一键创建完整 AudioSystem（AudioMixer + Settings + Manager + ClipData）

## 目录结构

```
Assets/AudioSystem/
├── Core/                           # 运行时核心代码
│   ├── AudioManager.cs             # 音频管理器（单例）
│   ├── AudioPool.cs                # AudioSource 对象池
│   ├── AudioSettingsSO.cs          # 音频配置 ScriptableObject（音量 + Mixer 路由）
│   ├── AudioClipDataSO.cs          # 音频资源表 ScriptableObject（ID → Clip 映射）
│   ├── AudioGroup.cs               # 音频分组枚举
│   ├── AudioPersistentSettings.cs  # 音量持久化（PlayerPrefs）
│   ├── AudioExtensions.cs          # 便捷扩展方法
│   └── AudioSystem.Core.asmdef     # 核心程序集定义
├── Editor/                         # Editor 工具
│   ├── AudioManagerSetup.cs        # 一键创建工具（菜单项）
│   ├── AudioSettingsSOEditor.cs    # Settings 自定义 Inspector
│   ├── AudioClipDataSOEditor.cs    # ClipData 自定义 Inspector
│   └── AudioSystem.Editor.asmdef   # Editor 程序集定义
├── Resources/                      # 运行时资源
│   ├── AudioSettings.asset         # 音频配置实例
│   ├── AudioClipData.asset         # 音频资源表实例
│   └── AudioMixer.mixer            # AudioMixer 资源
└── README.md
```

## 架构概览

```
┌──────────────────────────────────────────────────────────────────┐
│                        AudioManager                              │
│  (全局单例 · DontDestroyOnLoad · 跨场景持久化)                      │
├──────────────────────────────────────────────────────────────────┤
│  ┌──────────┐  ┌──────────────┐  ┌───────────────┐  ┌─────────┐ │
│  │AudioPool │  │AudioSettingsSO│  │AudioClipDataSO│  │Group    │ │
│  │ (对象池)  │  │ (音量/路由配置) │  │ (ID→Clip映射)  │  │Sources  │ │
│  └──────────┘  └──────────────┘  └───────────────┘  └─────────┘ │
│                          │                                       │
│            ┌─────────────▼─────────────┐                         │
│            │      AudioMixer           │  ← 优先                  │
│            │  (音量 · 路由 · 效果器)      │                         │
│            └───────────────────────────┘                         │
│                    OR (降级)                                       │
│            ┌───────────────────────────┐                         │
│            │  AudioSource.volume = x   │  ← 代码方案              │
│            └───────────────────────────┘                         │
└──────────────────────────────────────────────────────────────────┘
```

## 快速开始

### 1. 一键创建完整系统

在 Unity Editor 菜单栏中选择：
```
GameObject → AudioSystem → 一键创建完整 AudioSystem
```

这将自动完成：
- 在 `Assets/AudioSystem/Resources/` 下创建 `AudioMixer.mixer`
- 创建并配置 `AudioSettings.asset`
- 创建 `AudioClipData.asset`
- 在场景中创建 `[AudioManager]` GameObject（已关联 Settings 和 ClipData）

### 2. 配置音频资源表

在 Project 窗口选中 `Assets/AudioSystem/Resources/AudioClipData.asset`，在 Inspector 中添加条目：

| 字段 | 说明 |
|------|------|
| Audio ID | 唯一标识符，如 `"bgm_main"`, `"sfx_click"` |
| Audio Clip | 音频资源 |
| Group | 所属分组 |
| Volume | 音量倍率（0~2，叠加在分组音量上） |
| Pitch | 音调随机范围 `(x=最小值, y=最大值)`，相等则为固定值 |

### 3. 通过 ID 播放音效

```csharp
using AudioSystem;
using UnityEngine;

public class Example : MonoBehaviour
{
    private void Start()
    {
        // 通过 audio_id 播放（推荐——解耦资源引用）
        AudioManager.Instance.Play("bgm_main");
        AudioManager.Instance.Play("sfx_click");

        // 通过 audio_id 在指定位置播放 3D 音效
        AudioManager.Instance.Play("sfx_explosion", transform.position);
    }
}
```

### 4. 通过 Clip 直接播放

```csharp
using AudioSystem;

public class Example : MonoBehaviour
{
    public AudioClip bgmClip;
    public AudioClip sfxClip;

    private void Start()
    {
        // 播放背景音乐（循环）
        AudioManager.Instance.PlayLoop(bgmClip, AudioGroup.BGM);

        // 播放 UI 音效
        AudioManager.Instance.Play(sfxClip, AudioGroup.UI);

        // 播放 3D 音效，指定音调随机范围
        AudioManager.Instance.PlayAtPosition(explosionClip, AudioGroup.Battle,
            transform.position, new Vector2(0.9f, 1.1f));
    }
}
```

### 5. 使用扩展方法（更简洁）

```csharp
using AudioSystem;

// 通过 ID 播放
this.AudioPlay("sfx_click");

// 通过 Clip 播放
this.AudioPlay(sfxClip, AudioGroup.UI);

// 在指定位置播放 3D 音效
this.AudioPlayAtPosition(explosionClip, AudioGroup.Battle, transform.position);

// 播放跟随物体的音效
this.AudioPlayAttached(engineClip, AudioGroup.Environment, carTransform);
```

### 6. 控制音量

```csharp
// 设置 BGM 音量为 80%
AudioManager.Instance.SetVolume(AudioGroup.BGM, 0.8f);

// 获取当前音量
float volume = AudioManager.Instance.GetVolume(AudioGroup.BGM);

// 重置所有音量
AudioManager.Instance.ResetAllVolumes();
```

### 7. 控制播放

```csharp
// 获取播放句柄
AudioHandle handle = AudioManager.Instance.Play("sfx_click");

// 停止
handle.Stop();

// 暂停/恢复
handle.Pause();
handle.Resume();

// 检查状态
if (handle.is_playing) { ... }
```

## API 参考

### AudioManager

#### ID 驱动播放

| 方法 | 说明 |
|------|------|
| `Play(string audio_id)` | 通过 audio_id 播放 2D 音效 |
| `Play(string audio_id, Vector3 position)` | 通过 audio_id 在指定位置播放 3D 音效 |

#### Clip 直接播放

| 方法 | 说明 |
|------|------|
| `Play(AudioClip clip, AudioGroup group)` | 播放 2D 音效（pitch 默认 (1,1)） |
| `Play(AudioClip clip, AudioGroup group, Vector2 pitch_range)` | 播放 2D 音效，指定音调随机范围 |
| `Play(AudioClip clip, AudioGroup group, float volume_multiplier)` | 播放 2D 音效，指定音量倍率 |
| `Play(AudioClip clip, AudioGroup group, float volume_multiplier, Vector2 pitch_range)` | 播放 2D 音效，指定音量倍率和音调范围 |
| `PlayAtPosition(AudioClip clip, AudioGroup group, Vector3 position, float volume_multiplier = 1f)` | 在指定位置播放 3D 音效 |
| `PlayAtPosition(AudioClip clip, AudioGroup group, Vector3 position, Vector2 pitch_range, float volume_multiplier = 1f)` | 3D 音效 + 音调范围 |
| `PlayLoop(AudioClip clip, AudioGroup group, float volume_multiplier = 1f)` | 播放循环音效（常用于 BGM） |
| `PlayLoop(AudioClip clip, AudioGroup group, Vector2 pitch_range, float volume_multiplier = 1f)` | 循环音效 + 音调范围 |
| `PlayLoopAtPosition(AudioClip clip, AudioGroup group, Vector3 position, float volume_multiplier = 1f)` | 在指定位置播放循环 3D 音效 |
| `PlayLoopAtPosition(AudioClip clip, AudioGroup group, Vector3 position, Vector2 pitch_range, float volume_multiplier = 1f)` | 循环 3D + 音调范围 |
| `PlayAttached(AudioClip clip, AudioGroup group, Transform follow_target, float volume_multiplier = 1f)` | 播放跟随物体的 3D 音效 |

#### 高级 API

| 方法 | 说明 |
|------|------|
| `Play(AudioClip clip, AudioGroup group, Vector3 position, float volume, Vector2 pitch_range, bool loop)` | 完整播放控制（音调范围） |
| `Play(AudioClip clip, AudioGroup group, Vector3 position, float volume, float pitch, bool loop)` | 完整播放控制（固定音调） |

#### 音量控制

| 方法 | 说明 |
|------|------|
| `SetVolume(AudioGroup group, float volume)` | 设置分组音量（0~1） |
| `GetVolume(AudioGroup group)` | 获取分组最终音量 |
| `GetGroupVolume(AudioGroup group)` | 获取分组原始音量（不含主音量） |
| `ResetAllVolumes()` | 重置所有分组音量为 1 |
| `ApplyAllVolumes()` | 手动刷新所有音源音量 |

#### 停止控制

| 方法 | 说明 |
|------|------|
| `StopGroup(AudioGroup group)` | 停止指定分组的所有音效 |
| `StopAll()` | 停止所有音效 |

#### 属性

| 属性 | 类型 | 说明 |
|------|------|------|
| `Settings` | `AudioSettingsSO` | 获取音频配置引用 |
| `ClipData` | `AudioClipDataSO` | 获取音频资源表引用 |
| `Instance` | `AudioManager` | 全局单例访问 |

### AudioClipDataSO

ScriptableObject，存储 audio_id 到 AudioClip 的映射关系。通过 Resources 自动加载。

| 方法/属性 | 说明 |
|-----------|------|
| `Entries` | 获取所有音频条目（只读） |
| `TryGetEntry(string audio_id, out AudioClipEntry entry)` | 通过 ID 查找条目 |
| `SetEntry(AudioClipEntry entry)` | 添加或更新条目 |
| `RemoveEntry(string audio_id)` | 删除条目 |
| `BuildLookup()` | 手动重建 ID 查找表 |

**AudioClipEntry 字段：**

| 字段 | 类型 | 说明 |
|------|------|------|
| `audio_id` | `string` | 唯一标识符 |
| `clip` | `AudioClip` | 音频资源 |
| `group` | `AudioGroup` | 所属分组 |
| `volume` | `float` | 音量倍率（0~2） |
| `pitch` | `Vector2` | 音调随机范围（x=最小, y=最大），相等则为固定值 |
| `use_fixed_pitch` | `bool` | 是否使用固定音调 |
| `GetRandomPitch()` | `float` | 获取随机/固定音调值 |

### AudioGroup 枚举

| 值 | 说明 |
|----|------|
| `Master` | 主音量（影响所有分组） |
| `BGM` | 背景音乐 |
| `Battle` | 战斗音效 |
| `UI` | 界面音效 |
| `Environment` | 环境音效 |
| `Dialogue` | 对话/语音 |

### AudioHandle 结构体

| 属性/方法 | 说明 |
|-----------|------|
| `is_valid` | 是否有效 |
| `is_playing` | 是否正在播放 |
| `source` | 获取原始 AudioSource |
| `group` | 获取所属分组 |
| `Stop()` | 停止播放（回收到池） |
| `Pause()` | 暂停 |
| `Resume()` | 恢复 |

### 扩展方法（可在任意 MonoBehaviour 中直接调用）

| 方法 | 说明 |
|------|------|
| `this.AudioPlay(string audio_id)` | 通过 ID 播放 2D 音效 |
| `this.AudioPlay(string audio_id, Vector3 position)` | 通过 ID 播放 3D 音效 |
| `this.AudioPlay(AudioClip clip, AudioGroup group)` | 播放 2D 音效 |
| `this.AudioPlay(AudioClip clip, AudioGroup group, float volume)` | 播放 2D 音效，指定音量 |
| `this.AudioPlayAtPosition(AudioClip clip, AudioGroup group, Vector3 pos, float volume = 1f)` | 3D 位置播放 |
| `this.AudioPlayLoop(AudioClip clip, AudioGroup group, float volume = 1f)` | 循环播放 |
| `this.AudioPlayAttached(AudioClip clip, AudioGroup group, Transform target, float volume = 1f)` | 跟随物体播放 |

### AudioPersistentSettings（静态方法）

| 方法 | 说明 |
|------|------|
| `Save(AudioSettingsSO settings)` | 保存当前音量到 PlayerPrefs |
| `Load(AudioSettingsSO settings)` | 从 PlayerPrefs 恢复音量 |
| `HasSavedData()` | 是否有已保存的数据 |
| `Clear()` | 清除所有持久化数据 |

## AudioMixer 集成详解

### 双轨架构

系统自动选择音量管理方案：

```
if (AudioSettingsSO.Mixer != null)
    → 使用 AudioMixer exposed parameters（推荐）
    → 在 Audio Mixer 窗口中可添加效果器
else
    → 使用 AudioSource.volume 逐源控制（降级方案）
```

### 程序化创建 AudioMixer

通过菜单 `GameObject → AudioSystem → 创建 AudioMixer` 或 `一键创建完整 AudioSystem` 可以程序化创建 AudioMixer 资源，自动包含：

- **6 个混音组**：Master / BGM / Battle / UI / Environment / Dialogue
- **6 个 exposed parameters**：MasterVolume / BgmVolume / BattleVolume / UIVolume / EnvironmentVolume / DialogueVolume
- 所有子组统一路由到 Master 组

创建后在 Window → Audio → Audio Mixer 中可查看和编辑。

### 在 Mixer 中添加效果器

1. 打开 `Window → Audio → Audio Mixer`
2. 选择一个分组（如 BGM）
3. 点击 "Add Effect" 添加效果器（EQ、Reverb、Compressor 等）
4. 所有通过该分组播放的音效将自动经过效果器处理

### Exposed Parameter 命名约定

| Parameter | 分组 | 默认值 |
|-----------|------|--------|
| `MasterVolume` | Master | 0 dB |
| `BgmVolume` | BGM | 0 dB |
| `BattleVolume` | Battle | 0 dB |
| `UIVolume` | UI | 0 dB |
| `EnvironmentVolume` | Environment | 0 dB |
| `DialogueVolume` | Dialogue | 0 dB |

> 可通过脚本直接控制：`mixer.SetFloat("BgmVolume", -6f);`（-6 dB = 50% 音量）

## Editor 菜单

| 菜单路径 | 功能 |
|----------|------|
| `GameObject → AudioSystem → 一键创建完整 AudioSystem` | 创建所有资源（Mixer + Settings + ClipData + Manager） |
| `GameObject → AudioSystem → 创建 AudioManager` | 创建场景中的 Manager（自动关联已有资源） |
| `GameObject → AudioSystem → 创建 AudioSettingsSO` | 仅创建 Settings 资源 |
| `GameObject → AudioSystem → 创建 AudioMixer` | 仅创建 Mixer 资源 |
| `GameObject → AudioSystem → 场景设置：添加 AudioManager 到首场景` | 快速配置首场景 |

## 音量持久化

```csharp
// 保存（例如在设置面板关闭时）
AudioPersistentSettings.Save(AudioManager.Instance.Settings);

// 加载（例如在游戏启动时）
if (AudioPersistentSettings.HasSavedData())
{
    AudioPersistentSettings.Load(AudioManager.Instance.Settings);
    AudioManager.Instance.ApplyAllVolumes();
}
```

## 最佳实践

1. **在首场景放置 AudioManager**：由于标记了 `DontDestroyOnLoad`，它会在整个游戏生命周期中存在。
2. **使用 AudioClipDataSO 管理资源**：通过 `audio_id` 播放解耦硬编码的 AudioClip 引用，方便策划配置和热更新。
3. **优先使用 AudioMixer**：通过 `一键创建完整 AudioSystem` 自动创建，方便后续添加效果器。
4. **使用 AudioHandle 管理播放**：通过 `handle.Stop()` / `handle.Pause()` 精确控制音效。
5. **音量持久化**：在设置面板关闭时调用 `AudioPersistentSettings.Save()`。
6. **对象池容量**：默认初始 10 个 AudioSource，最大 30 个，可在 AudioManager 的 Inspector 中调整。

## 依赖

- Unity 2022.3+
- `UnityEngine.AudioModule`

## 许可

MIT License