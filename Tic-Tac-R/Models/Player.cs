using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace Tic_Tac_R.Models
{
    public class Player
    {
        public string Uid { get; set; } //unique identifier of the player
        [Required]
        public string Nick { get; set; }
        public string ConnectionId { get; set; }
        public string NowPlaying { get; set; }
        public bool Turn { get; set; }
        public string RoomName { get; set; }
        public List<Mark> Marks { get; set; }
    }
}