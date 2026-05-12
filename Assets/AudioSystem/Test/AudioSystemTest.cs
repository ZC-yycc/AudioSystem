using System.Collections;
using UnityEngine;

namespace AudioSystem
{
    /// <summary>
    /// AudioSystem 简单测试案例
    /// 将此脚本挂载到场景中的任意 GameObject 上，按键盘按键即可测试各项功能
    /// 
    /// 前提条件：
    /// 1. AudioManager 会自动创建（无需手动放置）
    /// 2. 将 AudioClipDataSO 放入 Resources 目录（已存在：Resources/AudioClipData.asset）
    /// 3. 在 AudioClipDataSO 中配置至少一条音频条目（audio_id、clip、group）
    /// </summary>
    public class AudioSystemTest : MonoBehaviour
    {
        [Header("测试音频（直接引用，无需 AudioClipDataSO）")]
        [SerializeField] private AudioClip test_clip_;
        [SerializeField] private AudioClip test_bgm_clip_;

        [Header("3D 测试目标（可选）")]
        [SerializeField] private Transform test_3d_target_;

        [Header("AudioClipDataSO 中的音频 ID")]
        [SerializeField] private string test_audio_id_ = "test_sfx";

        private AudioHandle? current_handle_;
        private AudioHandle? bgm_handle_;
        private AudioHandle? attached_handle_;

        private void Start()
        {
            // AudioManager 单例会在首次访问时自动创建
            Debug.Log("===== AudioSystem Test Ready =====");
            Debug.Log("按键盘按键测试：");
            Debug.Log("  1 - 播放 2D 音效（直接 Clip 引用）");
            Debug.Log("  2 - 播放 2D 音效（通过 audio_id）");
            Debug.Log("  3 - 播放 3D 音效（在随机位置）");
            Debug.Log("  4 - 播放/切换 BGM 循环");
            Debug.Log("  5 - 播放跟随目标的 3D 音效");
            Debug.Log("  P - 暂停当前音效");
            Debug.Log("  R - 恢复当前音效");
            Debug.Log("  S - 停止当前音效");
            Debug.Log("  A - 停止所有音效");
            Debug.Log("  Q - 增大 BGM 音量");
            Debug.Log("  W - 减小 BGM 音量");
            Debug.Log("  E - 测试音调随机变化（Battle 组）");
            Debug.Log("  V - 打印当前状态");
            Debug.Log("  T - 逐步测试（自动运行全部测试）");
            Debug.Log("====================================");
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Alpha1))
                Test_Play2D_DirectClip();

            if (Input.GetKeyDown(KeyCode.Alpha2))
                Test_Play2D_ByAudioId();

            if (Input.GetKeyDown(KeyCode.Alpha3))
                Test_Play3D_AtPosition();

            if (Input.GetKeyDown(KeyCode.Alpha4))
                Test_PlayBGM_Loop();

            if (Input.GetKeyDown(KeyCode.Alpha5))
                Test_Play3D_Attached();

            if (Input.GetKeyDown(KeyCode.P))
                Test_Pause();

            if (Input.GetKeyDown(KeyCode.R))
                Test_Resume();

            if (Input.GetKeyDown(KeyCode.S))
                Test_Stop();

            if (Input.GetKeyDown(KeyCode.A))
                Test_StopAll();

            if (Input.GetKeyDown(KeyCode.Q))
                Test_IncreaseVolume();

            if (Input.GetKeyDown(KeyCode.W))
                Test_DecreaseVolume();

            if (Input.GetKeyDown(KeyCode.E))
                Test_PitchRandomization();

            if (Input.GetKeyDown(KeyCode.V))
                Test_PrintStatus();

