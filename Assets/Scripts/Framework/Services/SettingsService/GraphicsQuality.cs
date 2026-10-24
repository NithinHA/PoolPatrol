using UnityEngine;

namespace PTL.Framework.Services
{
    /// <summary>
    /// The graphics quality tier the player picks in the Settings popup (Low / Mid / High).
    /// Persisted by <see cref="ISettingsService"/>. What each tier actually changes lives in
    /// <see cref="GraphicsQualityProfile"/>, so systems never switch on the enum directly.
    /// </summary>
    public enum GraphicsQuality
    {
        Low = 0,
        Mid = 1,
        High = 2,
    }

    /// <summary>
    /// Every rendering knob that differs between quality tiers, with one preset per tier in
    /// <see cref="For"/>. This is the single place to decide "what does Mid mean".
    ///
    /// Adding a knob:
    ///  1. Add a field here with a doc comment saying what it trades off.
    ///  2. Give it a value for each tier in <see cref="For"/>.
    ///  3. Read it from <see cref="ISettingsService.GraphicsProfile"/> in the system that owns the
    ///     effect, and re-apply on <see cref="ISettingsService.OnSettingsChanged"/>.
    ///     Scene-level knobs belong to that scene's system (e.g. PoolEnvironment for the water).
    ///     Project-wide knobs (target frame rate, URP render scale, Unity quality level, shadow
    ///     distance...) would be applied from SettingsService when the tier changes.
    ///
    /// Current consumers: PoolEnvironment (water shaders, floor bake, bloom).
    /// </summary>
    public struct GraphicsQualityProfile
    {
        /// <summary>
        /// Cheaper water shader variants (global keyword _POOL_LOW): no surface glints, no chromatic
        /// caustics, no caustic brightness variation, flatter deck. Saves the most GPU time.
        /// </summary>
        public bool CheapWaterShaders;

        /// <summary>
        /// Bloom post-processing on the gameplay volume. Makes foam and glints glow; the most
        /// expensive post effect on weak GPUs.
        /// </summary>
        public bool Bloom;

        /// <summary>
        /// Resolution of the baked pool floor, in texture pixels per world unit. ~64 is sharp on a
        /// 1080p phone at the current camera zoom (~8 MB); 40 is slightly soft (~3 MB).
        /// </summary>
        public int FloorBakePixelsPerUnit;

        /// <summary>The preset for a tier.</summary>
        public static GraphicsQualityProfile For(GraphicsQuality quality)
        {
            switch (quality)
            {
                case GraphicsQuality.Low:
                    return new GraphicsQualityProfile { CheapWaterShaders = true, Bloom = false, FloorBakePixelsPerUnit = 40 };
                case GraphicsQuality.Mid:
                    return new GraphicsQualityProfile { CheapWaterShaders = false, Bloom = false, FloorBakePixelsPerUnit = 48 };
                default:
                    return new GraphicsQualityProfile { CheapWaterShaders = false, Bloom = true, FloorBakePixelsPerUnit = 64 };
            }
        }

        /// <summary>
        /// Starting tier before the player has chosen one. A deliberately simple heuristic:
        /// phones with ≤3.5 GB RAM or ≤4 cores get Low, other phones with ≤6 GB get Mid,
        /// everything else (bigger phones, tablets, desktop, Editor) gets High.
        /// </summary>
        public static GraphicsQuality DefaultForDevice()
        {
            if (!Application.isMobilePlatform) return GraphicsQuality.High;
            if (SystemInfo.systemMemorySize <= 3500 || SystemInfo.processorCount <= 4) return GraphicsQuality.Low;
            if (SystemInfo.systemMemorySize <= 6000) return GraphicsQuality.Mid;
            return GraphicsQuality.High;
        }
    }
}
