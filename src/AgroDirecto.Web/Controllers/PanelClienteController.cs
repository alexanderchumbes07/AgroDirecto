using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AgroDirecto.Web.Controllers;

// Panel del cliente. Por ahora solo la pantalla de inicio;
// los módulos se agregan en la Fase 5 (catálogo) y siguientes.
[Authorize(Roles = "Cliente")]
public class PanelClienteController : Controller
{
    // GET: /PanelCliente
    public IActionResult Index() => View();
}
