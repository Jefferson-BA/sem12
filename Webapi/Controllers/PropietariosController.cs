using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Webapi.Data;
using Webapi.Models;

namespace Webapi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PropietariosController(VeterinariaContext context) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<Propietario>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<Propietario>>> GetAll([FromQuery] string? buscar)
    {
        var query = context.Propietarios.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(buscar))
        {
            var texto = buscar.Trim().ToLower();
            query = query.Where(p => p.Nombre.ToLower().Contains(texto) ||
                p.Dni.Contains(texto) || p.Email.ToLower().Contains(texto));
        }
        return Ok(await query.OrderBy(p => p.IdPropietario).ToListAsync());
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(Propietario), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Propietario>> GetById(int id)
    {
        var propietario = await context.Propietarios.AsNoTracking()
            .FirstOrDefaultAsync(p => p.IdPropietario == id);
        return propietario is null ? NotFound() : Ok(propietario);
    }

    [HttpGet("{id:int}/mascotas")]
    [ProducesResponseType(typeof(IEnumerable<Mascota>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<Mascota>>> GetMascotas(int id)
    {
        if (!await context.Propietarios.AnyAsync(p => p.IdPropietario == id)) return NotFound();
        return Ok(await context.Mascotas.AsNoTracking().Where(m => m.IdPropietario == id)
            .OrderBy(m => m.IdMascota).ToListAsync());
    }

    [HttpPost]
    [ProducesResponseType(typeof(Propietario), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<Propietario>> Create(Propietario propietario)
    {
        if (propietario.IdPropietario != 0) return BadRequest("El ID se genera automáticamente; envíe 0 u omítalo.");
        Normalizar(propietario);
        if (await Duplicado(propietario)) return Conflict("Ya existe un propietario con ese DNI o email.");
        propietario.Mascotas = new List<Mascota>();
        context.Propietarios.Add(propietario);
        try { await context.SaveChangesAsync(); }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        { return Conflict("Ya existe un propietario con ese DNI o email."); }
        return CreatedAtAction(nameof(GetById), new { id = propietario.IdPropietario }, propietario);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(int id, Propietario propietario)
    {
        if (id != propietario.IdPropietario) return BadRequest("El ID de la ruta y del cuerpo deben coincidir.");
        var actual = await context.Propietarios.FindAsync(id);
        if (actual is null) return NotFound();
        Normalizar(propietario);
        if (await Duplicado(propietario)) return Conflict("Ya existe un propietario con ese DNI o email.");
        actual.Nombre = propietario.Nombre;
        actual.Dni = propietario.Dni;
        actual.Telefono = propietario.Telefono;
        actual.Email = propietario.Email;
        try { await context.SaveChangesAsync(); }
        catch (DbUpdateConcurrencyException) { return NotFound(); }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        { return Conflict("Ya existe un propietario con ese DNI o email."); }
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(int id)
    {
        var propietario = await context.Propietarios.FindAsync(id);
        if (propietario is null) return NotFound();
        if (await context.Mascotas.AnyAsync(m => m.IdPropietario == id))
            return Conflict("No se puede eliminar un propietario con mascotas vinculadas.");
        context.Propietarios.Remove(propietario);
        try { await context.SaveChangesAsync(); }
        catch (DbUpdateConcurrencyException) { return NotFound(); }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.ForeignKeyViolation })
        { return Conflict("No se puede eliminar un propietario con mascotas vinculadas."); }
        return NoContent();
    }

    private Task<bool> Duplicado(Propietario p) => context.Propietarios.AnyAsync(x =>
        x.IdPropietario != p.IdPropietario && (x.Dni == p.Dni || x.Email.ToLower() == p.Email));

    private static void Normalizar(Propietario p)
    {
        p.Nombre = p.Nombre.Trim();
        p.Dni = p.Dni.Trim();
        p.Telefono = p.Telefono.Trim();
        p.Email = p.Email.Trim().ToLowerInvariant();
    }
}
