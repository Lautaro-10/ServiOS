namespace ServiOS.Models;

public class OrdenAdjunto
{
    public int Id { get; set; }
    public int OrdenId { get; set; }
    public string RutaArchivo { get; set; } = string.Empty;
    public DateTime FechaSubida { get; set; }
}
