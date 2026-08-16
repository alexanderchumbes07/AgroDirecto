using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AgroDirecto.Web.Controllers;

// Panel del administrador: agrupa los mantenimientos del sistema.
[Authorize(Roles = "Administrador")]
public class PanelAdminController : Controller
{
    // GET: /PanelAdmin
    public IActionResult Index() => View();
}
