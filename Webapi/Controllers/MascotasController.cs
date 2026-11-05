using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Webapi.Data;
using Webapi.Models;

namespace Webapi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MascotasController(VeterinariaContext context) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<Mascota>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<Mascota>>> GetAll([FromQuery] string? especie)
    {
        var query = context.Mascotas.AsNoTracking().Include(m => m.Propietario).AsQueryable();
        if (!string.IsNullOrWhiteSpace(especie))
        {
            var texto = especie.Trim().ToLower();
            query = query.Where(m => m.Especie.ToLower().Contains(texto));
        }
        return Ok(await query.OrderBy(m => m.IdMascota).ToListAsync());
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(Mascota), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Mascota>> GetById(int id)
    {
        var mascota = await context.Mascotas.AsNoTracking().Include(m => m.Propietario)
            .FirstOrDefaultAsync(m => m.IdMascota == id);
        return mascota is null ? NotFound() : Ok(mascota);
    }

    [HttpPost]
    [ProducesResponseType(typeof(Mascota), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Mascota>> Create(Mascota mascota)
    {
        if (mascota.IdMascota != 0) return BadRequest("El ID se genera automáticamente; envíe 0 u omítalo.");
        if (!await context.Propietarios.AnyAsync(p => p.IdPropietario == mascota.IdPropietario))
            return BadRequest("El propietario indicado no existe.");
        // Solo se usa la FK; evita insertar un propietario recibido como objeto anidado.
        mascota.Propietario = null;
        context.Mascotas.Add(mascota);
        try { await context.SaveChangesAsync(); }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.ForeignKeyViolation })
        { return BadRequest("El propietario indicado no existe."); }
        return CreatedAtAction(nameof(GetById), new { id = mascota.IdMascota }, mascota);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, Mascota mascota)
    {
        if (id != mascota.IdMascota) return BadRequest("El ID de la ruta y del cuerpo deben coincidir.");
        var actual = await context.Mascotas.FindAsync(id);
        if (actual is null) return NotFound();
        if (!await context.Propietarios.AnyAsync(p => p.IdPropietario == mascota.IdPropietario))
            return BadRequest("El propietario indicado no existe.");
        actual.Nombre = mascota.Nombre;
        actual.Especie = mascota.Especie;
        actual.Raza = mascota.Raza;
        actual.Edad = mascota.Edad;
        actual.Peso = mascota.Peso;
        actual.IdPropietario = mascota.IdPropietario;
        try { await context.SaveChangesAsync(); }
        catch (DbUpdateConcurrencyException) { return NotFound(); }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.ForeignKeyViolation })
        { return BadRequest("El propietario indicado no existe."); }
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        var mascota = await context.Mascotas.FindAsync(id);
        if (mascota is null) return NotFound();
        context.Mascotas.Remove(mascota);
        try { await context.SaveChangesAsync(); }
        catch (DbUpdateConcurrencyException) { return NotFound(); }
        return NoContent();
    }
}
