using System.ComponentModel.DataAnnotations;

namespace ServiOS.Models;

public class Pago
{
    public int Id { get; set; }

    [Required]
    public int OrdenId { get; set; }
    public OrdenReparacion? Orden { get; set; }

    [Required]
    public int UsuarioCobroId { get; set; }
    public Usuario? UsuarioCobro { get; set; }

    [Display(Name = "Fecha de Pago")]
    public DateTime FechaPago { get; set; } = DateTime.Now;

    [Required(ErrorMessage = "El importe es obligatorio.")]
    [Range(1, 10000000, ErrorMessage = "El importe debe ser mayor a 0.")]
    public decimal Importe { get; set; }

    [Required(ErrorMessage = "El concepto es obligatorio.")]
    [StringLength(100)]
    public string Concepto { get; set; } = "Seña repuesto"; 

    public bool Anulado { get; set; } = false;
}