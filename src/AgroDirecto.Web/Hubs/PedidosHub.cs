using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using AgroDirecto.Web.Data;

namespace AgroDirecto.Web.Hubs;

/* Notificaciones en tiempo real de los pedidos.
   El navegador abre una conexión permanente (WebSocket) y el servidor le
   habla cuando pasa algo, sin que el usuario recargue.

   La pieza importante son los GRUPOS. Aquí viajan pedidos y precios, así
   que nadie recibe lo de otro: al conectarse, cada usuario entra al grupo
   que le corresponde y solo escucha ese.

       cliente-3      -> avisos de las compras del cliente 3
       agricultor-6   -> pedidos nuevos del agricultor 6

   El grupo se calcula AQUÍ, a partir de la cookie de sesión. Nunca lo
   manda el navegador: si el navegador pudiera pedir "méteme en
   agricultor-6", cualquiera leería los pedidos de la competencia, que es
   justo lo que se cerró al hacer privado el catálogo. */
[Authorize]
public class PedidosHub : Hub
{
    private readonly ICarritoRepositorio _carritos;
    private readonly IProductoRepositorio _productos;

    public PedidosHub(ICarritoRepositorio carritos, IProductoRepositorio productos)
    {
        _carritos = carritos;
        _productos = productos;
    }

    // Nombres de grupo en un solo sitio, para que el Hub y los
    // controladores no se desincronicen.
    public static string GrupoCliente(int clienteId) => $"cliente-{clienteId}";
    public static string GrupoAgricultor(int agricultorId) => $"agricultor-{agricultorId}";

    public override async Task OnConnectedAsync()
    {
        var id = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (int.TryParse(id, out int usuarioId))
        {
            if (Context.User!.IsInRole("Cliente"))
            {
                int? clienteId = _carritos.ObtenerClienteIdPorUsuario(usuarioId);
                if (clienteId is not null)
                    await Groups.AddToGroupAsync(Context.ConnectionId, GrupoCliente(clienteId.Value));
            }
            else if (Context.User.IsInRole("Agricultor"))
            {
                int? agricultorId = _productos.ObtenerAgricultorIdPorUsuario(usuarioId);
                if (agricultorId is not null)
                    await Groups.AddToGroupAsync(Context.ConnectionId, GrupoAgricultor(agricultorId.Value));
            }
        }

        await base.OnConnectedAsync();
    }

    /* No hace falta quitar la conexión del grupo al desconectarse:
       SignalR lo hace solo cuando la conexión muere. */
}
