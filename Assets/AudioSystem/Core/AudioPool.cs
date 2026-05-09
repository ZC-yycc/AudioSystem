using System.Collections.Generic;
using UnityEngine;

namespace AudioSystem
{
    /// <summary>
    /// AudioSource 对象池，避免频繁创建销毁，提高性能
    /// </summary>
    public class AudioPool
    {
        private readonly GameObject                         pool_root_;
        private readonly Queue<AudioSource>                 pool_ = new();
        private readonly List<AudioSource>                  active_sources_ = new();
        private readonly int                                initial_capacity_;
        private readonly int                                max_capacity_;

        public int ActiveCount => active_sources_.Count;
        public int PooledCount => pool_.Count;

        public AudioPool(Transform parent, int initial_capacity = 10, int max_capacity = 30)
        {
            pool_root_ = new GameObject("[AudioPool]");
            pool_root_.transform.SetParent(parent);
            initial_capacity_ = initial_capacity;
            max_capacity_ = max_capacity;

            for (int i = 0; i < initial_capacity; i++)
            {
                CreateNewSource();
            }
        }

        /// <summary>
        /// 从池中获取一个可用的 AudioSource
        /// </summary>
        public AudioSource Get()
        {
            // 清理已播放完毕的活跃音源，回收到池中
            for (int i = active_sources_.Count - 1; i >= 0; i--)
            {
                AudioSource src = active_sources_[i];
                if (src == null)
                {
                    active_sources_.RemoveAt(i);
                    continue;
                }
                if (!src.isPlaying && !src.loop)
                {
                    active_sources_.RemoveAt(i);
                    ReturnToPool(src);
                }
            }

            if (pool_.Count > 0)
            {
                AudioSource source = pool_.Dequeue();
                active_sources_.Add(source);
                source.gameObject.SetActive(true);
                return source;
            }

            if (active_sources_.Count + pool_.Count < max_capacity_)
            {
                AudioSource source = CreateNewSource();
                active_sources_.Add(source);
                return source;
            }

            // 超出最大容量，回收最旧的一个活跃音源（优先回收非循环的）
            for (int i = 0; i < active_sources_.Count; i++)
            {
                if (!active_sources_[i].loop)
                {
                    AudioSource old_source = active_sources_[i];
                    old_source.Stop();
                    old_source.clip = null;
                    active_sources_.RemoveAt(i);
                    active_sources_.Add(old_source);
                    return old_source;
                }
            }

            // 如果都在循环播放，复用第一个
            AudioSource fallback = active_sources_[0];
            fallback.Stop();
            fallback.clip = null;
            fallback.loop = false;
            active_sources_.RemoveAt(0);
            active_sources_.Add(fallback);
            return fallback;
        }

        /// <summary>
        /// 停止所有活跃音源
        /// </summary>
        public void StopAll()
        {
            for (int i = active_sources_.Count - 1; i >= 0; i--)
            {
                AudioSource src = active_sources_[i];
                if (src == null)
                {
                    active_sources_.RemoveAt(i);
                    continue;
                }
                src.Stop();
                src.clip = null;
                src.loop = false;
                ReturnToPool(src);
                active_sources_.RemoveAt(i);
            }
        }

        public void Destroy()
        {
            StopAll();
            foreach (AudioSource src in pool_)
            {
                if (src != null)
                    Object.Destroy(src.gameObject);
            }
            pool_.Clear();
            active_sources_.Clear();
        }

        private AudioSource CreateNewSource()
        {
            GameObject go = new GameObject($"AudioSource_{pool_.Count}");
            go.transform.SetParent(pool_root_.transform);
            go.SetActive(false);

            AudioSource source = go.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f; // 默认2D音效

            pool_.Enqueue(source);
            return source;
        }

        private void ReturnToPool(AudioSource source)
        {
            if (source == null) return;
            source.Stop();
            source.clip = null;
            source.loop = false;
            source.spatialBlend = 0f;
            source.gameObject.SetActive(false);
            if (!pool_.Contains(source))
                pool_.Enqueue(source);
        }
    }
}