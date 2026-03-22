using System.Collections;
using System.Collections.Generic;
using System;
using TerrariaInjector.Utils;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

/*
 *  From https://github.com/fernandoescolar/ChildServiceProvider/blob/main/src/ChildServiceCollection.cs
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
public sealed class ChildServiceCollection(IServiceCollection parent) : IChildServiceCollection
{
    public IServiceCollection ParentServices { get; } = parent ?? throw new ArgumentNullException(nameof(parent));

    public IServiceCollection ChildServices { get; } = new ServiceCollection();

    public ServiceDescriptor this[int index]
    {
        get
        {
            if (index < 0 || index >= Count)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            return index < ParentServices.Count
                ? ParentServices[index]
                : ChildServices[index - ParentServices.Count];
        }
        set
        {
            if (index < 0 || index >= Count)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            if (index < ParentServices.Count)
            {
                throw new NotSupportedException("Cannot modify parent service descriptors.");
            }

            ChildServices[index - ParentServices.Count] = value;
        }
    }

    public int Count => ParentServices.Count + ChildServices.Count;

    public bool IsReadOnly => false;

    public void Add(ServiceDescriptor item) => ChildServices.Add(item);

    public void Clear() => ChildServices.Clear();

    public bool Contains(ServiceDescriptor item) => ChildServices.Contains(item) || ParentServices.Contains(item);

    public void CopyTo(ServiceDescriptor[] array, int arrayIndex)
    {
        Guard.ThrowIfNull(array);

        if (arrayIndex < 0 || arrayIndex > array.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(arrayIndex));
        }

        if (array.Length - arrayIndex < Count)
        {
            throw new ArgumentException("The target array is not large enough to contain the collection.", nameof(array));
        }

        var offset = arrayIndex;
        foreach (var descriptor in ParentServices)
        {
            array[offset++] = descriptor;
        }

        foreach (var descriptor in ChildServices)
        {
            array[offset++] = descriptor;
        }
    }

    public IEnumerator<ServiceDescriptor> GetEnumerator()
    {
        foreach (var descriptor in ParentServices)
        {
            yield return descriptor;
        }

        foreach (var descriptor in ChildServices)
        {
            yield return descriptor;
        }
    }

    public int IndexOf(ServiceDescriptor item)
    {
        var parentIndex = ParentServices.IndexOf(item);
        if (parentIndex >= 0)
        {
            return parentIndex;
        }

        var innerIndex = ChildServices.IndexOf(item);
        return innerIndex >= 0 ? ParentServices.Count + innerIndex : -1;
    }

    public void Insert(int index, ServiceDescriptor item)
    {
        if (index < 0 || index > Count)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        if (index < ParentServices.Count)
        {
            throw new NotSupportedException("Cannot insert before parent service descriptors.");
        }

        ChildServices.Insert(index - ParentServices.Count, item);
    }

    public bool Remove(ServiceDescriptor item)
    {
        if (ChildServices.Remove(item))
        {
            return true;
        }

        if (ParentServices.Contains(item))
        {
            throw new NotSupportedException("Cannot remove parent service descriptors.");
        }

        return false;
    }

    public void RemoveAt(int index)
    {
        if (index < 0 || index >= Count)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        if (index < ParentServices.Count)
        {
            throw new NotSupportedException("Cannot remove parent service descriptors.");
        }

        ChildServices.RemoveAt(index - ParentServices.Count);
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}