using System.ComponentModel.DataAnnotations;

namespace Webapi.Models;

public class Mascota
{
    [Key]
    public int IdMascota { get; set; }
    [Required, StringLength(50)]
    public string Nombre { get; set; } = string.Empty;
    [Required, StringLength(30)]
    public string Especie { get; set; } = string.Empty;
    [Required, StringLength(50)]
    public string Raza { get; set; } = string.Empty;
    [Required, Range(0, 100)]
    public int Edad { get; set; }
    [Required, Range(typeof(decimal), "0.01", "999.99")]
    public decimal Peso { get; set; }
    [Required, Range(1, int.MaxValue)]
    public int IdPropietario { get; set; }
    public Propietario? Propietario { get; set; }
}
