using System.Runtime.CompilerServices;

namespace Marioalexsan.ModAudio;

// TODO: Investigate Burst with hand-written intrinsics
internal static class OptimizedMethods
{
    // Note: Burst's automated vectorization couldn't optimize this method any better than JIT
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void MultiplyFloatArray(float[] data, float factor)
    {
        unsafe
        {
            fixed (float* start = data)
            {
                float* current = start;
                float* end = start + data.Length;

                while (current != end)
                {
                    *current = *current * factor;
                    current++;
                }
            }
        }
    }
}
