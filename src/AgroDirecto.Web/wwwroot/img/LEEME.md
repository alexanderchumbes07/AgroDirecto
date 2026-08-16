# Imágenes e iconos del sitio

```
wwwroot/img/
├── fotos/     fotografías
└── iconos/    iconos y logo (preferible .svg)
```

Un archivo en `wwwroot/img/fotos/hero.jpg` se sirve en `/img/fotos/hero.jpg`, sin configurar
nada ni reiniciar la aplicación. Basta con copiarlo y recargar la página con `Ctrl+F5`.

---

## Archivos que la portada ya está esperando

Mientras un archivo no exista, su hueco se ve **vacío** (un círculo verde claro): nunca
aparece el icono roto del navegador. Al copiar el archivo con el nombre exacto, se muestra solo.

### `fotos/`

| Archivo | Dónde aparece | Medida sugerida |
|---|---|---|
| `hero.jpg` | Foto grande de la portada (la canasta de verduras) | 1200 × 800 px |

### `iconos/`

| Archivo | Dónde aparece |
|---|---|
| `logo.svg` | Logo junto a "AgroDirecto", en la barra superior y en el pie |
| `hoja.svg` | Dentro del botón "Únete a AgroDirecto" |
| `fresco.svg` | Ventaja "Productos frescos" |
| `precio.svg` | Ventaja "Precios justos" |
| `apoyo.svg` | Ventaja "Apoyas al agricultor" |
| `natural.svg` | Insignia redonda "Fresco y natural" sobre la foto |
| `tractor.svg` | Tarjeta "Para el agricultor" y botón "Soy agricultor" |
| `canasta.svg` | Tarjeta "Para el cliente" y botón "Quiero comprar" |
| `manos.svg` | Tarjeta "Sin intermediarios" |
| `planta.svg` | Franja verde de llamada a la acción |
| `facebook.svg` | Redes sociales del pie |
| `instagram.svg` | Redes sociales del pie |
| `whatsapp.svg` | Redes sociales del pie |

Los nombres deben coincidir **exactamente**, incluida la extensión. Si cambias alguno, hay
que cambiarlo también en `wwwroot/css/site.css`, en el bloque "Un archivo por icono".

---

## Cómo usar una imagen en otra vista

```html
<img src="~/img/fotos/palta.jpg" alt="Paltas recién cosechadas" class="img-fluid" />
```

El `alt` no es opcional: describe la imagen para quien no puede verla, y es lo que aparece
si el archivo falla. Si el icono es solo decorativo, se marca para que los lectores de
pantalla lo ignoren:

```html
<img src="~/img/iconos/carrito.svg" alt="" aria-hidden="true" width="20" height="20" />
```

---

## Antes de agregar un archivo

**Peso.** Máximo ~300 KB por foto. Una imagen de celular pesa 4-8 MB y no debe subirse así:
el repositorio crece para siempre —git guarda cada versión— y la página tarda en cargar.
Redúcela en https://squoosh.app, que funciona en el navegador sin instalar nada.

**Medidas.** Nada por encima de 1600 px de ancho.

**Formato.** Fotos en `.jpg`. Iconos y logo en `.svg`: se ven nítidos a cualquier tamaño y
pesan muy poco. `.png` solo si hace falta fondo transparente.

**Derechos.** Este repositorio es **público**. No uses imágenes sacadas de Google: tienen
dueño y estarías redistribuyéndolas. De uso libre:
[Unsplash](https://unsplash.com) · [Pexels](https://pexels.com) · [Pixabay](https://pixabay.com).
Iconos: [Bootstrap Icons](https://icons.getbootstrap.com) · [Lucide](https://lucide.dev) ·
[Tabler Icons](https://tabler.io/icons).

**Nombres.** Minúsculas, sin tildes ni espacios: `papa-amarilla.jpg`, no `Papá Amarilla.JPG`.

---

Los `.gitkeep` existen solo para que git conserve las carpetas vacías. Se pueden borrar
cuando cada carpeta tenga contenido real.
