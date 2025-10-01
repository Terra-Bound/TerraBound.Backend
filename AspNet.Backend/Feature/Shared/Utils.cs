using Microsoft.EntityFrameworkCore;

namespace AspNet.Backend.Feature.Shared;

/// <summary>
/// The <see cref="Scoped{T}"/> struct
/// manages the lifecycle of a scoped service by creating/obtaining and disposing it.
/// </summary>
/// <param name="serviceProvider">The <see cref="IServiceProvider"/>.</param>
/// <typeparam name="T">The type.</typeparam>
public class Scoped<T>(IServiceProvider serviceProvider) : IDisposable where T : notnull
{
    private T? _instance;
    
    /// <summary>
    /// The <see cref="IServiceProvider"/> used to create & dispose the scoped service.
    /// </summary>
    private IServiceScope? ServiceScope { get; set; }

    /// <summary>
    /// The created scoped service.
    /// </summary>
    public T Value
    {
        get => _instance == null ? Create() : _instance;
        private set => _instance = value;
    }

    /// <summary>
    /// Gets a value indicating whether the instance has been created and is not null.
    /// </summary>
    public bool HasValue => _instance != null;

    /// <summary>
    /// Creates the scoped service.
    /// </summary>
    public T Create()
    {
        ServiceScope = serviceProvider.CreateScope();
        _instance = ServiceScope.ServiceProvider.GetRequiredService<T>();
        return _instance;
    }

    /// <summary>
    /// Disposes the scoped service.
    /// </summary>
    public void Dispose()
    {
        ServiceScope?.Dispose();
        Value = default!;
    }
}

/// <summary>
/// The <see cref="Cached{T}"/> struct
/// manages the lifecycle of a cached instance by creating/obtaining and disposing it.
/// </summary>
/// <param name="create">The <see cref="Func{T}"/> to create/obtain the instance.</param>
/// <typeparam name="T">The type.</typeparam>
public class Cached<T>(Func<T> create) : IDisposable where T : IDisposable
{
    private T? _instance;
    
    /// <summary>
    /// The created scoped service.
    /// </summary>
    public T Value
    {
        get => _instance == null ? Create() : _instance;
        private set => _instance = value;
    }

    /// <summary>
    /// Creates the scoped service.
    /// </summary>
    public T Create()
    {
        _instance = create.Invoke();
        return _instance;
    }

    /// <summary>
    /// Disposes the scoped service.
    /// </summary>
    public void Dispose()
    {
        _instance?.Dispose();
        Value = default!;
    }
}