using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Comet.Tools.GM.UI
{
    internal static class Kernel
    {
        public static bool Desktop
        {
            get
            {
#if WINDOWS || MACCATALYST
            return true;
#else
                return false;
#endif
            }
        }
    }
}
