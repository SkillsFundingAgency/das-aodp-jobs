using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SFA.DAS.AODP.Jobs.Extensions
{
    public static class ListExtentions
    {
        public static List<T> Append<T>(this List<T> list,T value)
        {
            list.Add(value);
            return list;
        }

        public static List<T> AppendIf<T>(this List<T> list, bool condition, T value)
        {
            return condition ? list.Append(value) : list;
        }
    }
}
