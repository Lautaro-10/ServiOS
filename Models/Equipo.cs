using System.ComponentModel.DataAnnotations;

namespace ServiOS.Models;

public class Equipo
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Debe asignar un cliente.")]
    [Display(Name = "Cliente")]
    public int ClienteId { get; set; }
    public Cliente? Cliente { get; set; }

    [Required(ErrorMessage = "La marca es obligatoria.")]
    [StringLength(50)]
    public string Marca { get; set; } = string.Empty;

    [Required(ErrorMessage = "El modelo es obligatorio.")]
    [StringLength(50)]
    public string Modelo { get; set; } = string.Empty;

    [Required(ErrorMessage = "El IMEI o número de serie es obligatorio.")]
    [StringLength(20)]
    [Display(Name = "IMEI / Nro. Serie")]
    public string Imei { get; set; } = string.Empty;

    [StringLength(50)]
    [Display(Name = "PIN / Patrón de desbloqueo")]
    public string? ClaveDesbloqueo { get; set; }

    [StringLength(255)]
    [Display(Name = "Detalles estéticos al ingresar")]
    public string? ObservacionesEsteticas { get; set; }

    public bool Activo { get; set; } = true;

    public string DatosEquipo => $"{Marca} {Modelo} (IMEI: {Imei})";
}