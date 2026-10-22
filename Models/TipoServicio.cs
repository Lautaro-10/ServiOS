using System.ComponentModel.DataAnnotations;

namespace ServiOS.Models;

public class TipoServicio
{
    public int Id { get; set; }

    [Required(ErrorMessage = "La descripción del servicio es obligatoria.")]
    [StringLength(100)]
    public string Descripcion { get; set; } = string.Empty;

    [Required]
    [Range(0, 10000000, ErrorMessage = "El costo debe ser mayor o igual a 0.")]
    [Display(Name = "Costo Base Estimado")]
    public decimal CostoBaseEstimado { get; set; } = 0.00m;
}