using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AgroDirecto.Web.Controllers;

// Panel del agricultor. Por ahora solo la pantalla de inicio;
// los módulos se agregan en la Fase 4 (productos) y siguientes.
[Authorize(Roles = "Agricultor")]
public class PanelAgricultorController : Controller
{
    // GET: /PanelAgricultor
    public IActionResult Index() => View();
}
