using System;
using System.Threading.Tasks;
using TerrariaInjector.Utils;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

/*
 *  From https://github.com/fernandoescolar/ChildServiceProvider/blob/main/src/ChildServiceProvider.cs
MIT License

Copyright (c) 2025 Fernando Escolar

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
 */
public sealed class ChildServiceProvider : CompositeServiceProvider, IServiceScopeFactory, IDisposable, IAsyncDisposable
{
    private readonly IServiceProvider _parentProvider;
    private readonly IServiceProvider _innerProvider;
    private readonly IChildServiceCollection _childServices;
    private readonly bool _ownsInnerProvider;
    private bool _disposed;

    internal ChildServiceProvider(IServiceProvider parentProvider, IChildServiceCollection childServices)
        : this(
            parentProvider,
            childServices,
            childServices.BuildServiceProvider(),
            true)
    {
    }

    internal ChildServiceProvider(
        IServiceProvider parentProvider,
        IChildServiceCollection childServices,
        IServiceProvider innerProvider,
        bool ownsInnerProvider) : base(innerProvider, parentProvider)
    {
        _parentProvider = parentProvider ?? throw new ArgumentNullException(nameof(parentProvider));
        _childServices = childServices ?? throw new ArgumentNullException(nameof(childServices));
        _innerProvider = innerProvider ?? throw new ArgumentNullException(nameof(innerProvider));
        _ownsInnerProvider = ownsInnerProvider;
    }

    public IServiceScope CreateScope()
    {
        var parentScopeFactory = _parentProvider.GetRequiredService<IServiceScopeFactory>();
        var parentScope = parentScopeFactory.CreateScope();

        var innerScopeFactory = _innerProvider.GetRequiredService<IServiceScopeFactory>();
        var innerScope = innerScopeFactory.CreateScope();

        return new ChildServiceScope(parentScope, innerScope);
    }

    public override object? GetService(Type serviceType)
    {
        Guard.ThrowIfDisposed(_disposed, this);
        Guard.ThrowIfNull(serviceType);

        if (serviceType == typeof(IServiceScopeFactory) || serviceType == typeof(IServiceProvider) || serviceType == typeof(IKeyedServiceProvider))
        {
            return this;
        }

        return base.GetService(serviceType);
    }

    public override object? GetKeyedService(Type serviceType, object? serviceKey)
    {
        Guard.ThrowIfDisposed(_disposed, this);
        Guard.ThrowIfNull(serviceType);

        return base.GetKeyedService(serviceType, serviceKey);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        if (_ownsInnerProvider && _innerProvider is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }
    
    public ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return default;
        }

        _disposed = true;

        if (!_ownsInnerProvider)
        {
            return default;
        }

        if (_innerProvider is IAsyncDisposable asyncDisposable)
        {
            return asyncDisposable.DisposeAsync();
        }

        if (_innerProvider is IDisposable disposable)
        {
            disposable.Dispose();
        }

        return default;
    }
}