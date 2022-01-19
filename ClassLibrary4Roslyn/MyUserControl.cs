using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClassLibrary4Roslyn
{
    public partial class MyUserControl : Component
    {
        public MyUserControl()
        {
            InitializeComponent();
        }

        public MyUserControl(IContainer container)
        {
            container.Add(this);

            InitializeComponent();
        }
    }
}
