using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using SignalR;
using Tic_Tac_R.Models;
using System.Text;

namespace Tic_Tac_R.Controllers
{
    public class GameController : Controller
    {
        SignalR.Hubs.IHubContext _context;
        public GameController()
        {
            _context = GlobalHost.ConnectionManager.GetHubContext<GameEngine>();
        }

        public ActionResult Play()
        {
            return View("Login");
        }

        [HttpPost]
        public ActionResult Play(Player player)
        {
            HttpContext.Session["player"] = player;
            return View();

        }
    }
}
