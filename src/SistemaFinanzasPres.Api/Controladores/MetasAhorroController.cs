using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaFinanzasPres.Api.Datos;
using SistemaFinanzasPres.Api.Dtos;
using SistemaFinanzasPres.Api.Extensiones;
using SistemaFinanzasPres.Api.Modelos;

namespace SistemaFinanzasPres.Api.Controladores;

[ApiController, Authorize, Route("api/metas-ahorro")]
public class MetasAhorroController : ControllerBase
{
    private readonly BaseDatosContexto _bd;
    public MetasAhorroController(BaseDatosContexto bd) => _bd = bd;

    [HttpGet]
    public async Task<ActionResult<IEnumerable<MetaAhorroDto>>> Listar()
    {
        var uid = User.ObtenerId();
        var datos = await _bd.MetasAhorro.AsNoTracking()
            .Where(m => m.UsuarioId == uid)
            .OrderByDescending(m => m.Activa).ThenBy(m => m.Orden).ThenBy(m => m.Id)
            .Select(m => new MetaAhorroDto
            {
                Id = m.Id, Nombre = m.Nombre, Prioridad = m.Prioridad,
                Objetivo = m.Objetivo, Acumulado = m.Acumulado,
                FechaLimite = m.FechaLimite, AporteMensualPlaneado = m.AporteMensualPlaneado,
                Activa = m.Activa, Notas = m.Notas, Orden = m.Orden,
            }).ToListAsync();
        return Ok(datos);
    }

    [HttpPost]
    public async Task<ActionResult<MetaAhorroDto>> Crear([FromBody] MetaAhorroDto dto)
    {
        var uid = User.ObtenerId();
        var m = new MetaAhorro
        {
            UsuarioId = uid,
            Nombre = dto.Nombre.Trim(),
            Prioridad = string.IsNullOrWhiteSpace(dto.Prioridad) ? "MEDIA" : dto.Prioridad,
            Objetivo = dto.Objetivo,
            Acumulado = Math.Max(0, dto.Acumulado),
            FechaLimite = dto.FechaLimite,
            AporteMensualPlaneado = Math.Max(0, dto.AporteMensualPlaneado),
            Activa = dto.Activa,
            Notas = string.IsNullOrWhiteSpace(dto.Notas) ? null : dto.Notas.Trim(),
            Orden = dto.Orden,
        };
        _bd.MetasAhorro.Add(m);
        await _bd.SaveChangesAsync();
        dto.Id = m.Id;
        return Ok(dto);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Actualizar(int id, [FromBody] MetaAhorroDto dto)
    {
        var uid = User.ObtenerId();
        var m = await _bd.MetasAhorro.FirstOrDefaultAsync(x => x.Id == id && x.UsuarioId == uid);
        if (m == null) return NotFound();
        m.Nombre = dto.Nombre.Trim();
        m.Prioridad = dto.Prioridad;
        m.Objetivo = dto.Objetivo;
        m.Acumulado = Math.Max(0, dto.Acumulado);
        m.FechaLimite = dto.FechaLimite;
        m.AporteMensualPlaneado = Math.Max(0, dto.AporteMensualPlaneado);
        m.Activa = dto.Activa;
        m.Notas = string.IsNullOrWhiteSpace(dto.Notas) ? null : dto.Notas.Trim();
        m.Orden = dto.Orden;
        await _bd.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Eliminar(int id)
    {
        var uid = User.ObtenerId();
        var m = await _bd.MetasAhorro.FirstOrDefaultAsync(x => x.Id == id && x.UsuarioId == uid);
        if (m == null) return NotFound();
        _bd.MetasAhorro.Remove(m);
        await _bd.SaveChangesAsync();
        return NoContent();
    }

    [HttpPost("{id:int}/aporte")]
    public async Task<IActionResult> RegistrarAporte(int id, [FromBody] AporteMetaDto dto)
    {
        var uid = User.ObtenerId();
        var m = await _bd.MetasAhorro.FirstOrDefaultAsync(x => x.Id == id && x.UsuarioId == uid);
        if (m == null) return NotFound();
        m.Acumulado += dto.Monto;
        await _bd.SaveChangesAsync();
        return Ok(new { acumulado = m.Acumulado });
    }
}
