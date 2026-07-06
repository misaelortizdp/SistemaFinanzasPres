using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaFinanzasPres.Api.Datos;
using SistemaFinanzasPres.Api.Dtos;
using SistemaFinanzasPres.Api.Extensiones;
using SistemaFinanzasPres.Api.Modelos;

namespace SistemaFinanzasPres.Api.Controladores;

[ApiController]
[Authorize]
[Route("api/categorias")]
public class CategoriasController : ControllerBase
{
    private readonly BaseDatosContexto _bd;

    public CategoriasController(BaseDatosContexto bd) => _bd = bd;

    [HttpGet]
    public async Task<ActionResult<IEnumerable<CategoriaDto>>> Listar()
    {
        var usuarioId = User.ObtenerId();
        var datos = await _bd.Categorias
            .AsNoTracking()
            .Where(c => c.UsuarioId == usuarioId)
            .OrderBy(c => c.Orden).ThenBy(c => c.Nombre)
            .Select(c => new CategoriaDto
            {
                Id = c.Id,
                Nombre = c.Nombre,
                Tipo = c.Tipo,
                Color = c.Color,
                Icono = c.Icono,
                Orden = c.Orden,
                Activa = c.Activa,
            })
            .ToListAsync();
        return Ok(datos);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<CategoriaDto>> Obtener(int id)
    {
        var usuarioId = User.ObtenerId();
        var c = await _bd.Categorias.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id && x.UsuarioId == usuarioId);
        if (c == null) return NotFound();
        return Ok(new CategoriaDto
        {
            Id = c.Id, Nombre = c.Nombre, Tipo = c.Tipo,
            Color = c.Color, Icono = c.Icono, Orden = c.Orden, Activa = c.Activa,
        });
    }

    [HttpPost]
    public async Task<ActionResult<CategoriaDto>> Crear([FromBody] CategoriaDto dto)
    {
        var usuarioId = User.ObtenerId();
        var categoria = new Categoria
        {
            UsuarioId = usuarioId,
            Nombre = dto.Nombre.Trim(),
            Tipo = dto.Tipo,
            Color = dto.Color,
            Icono = dto.Icono,
            Orden = dto.Orden,
            Activa = dto.Activa,
        };
        _bd.Categorias.Add(categoria);
        await _bd.SaveChangesAsync();
        dto.Id = categoria.Id;
        return CreatedAtAction(nameof(Obtener), new { id = categoria.Id }, dto);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Actualizar(int id, [FromBody] CategoriaDto dto)
    {
        var usuarioId = User.ObtenerId();
        var c = await _bd.Categorias.FirstOrDefaultAsync(x => x.Id == id && x.UsuarioId == usuarioId);
        if (c == null) return NotFound();

        c.Nombre = dto.Nombre.Trim();
        c.Tipo = dto.Tipo;
        c.Color = dto.Color;
        c.Icono = dto.Icono;
        c.Orden = dto.Orden;
        c.Activa = dto.Activa;
        await _bd.SaveChangesAsync();
        return NoContent();
    }

    [HttpPatch("{id:int}/toggle-activa")]
    public async Task<IActionResult> ToggleActiva(int id)
    {
        var usuarioId = User.ObtenerId();
        var c = await _bd.Categorias.FirstOrDefaultAsync(x => x.Id == id && x.UsuarioId == usuarioId);
        if (c == null) return NotFound();
        c.Activa = !c.Activa;
        await _bd.SaveChangesAsync();
        return Ok(new { activa = c.Activa });
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Eliminar(int id)
    {
        var usuarioId = User.ObtenerId();
        var c = await _bd.Categorias.FirstOrDefaultAsync(x => x.Id == id && x.UsuarioId == usuarioId);
        if (c == null) return NotFound();

        var tieneMovimientos = await _bd.Movimientos.AnyAsync(m => m.CategoriaId == id);
        if (tieneMovimientos)
            return Conflict(new { error = "No se puede eliminar: la categoría tiene movimientos registrados. Puedes desactivarla en su lugar." });

        _bd.Categorias.Remove(c);
        await _bd.SaveChangesAsync();
        return NoContent();
    }
}
