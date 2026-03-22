using System.IO;
using System.Reflection;
using TerrariaInjector.Core.Logging;

namespace TerrariaInjector.Extensions;

public static class AssemblyExtensions
{

    public static void LoadAssembliesFromManifestResourceStream(this Assembly assembly, ILogger logger = null)
    {
        var isLoggerNull = logger is null;
        foreach (var file in assembly.GetManifestResourceNames())
        {
            if (file.Contains(".dll"))
            {
                if (isLoggerNull == false)
                {
                    logger.LogInformation("Loading: " + file);
                }
                Stream input = assembly.GetManifestResourceStream(file);
                Assembly.Load(ReadStreamAssembly(input));
            }
        }
    }
    
    private static byte[] ReadStreamAssembly(Stream assemblyStream)
    {
        byte[] array = new byte[assemblyStream.Length];
        using (Stream a = assemblyStream)
        {
            a.Read(array, 0, array.Length);
        }
        return array;
    }
}