/* Conexión con el servidor para recibir avisos en tiempo real.

   Cada página registra lo que quiere escuchar y luego llama a iniciar():

       AgroAvisos.conexion.on("EstadoCambiado", datos => { ... });
       AgroAvisos.iniciar();

   Los avisos son un extra. Si la conexión falla, la página sigue
   funcionando igual: basta recargar para ver el dato correcto, porque
   la verdad está en la base de datos y no en la notificación. */
const AgroAvisos = (function () {

    const conexion = new signalR.HubConnectionBuilder()
        .withUrl("/hub/pedidos")
        .withAutomaticReconnect()      // si se cae el WiFi, reintenta solo
        .build();

    // Aviso flotante en la esquina, se va solo a los 6 segundos.
    function mostrar(texto, tipo) {
        let caja = document.getElementById("avisos-flotantes");
        if (!caja) {
            caja = document.createElement("div");
            caja.id = "avisos-flotantes";
            caja.className = "avisos-flotantes";
            document.body.appendChild(caja);
        }

        const aviso = document.createElement("div");
        aviso.className = "alert alert-" + (tipo || "success") + " shadow-sm mb-2";
        aviso.setAttribute("role", "status");
        aviso.textContent = texto;
        caja.appendChild(aviso);

        setTimeout(() => aviso.remove(), 6000);
    }

    function iniciar() {
        conexion.start().catch(function (e) {
            // No se le muestra al usuario: sin avisos la página funciona igual.
            console.warn("Sin avisos en tiempo real:", e.message);
        });
    }

    return { conexion: conexion, mostrar: mostrar, iniciar: iniciar };
})();
