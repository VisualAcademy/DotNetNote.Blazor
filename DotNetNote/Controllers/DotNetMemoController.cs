using Microsoft.AspNetCore.Mvc;

namespace DotNetNote.Controllers
{
    public class DotNetMemoController : Controller
    {
        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public IActionResult Create() => View();

        [HttpGet]
        public IActionResult Details() => View();

        [HttpGet]
        public IActionResult Edit() => View();

        [HttpGet]
        public IActionResult Delete() => View();

        [HttpGet]
        public IActionResult Reply() => View();
    }
}
