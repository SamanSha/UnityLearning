using System.Collections;
using System.Collections.Generic;
using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

using static Unity.Mathematics.math;

public static partial class Noise {

    [Serializable]
    public struct Settings {

        public int seed;

        [Min(1)]
        public int frequency;

        public static Settings Default => new Settings {
            frequency = 4
        };
    }

    public interface INoise {
        float4 GetNoise4 (float4x3 positions, SmallXXHash4 hash);
    }

    [BurstCompile(FloatPrecision.Standard, FloatMode.Fast, CompileSynchronously = true)]
    public struct Job<N> : IJobFor where N : struct, INoise {

        [ReadOnly]
        public NativeArray<float3x4> positions;

        [WriteOnly]
        public NativeArray<float4> noise;

        public Settings settings;

        public float3x4 domainTRS;

        public void Execute (int i) {
            float4x3 position = domainTRS.TransformVectors(transpose(positions[i]));
            var hash = SmallXXHash4.Seed(settings.seed);
            int frequency = settings.frequency;
            noise[i] = default(N).GetNoise4(frequency * position, hash);
        }

        public static JobHandle ScheduleParallel(
            NativeArray<float3x4> positions, NativeArray<float4> noise,
            Settings settings, SpaceTRS domainTRS, int resolution, JobHandle dependency
        ) => new Job<N> {
            positions = positions,
            noise = noise,
            settings = settings,
            domainTRS = domainTRS.Matrix,
        }.ScheduleParallel(positions.Length, resolution, dependency);
    }

    public delegate JobHandle ScheduleDelegate (
        NativeArray<float3x4> positions, NativeArray<float4> noise, 
        Settings settings, SpaceTRS domainTRS, int resolution, JobHandle dependency
    );
}
