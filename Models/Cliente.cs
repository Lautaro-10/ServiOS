using System.ComponentModel.DataAnnotations;

namespace ServiOS.Models;

public class Cliente
{
    public int Id { get; set; }

    [Required(ErrorMessage = "El DNI es obligatorio.")]
    [StringLength(15, ErrorMessage = "El DNI no puede superar los 15 caracteres.")]
    public string Dni { get; set; } = string.Empty;

    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(50)]
    public string Nombre { get; set; } = string.Empty;

    [Required(ErrorMessage = "El apellido es obligatorio.")]
    [StringLength(50)]
    public string Apellido { get; set; } = string.Empty;

    [Phone(ErrorMessage = "Formato de teléfono inválido.")]
    [StringLength(50)]
    public string? Telefono { get; set; }

    [EmailAddress(ErrorMessage = "Formato de email inválido.")]
    [StringLength(100)]
    public string? Email { get; set; }

    [Display(Name = "Fecha de Alta")]
    public DateTime FechaAlta { get; set; } = DateTime.Now;

    public string NombreCompleto => $"{Apellido}, {Nombre}";
}