#nullable enable
using System;
using System.Runtime.CompilerServices;

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
    }
    
}

