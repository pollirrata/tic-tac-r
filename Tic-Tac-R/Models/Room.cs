using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Tic_Tac_R.Models
{
    public class Room
    {
        public string Name { get; set; }
        public List<Player> Players { get; set; }
    }
}