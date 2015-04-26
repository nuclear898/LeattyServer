using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LeattyServer.Helpers
{
    class AutoIncrement
    {
        private Object locker = new Object();
        private int current;

        public AutoIncrement(int startValue = 0)
        {
            current = startValue;
        }

        public int Get
        {
            get
            {
                lock (locker)
                {
                    int ret = current;
                    current++;
                    return ret;
                }
            }
        }
    }
}