            if (Input.GetKeyDown(KeyCode.T))
                StartCoroutine(RunAllTestsCoroutine());
        }

        #region 测试方法

        /// <summary>
        /// 测试 1：直接引用 AudioClip 播放 2D 音效
        /// </summary>
        private void Test_Play2D_DirectClip()
        {
            if (test_clip_ == null)
            {
                Debug.LogWarning("[Test] test_clip_ 未赋值！请在 Inspector 中拖入一个 AudioClip。");
                return;
            }

            AudioHandle handle = AudioManager.Instance.Play(test_clip_, EAudioGroup.UI);
            current_handle_ = handle;
            Debug.Log($"[Test] 播放 2D 音效（直接引用）: {test_clip_.name}, IsValid={handle.IsValid}");
        }

        /// <summary>
        /// 测试 2：通过 audio_id 播放（依赖 AudioClipDataSO）
        /// </summary>
        private void Test_Play2D_ByAudioId()
        {
            if (string.IsNullOrEmpty(test_audio_id_))
            {
                Debug.LogWarning("[Test] test_audio_id_ 为空！请设置一个有效的音频 ID。");
                return;
            }

            AudioHandle handle = AudioManager.Instance.Play(test_audio_id_);
            if (handle.IsValid)
            {
                current_handle_ = handle;
                Debug.Log($"[Test] 通过 audio_id 播放成功: {test_audio_id_}");
            }
            else
            {
                Debug.LogWarning($"[Test] 通过 audio_id 播放失败: {test_audio_id_}（请检查 AudioClipDataSO 中是否配置了该 ID）");
            }
        }

        /// <summary>
        /// 测试 3：在随机世界坐标播放 3D 音效
        /// </summary>
        private void Test_Play3D_AtPosition()
        {
            if (test_clip_ == null)
            {
                Debug.LogWarning("[Test] test_clip_ 未赋值！");
                return;
            }

            Vector3 randomPos = new Vector3(
                Random.Range(-5f, 5f),
                0f,
                Random.Range(-5f, 5f)
            );

            AudioHandle handle = AudioManager.Instance.PlayAtPosition(test_clip_, EAudioGroup.Environment, randomPos);
            current_handle_ = handle;
            Debug.Log($"[Test] 播放 3D 音效，位置: {randomPos}, IsValid={handle.IsValid}");
        }

        /// <summary>
        /// 测试 4：循环播放 BGM
        /// </summary>
        private void Test_PlayBGM_Loop()
        {
            if (test_bgm_clip_ == null)
            {
                Debug.LogWarning("[Test] test_bgm_clip_ 未赋值！请在 Inspector 中拖入一个 BGM AudioClip。");
                return;
            }

            // 如果已有 BGM 在播放，先停止
            if (bgm_handle_ != null && bgm_handle_.Value.IsValid)
            {
                bgm_handle_.Value.Stop();
                Debug.Log("[Test] 停止旧 BGM");
            }

            AudioHandle handle = AudioManager.Instance.PlayLoop(test_bgm_clip_, EAudioGroup.BGM);
            bgm_handle_ = handle;
            Debug.Log($"[Test] BGM 循环播放: {test_bgm_clip_.name}, IsValid={handle.IsValid}");
        }

        /// <summary>
        /// 测试 5：跟随目标移动的 3D 音效
        /// </summary>
        private void Test_Play3D_Attached()
        {
            if (test_clip_ == null)
            {
                Debug.LogWarning("[Test] test_clip_ 未赋值！");
                return;
            }

            Transform target = test_3d_target_;
            if (target == null)
            {
                // 使用当前 GameObject 作为目标
                target = transform;
                Debug.Log("[Test] 未指定 3D 目标，使用当前 GameObject 作为跟随目标");
            }

            // 停止之前的 attached 音效
            if (attached_handle_ != null && attached_handle_.Value.IsValid)
            {
                attached_handle_.Value.Stop();
            }

            AudioHandle handle = AudioManager.Instance.PlayAttached(test_clip_, EAudioGroup.Environment, target);
            attached_handle_ = handle;
            current_handle_ = handle;
            Debug.Log($"[Test] 跟随播放，目标: {target.name}, IsValid={handle.IsValid}");
        }

        /// <summary>
        /// 测试：暂停当前音效
        /// </summary>
        private void Test_Pause()
        {
            if (current_handle_ != null && current_handle_.Value.IsValid)
            {
                current_handle_.Value.Pause();
                Debug.Log("[Test] 暂停当前音效");
            }
            else
            {
                Debug.Log("[Test] 没有可暂停的音效");
            }
        }

        /// <summary>
        /// 测试：恢复当前音效
        /// </summary>
        private void Test_Resume()
        {
            if (current_handle_ != null && current_handle_.Value.IsValid)
            {
                current_handle_.Value.Resume();
                Debug.Log("[Test] 恢复当前音效");
            }
            else
            {
                Debug.Log("[Test] 没有可恢复的音效");
            }
        }

        /// <summary>
        /// 测试：停止当前音效
        /// </summary>
        private void Test_Stop()
        {
            if (current_handle_ != null && current_handle_.Value.IsValid)
            {
                current_handle_.Value.Stop();
                Debug.Log("[Test] 停止当前音效");
                current_handle_ = null;
            }
            else
            {
                Debug.Log("[Test] 没有可停止的音效");
            }
        }

        /// <summary>
        /// 测试：停止所有音效
        /// </summary>
        private void Test_StopAll()
        {
            AudioManager.Instance.StopAll();
            current_handle_ = null;
            bgm_handle_ = null;
            attached_handle_ = null;
            Debug.Log("[Test] 已停止所有音效");
        }

        /// <summary>
        /// 测试：增大 BGM 音量
        /// </summary>
        private void Test_IncreaseVolume()
        {
            float current = AudioManager.Instance.GetVolume(EAudioGroup.BGM);
            float newVolume = Mathf.Min(current + 0.1f, 1f);
            AudioManager.Instance.SetVolume(EAudioGroup.BGM, newVolume);
            Debug.Log($"[Test] BGM 音量: {current:F2} -> {newVolume:F2}");
        }

        /// <summary>
        /// 测试：减小 BGM 音量
        /// </summary>
        private void Test_DecreaseVolume()
        {
            float current = AudioManager.Instance.GetVolume(EAudioGroup.BGM);
            float newVolume = Mathf.Max(current - 0.1f, 0f);
            AudioManager.Instance.SetVolume(EAudioGroup.BGM, newVolume);
            Debug.Log($"[Test] BGM 音量: {current:F2} -> {newVolume:F2}");
        }

        /// <summary>
        /// 测试：音调随机变化（Battle 组，pitch 在 0.8~1.5 之间）
        /// </summary>
        private void Test_PitchRandomization()
        {
            if (test_clip_ == null)
            {
                Debug.LogWarning("[Test] test_clip_ 未赋值！");
                return;
            }

            Vector2 pitchRange = new Vector2(0.8f, 1.5f);
            // 播放 5 次，每次 pitch 随机
            for (int i = 0; i < 5; i++)
            {
                AudioHandle handle = AudioManager.Instance.Play(test_clip_, EAudioGroup.Battle, pitchRange);
                Debug.Log($"[Test] 随机音调播放 #{i + 1}，pitch 范围: {pitchRange.x}~{pitchRange.y}");
            }
        }

        /// <summary>
        /// 测试：打印当前状态
        /// </summary>
        private void Test_PrintStatus()
        {
            Debug.Log("===== AudioSystem Status =====");

            if (AudioManager.Instance.Settings != null)
            {
                Debug.Log($"  Master Volume:    {AudioManager.Instance.GetVolume(EAudioGroup.Master):F2}");
                Debug.Log($"  BGM Volume:       {AudioManager.Instance.GetVolume(EAudioGroup.BGM):F2}");
                Debug.Log($"  Battle Volume:    {AudioManager.Instance.GetVolume(EAudioGroup.Battle):F2}");
                Debug.Log($"  UI Volume:        {AudioManager.Instance.GetVolume(EAudioGroup.UI):F2}");
                Debug.Log($"  Environment Vol:  {AudioManager.Instance.GetVolume(EAudioGroup.Environment):F2}");
                Debug.Log($"  Dialogue Volume:  {AudioManager.Instance.GetVolume(EAudioGroup.Dialogue):F2}");
                Debug.Log($"  使用 AudioMixer:  {AudioManager.Instance.Settings.HasMixer}");
            }
            else
            {
                Debug.Log("  Settings: null（使用默认值）");
            }

            if (AudioManager.Instance.ClipData != null)
            {
                Debug.Log($"  AudioClipData 条目数: {AudioManager.Instance.ClipData.Entries.Count}");
            }
            else
            {
                Debug.Log("  AudioClipData: null");
            }

            Debug.Log($"  current_handle_ IsValid: {current_handle_?.IsValid}, IsPlaying: {current_handle_?.IsPlaying}");
            Debug.Log($"  bgm_handle_ IsValid:     {bgm_handle_?.IsValid}, IsPlaying: {bgm_handle_?.IsPlaying}");
            Debug.Log($"  attached_handle_ IsValid:{attached_handle_?.IsValid}, IsPlaying: {attached_handle_?.IsPlaying}");
            Debug.Log("==============================");
        }

        /// <summary>
        /// 自动逐步运行全部测试（用于快速验证）
        /// </summary>
        private IEnumerator RunAllTestsCoroutine()
        {
            Debug.Log("===== 开始自动测试 =====");

            // 测试 1：状态打印
            Test_PrintStatus();
            yield return new WaitForSeconds(0.5f);

            // 测试 2：2D 直接引用播放
            if (test_clip_ != null)
            {
                Test_Play2D_DirectClip();
                yield return new WaitForSeconds(1.5f);
            }
            else
            {
                Debug.LogWarning("[AutoTest] 跳过 2D 直接引用测试（test_clip_ 为空）");
            }

            // 测试 3：暂停和恢复
            Test_Pause();
            yield return new WaitForSeconds(0.5f);
            Test_Resume();
            yield return new WaitForSeconds(1f);

            // 测试 4：停止
            Test_Stop();
            yield return new WaitForSeconds(0.5f);

            // 测试 5：通过 audio_id 播放
            Test_Play2D_ByAudioId();
            yield return new WaitForSeconds(1.5f);
            Test_StopAll();
            yield return new WaitForSeconds(0.5f);

            // 测试 6：3D 位置播放
            if (test_clip_ != null)
            {
                Test_Play3D_AtPosition();
                yield return new WaitForSeconds(1.5f);
                Test_StopAll();
                yield return new WaitForSeconds(0.5f);
            }

            // 测试 7：BGM 循环
            if (test_bgm_clip_ != null)
            {
                Test_PlayBGM_Loop();
                yield return new WaitForSeconds(2f);
                Test_StopAll();
                yield return new WaitForSeconds(0.5f);
            }

            // 测试 8：音量变化
            Test_IncreaseVolume();
            yield return new WaitForSeconds(0.3f);
            Test_IncreaseVolume();
            yield return new WaitForSeconds(0.3f);
            // 恢复音量
            AudioManager.Instance.SetVolume(EAudioGroup.BGM, 1f);
            Debug.Log("[AutoTest] BGM 音量已恢复为 1.0");
            yield return new WaitForSeconds(0.5f);

            // 测试 9：音调随机
            if (test_clip_ != null)
            {
                Test_PitchRandomization();
                yield return new WaitForSeconds(2f);
                Test_StopAll();
            }

            // 测试 10：跟随播放
            if (test_clip_ != null)
            {
                Test_Play3D_Attached();
                yield return new WaitForSeconds(2f);
                Test_StopAll();
            }

            // 最终状态
            yield return new WaitForSeconds(0.5f);
            Test_PrintStatus();

            Debug.Log("===== 自动测试完成 =====");
        }

        #endregion
    }
}