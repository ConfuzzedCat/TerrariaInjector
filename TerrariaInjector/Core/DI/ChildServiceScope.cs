using System.Threading.Tasks;
using System;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

/*
 *  From https://github.com/fernandoescolar/ChildServiceProvider/blob/main/src/ChildServiceScope.cs
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
internal sealed class ChildServiceScope : IServiceScope, IAsyncDisposable
{
    private readonly IServiceScope _parentScope;
    private readonly IServiceScope _innerScope;
    private readonly IServiceProvider _serviceProvider;
    private bool _disposed;

    public ChildServiceScope(IServiceScope parentScope, IServiceScope innerScope)
    {
        _parentScope = parentScope ?? throw new ArgumentNullException(nameof(parentScope));
        _innerScope = innerScope ?? throw new ArgumentNullException(nameof(innerScope));
        _serviceProvider = new CompositeServiceProvider(_innerScope.ServiceProvider, _parentScope.ServiceProvider);
    }

    public IServiceProvider ServiceProvider => _serviceProvider;

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        _innerScope.Dispose();
        _parentScope.Dispose();
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        if (_innerScope is IAsyncDisposable asyncInner)
        {
            await asyncInner.DisposeAsync().ConfigureAwait(false);
        }
        else
        {
            _innerScope.Dispose();
        }

        if (_parentScope is IAsyncDisposable asyncParent)
        {
            await asyncParent.DisposeAsync().ConfigureAwait(false);
        }
        else
        {
            _parentScope.Dispose();
        }
    }
}