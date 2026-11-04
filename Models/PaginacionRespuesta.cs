namespace ServiOS.Models;

public class PaginacionRespuesta<T>
{
    public List<T> Elementos { get; set; } = new();
    public int PaginaActual { get; set; }
    public int TotalPaginas { get; set; }
    public int TotalRegistros { get; set; }
    public int TamanioPagina { get; set; }

    public string? Busqueda { get; set; }
    public string? EstadoFiltro { get; set; }

    public bool TienePaginaAnterior => PaginaActual > 1;
    public bool TienePaginaSiguiente => PaginaActual < TotalPaginas;
}