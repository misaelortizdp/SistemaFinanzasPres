using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SistemaFinanzasPres.Api.Datos;
using SistemaFinanzasPres.Api.Dtos.Autenticacion;
using SistemaFinanzasPres.Api.Extensiones;
using SistemaFinanzasPres.Api.Modelos;
using SistemaFinanzasPres.Api.Servicios;

namespace SistemaFinanzasPres.Api.Controladores;

[ApiController]
[Route("api/autenticacion")]
public class AutenticacionController : ControllerBase
{
    private readonly UserManager<Usuario> _gestorUsuarios;
    private readonly SignInManager<Usuario> _gestorSesion;
    private readonly IServicioJwt _servicioJwt;
    private readonly BaseDatosContexto _bd;

    public AutenticacionController(
        UserManager<Usuario> gestorUsuarios,
        SignInManager<Usuario> gestorSesion,
        IServicioJwt servicioJwt,
        BaseDatosContexto bd)
    {
        _gestorUsuarios = gestorUsuarios;
        _gestorSesion = gestorSesion;
        _servicioJwt = servicioJwt;
        _bd = bd;
    }

    [HttpPost("registro")]
    public async Task<ActionResult<RespuestaAutenticacionDto>> Registrar([FromBody] RegistroDto dto)
    {
        var existente = await _gestorUsuarios.FindByEmailAsync(dto.Email);
        if (existente != null)
            return BadRequest(new { error = "Ya existe una cuenta con ese correo." });

        var usuario = new Usuario
        {
            UserName = dto.Email,
            Email = dto.Email,
            Nombre = dto.Nombre.Trim(),
            FechaRegistro = DateTime.UtcNow,
        };

        var resultado = await _gestorUsuarios.CreateAsync(usuario, dto.Contrasena);
        if (!resultado.Succeeded)
            return BadRequest(new { errores = resultado.Errors.Select(e => e.Description) });

        await SembrarDatosIniciales(usuario.Id);

        var (token, expira) = _servicioJwt.GenerarToken(usuario);
        return Ok(new RespuestaAutenticacionDto
        {
            Token = token,
            ExpiraEn = expira,
            UsuarioId = usuario.Id,
            Nombre = usuario.Nombre,
            Email = usuario.Email!,
        });
    }

    [HttpPost("iniciar-sesion")]
    public async Task<ActionResult<RespuestaAutenticacionDto>> IniciarSesion([FromBody] InicioSesionDto dto)
    {
        var usuario = await _gestorUsuarios.FindByEmailAsync(dto.Email);
        if (usuario == null)
            return Unauthorized(new { error = "Credenciales inválidas." });

        var ok = await _gestorSesion.CheckPasswordSignInAsync(usuario, dto.Contrasena, lockoutOnFailure: false);
        if (!ok.Succeeded)
            return Unauthorized(new { error = "Credenciales inválidas." });

        var (token, expira) = _servicioJwt.GenerarToken(usuario);
        return Ok(new RespuestaAutenticacionDto
        {
            Token = token,
            ExpiraEn = expira,
            UsuarioId = usuario.Id,
            Nombre = usuario.Nombre,
            Email = usuario.Email!,
        });
    }

    [HttpGet("yo")]
    [Microsoft.AspNetCore.Authorization.Authorize]
    public async Task<ActionResult<RespuestaAutenticacionDto>> ObtenerActual()
    {
        var id = User.ObtenerId();
        var usuario = await _gestorUsuarios.FindByIdAsync(id);
        if (usuario == null) return NotFound();

        return Ok(new RespuestaAutenticacionDto
        {
            Token = string.Empty,
            ExpiraEn = DateTime.MinValue,
            UsuarioId = usuario.Id,
            Nombre = usuario.Nombre,
            Email = usuario.Email!,
        });
    }

    private async Task SembrarDatosIniciales(string usuarioId)
    {
        Categoria N(string nombre, TipoCategoria tipo, string color, string icono, int orden) =>
            new() { UsuarioId = usuarioId, Nombre = nombre, Tipo = tipo, Color = color, Icono = icono, Orden = orden };

        _bd.Categorias.AddRange(
            N("Salario",     TipoCategoria.Ingreso,    "#10B981", "💼", 1),
            N("Otros",       TipoCategoria.Ingreso,    "#3B82F6", "💰", 2),
            N("Renta",       TipoCategoria.Necesidad,  "#EF4444", "🏠", 3),
            N("Comida",      TipoCategoria.Necesidad,  "#F59E0B", "🍽", 4),
            N("Transporte",  TipoCategoria.Necesidad,  "#6366F1", "🚌", 5),
            N("Servicios",   TipoCategoria.Necesidad,  "#0EA5E9", "💡", 6),
            N("Entretenim.", TipoCategoria.Deseo,      "#EC4899", "🎬", 7),
            N("Ropa",        TipoCategoria.Deseo,      "#A855F7", "👕", 8),
            N("Ahorro",      TipoCategoria.Ahorro,     "#10B981", "🐖", 9),
            N("Inversión",   TipoCategoria.Ahorro,     "#0F766E", "📈", 10)
        );

        _bd.Cuentas.AddRange(
            new Cuenta { UsuarioId = usuarioId, Nombre = "Efectivo",         Saldo = 0, Orden = 1, Activa = true },
            new Cuenta { UsuarioId = usuarioId, Nombre = "Cuenta bancaria",  Saldo = 0, Orden = 2, Activa = true }
        );

        _bd.ConfigUsuarios.Add(new ConfigUsuario { UsuarioId = usuarioId });

        await _bd.SaveChangesAsync();
    }
}
