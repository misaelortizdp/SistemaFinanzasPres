using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaFinanzasPres.Api.Datos;
using SistemaFinanzasPres.Api.Dtos;
using SistemaFinanzasPres.Api.Extensiones;
using SistemaFinanzasPres.Api.Modelos;

namespace SistemaFinanzasPres.Api.Controladores;

[ApiController, Authorize, Route("api/patrimonio")]
public class PatrimonioController : ControllerBase
{
    private readonly BaseDatosContexto _bd;
    public PatrimonioController(BaseDatosContexto bd) => _bd = bd;

    [HttpGet("actual")]
    public async Task<ActionResult<PatrimonioActualDto>> Actual()
        => Ok(await CalcularActualAsync(User.ObtenerId()));

    private async Task<PatrimonioActualDto> CalcularActualAsync(string usuarioId)
    {
        var cuentas = await _bd.Cuentas.AsNoTracking()
            .Where(c => c.UsuarioId == usuarioId && c.Activa)
            .OrderBy(c => c.Orden).ToListAsync();
        var deudas = await _bd.Deudas.AsNoTracking()
            .Where(d => d.UsuarioId == usuarioId && d.Activa)
            .OrderByDescending(d => d.SaldoActual).ToListAsync();

        var activos = cuentas.Select(c => new LineaPatrimonioDto(c.Nombre, c.Saldo)).ToList();
        var pasivos = deudas.Select(d => new LineaPatrimonioDto(d.Nombre, d.SaldoActual)).ToList();
        var ta = activos.Sum(a => a.Monto);
        var tp = pasivos.Sum(p => p.Monto);

        return new PatrimonioActualDto
        {
            Activos = activos,
            Pasivos = pasivos,
            TotalActivos = ta,
            TotalPasivos = tp,
            PatrimonioNeto = ta - tp,
        };
    }

    [HttpGet("snapshots")]
    public async Task<ActionResult<IEnumerable<SnapshotPatrimonialDto>>> ListarSnapshots()
    {
        var uid = User.ObtenerId();
        var datos = await _bd.SnapshotsPatrimoniales.AsNoTracking()
            .Where(s => s.UsuarioId == uid)
            .OrderByDescending(s => s.Fecha).ThenByDescending(s => s.Id)
            .Select(s => new SnapshotPatrimonialDto
            {
                Id = s.Id, Fecha = s.Fecha,
                Activos = s.Activos, Pasivos = s.Pasivos,
                PatrimonioNeto = s.PatrimonioNeto, Notas = s.Notas,
            }).ToListAsync();
        return Ok(datos);
    }

    [HttpPost("snapshots")]
    public async Task<ActionResult<SnapshotPatrimonialDto>> CrearSnapshot([FromBody] CrearSnapshotDto dto)
    {
        var uid = User.ObtenerId();
        var datos = await CalcularActualAsync(uid);
        var s = new SnapshotPatrimonial
        {
            UsuarioId = uid,
            Fecha = DateTime.UtcNow.Date,
            Activos = datos.TotalActivos,
            Pasivos = datos.TotalPasivos,
            PatrimonioNeto = datos.PatrimonioNeto,
            Notas = string.IsNullOrWhiteSpace(dto.Notas) ? null : dto.Notas.Trim(),
        };
        _bd.SnapshotsPatrimoniales.Add(s);
        await _bd.SaveChangesAsync();
        return Ok(new SnapshotPatrimonialDto
        {
            Id = s.Id, Fecha = s.Fecha, Activos = s.Activos,
            Pasivos = s.Pasivos, PatrimonioNeto = s.PatrimonioNeto, Notas = s.Notas,
        });
    }

    [HttpDelete("snapshots/{id:int}")]
    public async Task<IActionResult> EliminarSnapshot(int id)
    {
        var uid = User.ObtenerId();
        var s = await _bd.SnapshotsPatrimoniales.FirstOrDefaultAsync(x => x.Id == id && x.UsuarioId == uid);
        if (s == null) return NotFound();
        _bd.SnapshotsPatrimoniales.Remove(s);
        await _bd.SaveChangesAsync();
        return NoContent();
    }
}
