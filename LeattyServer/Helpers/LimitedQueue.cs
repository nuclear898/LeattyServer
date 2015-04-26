using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LeattyServer.Helpers
{
    public class LimitedQueue<T> : Queue<T>
    {
        private int Size;

        public LimitedQueue(int size) 
        {
            Size = size;
        }

        public new void Enqueue(T item)
        {
            while (this.Count >= Size)
            {
                this.Dequeue();
            }
            base.Enqueue(item);
        }        
    }
}
