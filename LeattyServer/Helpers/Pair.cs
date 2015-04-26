using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LeattyServer.Helpers
{
    public class Pair<E, F>
    {
        public E Left { get; set; }
        public F Right { get; set; }        
      
        public Pair(E left, F right)
        {
            Left = left;
            Right = right;
        }     
    }
}
