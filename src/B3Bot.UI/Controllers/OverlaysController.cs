using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace B3Bot.UI.Controllers
{
    public class OverlaysController : Controller
    {
        public IActionResult Followers()
        {
            return View();
        }

        public IActionResult Viewers()
        {
            return View();
        }

    }
}
