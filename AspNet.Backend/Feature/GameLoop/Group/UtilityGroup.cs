using System.Runtime.CompilerServices;
using Arch.System;

namespace AspNet.Backend.Feature.GameLoop.Group;

/// <summary>
/// The <see cref="IntervalGroup"/> class
/// is a group of systems that are being executed in a set interval. 
/// </summary>
public class IntervalGroup : ISystem<float>
{
    public IntervalGroup(float triggerSec, params ISystem<float>[] systems)
    {
        Systems = systems;
        TriggerSec = triggerSec;
    }

    public IntervalGroup(float triggerSec, IEnumerable<ISystem<float>> systems)
    {
        Systems = (systems ?? throw new ArgumentNullException(nameof(systems))).Where(s => s != null).ToArray();
        TriggerSec = triggerSec;
    }

    /// <summary>
    /// The registered <see cref="ISystem{T}"/>s.
    /// </summary>
    private ISystem<float>[] Systems { get; }
    
    /// <summary>
    /// The counted intervall.
    /// </summary>
    public float Intervall { get; set; }
    
    /// <summary>
    /// The time that triggers the execution once <see cref="Intervall"/> reached it.
    /// </summary>
    public float TriggerSec { get; set; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Initialize()
    {
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void BeforeUpdate(in float t)
    {
        Intervall += t;
        
        if (Intervall <= TriggerSec) return;
        for (var index = 0; index < Systems.Length; index++)
        {
            var system = Systems[index];
            system.BeforeUpdate(t);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Update(in float t)
    {
        if (Intervall <= TriggerSec) return;
        for (var index = 0; index < Systems.Length; index++)
        {
            var system = Systems[index];
            system.Update(t);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AfterUpdate(in float t)
    {
        if (Intervall <= TriggerSec) return;
        Intervall = 0;
        
        for (var index = 0; index < Systems.Length; index++)
        {
            var system = Systems[index];
            system.AfterUpdate(t);
        }
    }
    
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Dispose()
    {
        for (var index = 0; index < Systems.Length; index++)
        {
            var system = Systems[index];
            system.Dispose();
        }
    }
}