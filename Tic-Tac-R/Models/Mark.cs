using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Tic_Tac_R.Models
{
    public class Mark
    {
        public int X { get; set; }
        public int Y { get; set; }
        public string XY { get { return X.ToString() + "," + Y.ToString(); } }
    }
}