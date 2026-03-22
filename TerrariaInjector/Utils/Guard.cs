#nullable enable
using System;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.DependencyInjection;

namespace TerrariaInjector.Utils
{
    public static class Guard
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ThrowIfNull<T>(T? obj, [CallerArgumentExpression(nameof(obj))] string paramName = "obj") where T : class
        {
            if (obj == null)
            {
                throw new ArgumentNullException(paramName);
            }
        }

        public static void ThrowIfDisposed(bool condition, object instance)
        {
            if (condition)
            {
                throw new ObjectDisposedException(instance?.GetType().FullName);
            }
        }
    }
    
}

