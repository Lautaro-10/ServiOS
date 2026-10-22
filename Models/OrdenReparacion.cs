using System.ComponentModel.DataAnnotations;

namespace ServiOS.Models;

public class OrdenReparacion
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Debe seleccionar un equipo.")]
    [Display(Name = "Equipo")]
    public int EquipoId { get; set; }
    public Equipo? Equipo { get; set; }

    [Required(ErrorMessage = "Debe clasificar el tipo de servicio.")]
    [Display(Name = "Tipo de Servicio")]
    public int TipoServicioId { get; set; }
    public TipoServicio? TipoServicio { get; set; }

    [Display(Name = "Técnico Asignado")]
    public int? UsuarioTecnicoId { get; set; }
    public Usuario? Tecnico { get; set; }

    [Required]
    [DataType(DataType.Date)]
    [Display(Name = "Fecha de Ingreso")]
    public DateTime FechaIngreso { get; set; } = DateTime.Today;

    [DataType(DataType.Date)]
    [Display(Name = "Fecha Prometida")]
    public DateTime? FechaPrometida { get; set; }

    [DataType(DataType.Date)]
    [Display(Name = "Fecha de Entrega")]
    public DateTime? FechaEntregaReal { get; set; }

    [Required(ErrorMessage = "Debe describir la falla que reporta el cliente.")]
    [StringLength(255)]
    [Display(Name = "Falla Declarada")]
    public string FallaDeclarada { get; set; } = string.Empty;

    [Display(Name = "Diagnóstico Técnico")]
    public string? DiagnosticoTecnico { get; set; }

    [Range(0, 10000000)]
    [Display(Name = "Costo Repuestos")]
    public decimal CostoRepuestos { get; set; } = 0.00m;

    [Range(0, 10000000)]
    [Display(Name = "Costo Mano de Obra")]
    public decimal CostoManoObra { get; set; } = 0.00m;

    [Required]
    [StringLength(30)]
    public string Estado { get; set; } = "Recibido"; 

    [Display(Name = "Costo Total")]
    public decimal CostoTotal => CostoRepuestos + CostoManoObra;
}