using System;

namespace ClassLibrary4Roslyn
{
    internal class ClassWithNestedType
    {
        public enum NestedType
        {
            Type1,
            Type2
        }

        public static NestedType GetNestedType(string typeName)
        {
            if (typeName == null)
            {
                return NestedType.Type1;
            }
            else
            {
                return NestedType.Type2;
            }
        }
    }
}
