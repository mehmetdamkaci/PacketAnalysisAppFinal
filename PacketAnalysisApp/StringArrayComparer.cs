using System;
using System.Collections.Generic;
using System.Linq;

namespace PacketAnalysisApp
{
    class StringArrayComparer : IEqualityComparer<string[]>
    {
        public bool Equals(string[] x, string[] y)
        {
            return x.SequenceEqual(y);
        }

        public int GetHashCode(string[] obj)
        {
            int hash = 17;
            foreach (var s in obj)
            {
                hash = hash * 31 + s.GetHashCode();
            }
            return hash;
        }
    }
}
