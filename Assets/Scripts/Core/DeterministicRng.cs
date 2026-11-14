using System;
using System.Collections.Generic;

namespace SevenPages.Core
{
    /// <summary>
    /// 确定性随机数（SplitMix64）。同一种子必然产生同一局，方便复现 bug 与录像回放。
    /// </summary>
    public sealed class DeterministicRng
    {
        private ulong _state;

        public DeterministicRng(int seed)
        {
            _state = (ulong)(uint)seed * 6364136223846793005UL + 1442695040888963407UL;
        }

        public uint NextUInt()
        {
            _state += 0x9E3779B97F4A7C15UL;
            ulong z = _state;
            z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
            z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
            return (uint)((z ^ (z >> 31)) >> 32);
        }

        /// <summary>返回 [0, maxExclusive) 的整数。</summary>
        public int Next(int maxExclusive)
        {
            if (maxExclusive <= 0) return 0;
            return (int)(NextUInt() % (uint)maxExclusive);
        }

        /// <summary>返回 [min, maxExclusive)。</summary>
        public int Range(int min, int maxExclusive)
        {
            if (maxExclusive <= min) return min;
            return min + Next(maxExclusive - min);
        }

        /// <summary>Fisher–Yates 洗牌。</summary>
        public void Shuffle<T>(IList<T> list)
        {
            if (list == null) return;
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = Next(i + 1);
                T tmp = list[i];
                list[i] = list[j];
                list[j] = tmp;
            }
        }
    }
}
