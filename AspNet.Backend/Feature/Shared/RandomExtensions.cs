using System.Numerics;
using System.Runtime.CompilerServices;

namespace AspNet.Backend.Feature.Shared;

/// <summary>
///     An extension for the buildin <see cref="System.Random" />
/// </summary>
public static class RandomExtensions
{
    //Function to get random number, prevent allocating one new random instance each time
    private static readonly Random Random = new();

    /// <summary>
    ///     Reuturns a random int between
    /// </summary>
    /// <param name="min"></param>
    /// <param name="max"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetRandom(int min, int max)
    {
        lock (Random)
        {
            return Random.Next(min, max);
        }
    }

    /// <summary>
    ///     Returns a random float between
    /// </summary>
    /// <param name="min"></param>
    /// <param name="max"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float GetRandom(float min, float max)
    {
        lock (Random)
        {
            return (float)(Random.NextDouble() * (max - min) + min);
        }
    }

    /// <summary>
    ///     Returns a unique unsigned int
    /// </summary>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint GetUniqueInt()
    {
        lock (Random)
        {
            var buffer = new byte[sizeof(uint)];
            Random.NextBytes(buffer);
            return BitConverter.ToUInt32(buffer, 0);
        }
    }
    

    /// <summary>
    ///     Creates a random rotation
    /// </summary>
    /// <param name="random"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Quaternion Quaternion()
    {
        lock (Random)
        {
            var randomX = Random.Next(0, 360) * (Math.PI / 180);
            var randomY = Random.Next(0, 360) * (Math.PI / 180);
            var randomZ = Random.Next(0, 360) * (Math.PI / 180);

            return System.Numerics.Quaternion.CreateFromYawPitchRoll((float)randomX, (float)randomY, (float)randomZ);
        }
    }

    /// <summary>
    ///     Creates a random rotation
    /// </summary>
    /// <param name="random"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Quaternion QuaternionStanding()
    {
        lock (Random)
        {
            var randomX = Random.Next(0, 360) * (Math.PI / 180);
            return System.Numerics.Quaternion.CreateFromYawPitchRoll((float)randomX, 0, 0);
        }
    }
}