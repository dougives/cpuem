using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cpuem.InstructionSet
{
    public class OpMap
    {
        const int MAP_SIZE = 16;

        readonly string[] tokenmap;

        public string this[int i]
        {
            get
            {
                if (i >= MAP_SIZE)
                    throw new IndexOutOfRangeException(string.Format(
                        "indexer must be less than map size of {0}",
                        MAP_SIZE));
                if (i < 0)
                    throw new IndexOutOfRangeException(string.Format(
                        "indexer must be non-negative",
                        MAP_SIZE));
                return tokenmap[i];
            }
            private set { }
        }

        public OpMap(string[] tokenmap)
        {
            if (tokenmap.Length != 16)
                throw new InvalidOperationException(string.Format(
                    "tokenmap should be of size {0}",
                    MAP_SIZE));
            this.tokenmap = tokenmap;
        }
    }
}
