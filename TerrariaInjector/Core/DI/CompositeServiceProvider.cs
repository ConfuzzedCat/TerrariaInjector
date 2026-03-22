using System;
using TerrariaInjector.Utils;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

/*
 *  From https://github.com/fernandoescolar/ChildServiceProvider/blob/main/src/CompositeServiceProvider.cs
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
public class CompositeServiceProvider : IServiceProvider, IKeyedServiceProvider
{
    private readonly IServiceProvider _primary;
    private readonly IServiceProvider _secondary;

    internal CompositeServiceProvider(IServiceProvider primary, IServiceProvider secondary)
    {
        _primary = primary;
        _secondary = secondary;
    }

    public virtual object? GetService(Type serviceType)
    {
        Guard.ThrowIfNull(serviceType);

        return _primary.GetService(serviceType) ?? _secondary.GetService(serviceType);
    }

    public virtual object? GetKeyedService(Type serviceType, object? serviceKey)
    {
        Guard.ThrowIfNull(serviceType);

        if (_primary is IKeyedServiceProvider primaryKeyed)
        {
            var primaryResult = primaryKeyed.GetKeyedService(serviceType, serviceKey);
            if (primaryResult is not null)
            {
                return primaryResult;
            }
        }
        else if (serviceKey is null)
        {
            var fallback = _primary.GetService(serviceType);
            if (fallback is not null)
            {
                return fallback;
            }
        }

        if (_secondary is IKeyedServiceProvider secondaryKeyed)
        {
            return secondaryKeyed.GetKeyedService(serviceType, serviceKey);
        }

        return serviceKey is null ? _secondary.GetService(serviceType) : null;
    }

    public object GetRequiredKeyedService(Type serviceType, object? serviceKey)
    {
        var resolved = GetKeyedService(serviceType, serviceKey);
        if (resolved is null)
        {
            throw new InvalidOperationException($"Unable to resolve keyed service of type '{serviceType}' with key '{serviceKey ?? "<null>"}'.");
        }

        return resolved;
    }
}