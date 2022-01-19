using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClassLibrary4Roslyn
{
    [ClassUsedAsAttribute("name")]
    internal class TestClass
    {
        public void MakeCalls()
        {
            try
            {
                EnumHasReferences enumRef = EnumHasReferences.YES;
                StaticClass.StaticMethod();
                SingletonClass.Instance.GetHashCode();
                if (ClassWithOnlyConstants.IndexColumnName == "index" && enumRef == EnumHasReferences.YES)
                {

                }
            }
            catch (Exception ex)
            {
                ex.LogError();
            }
        }
    }
}
