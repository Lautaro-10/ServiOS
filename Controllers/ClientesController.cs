using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ServiOS.Controllers;

[Authorize(Policy = "SoloAdmin")]
public class ClientesController : Controller
{
    public IActionResult Index() => View();
}
