using System;

namespace ClassLibrary4Roslyn
{
    internal static class StaticClassWithExtensionMethod
    {
        public static void LogError(this Exception ex)
        {
            Console.WriteLine(ex.ToString());
        }
    }
}
