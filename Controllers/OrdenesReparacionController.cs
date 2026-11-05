using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using MySqlConnector;
using ServiOS.Helpers;
using ServiOS.Models;

namespace ServiOS.Controllers;

[Authorize]
public class OrdenesReparacionController : Controller
{
    private readonly string _connectionString;
    private readonly IWebHostEnvironment _environment;

    public OrdenesReparacionController(IConfiguration configuration, IWebHostEnvironment environment)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection") ?? "";
        _environment = environment;
    }

    private void CargarSelectLists()
    {
        var equipos = new List<SelectListItem>();
        var tipos = new List<SelectListItem>();
        var tecnicos = new List<SelectListItem>();

        using (var connection = new MySqlConnection(_connectionString))
        {
            connection.Open();

            //Equipos activos con datos de su dueño
            string sqlEquipos = @"SELECT e.Id, CONCAT(e.Marca, ' ', e.Modelo, ' (IMEI: ', e.Imei, ') - Dueño: ', c.Apellido, ' ', c.Nombre) AS Info
                                  FROM Equipos e
                                  INNER JOIN Clientes c ON e.ClienteId = c.Id
                                  WHERE e.Activo = 1";
            using var cmdEquipos = new MySqlCommand(sqlEquipos, connection);
            using var rEquipos = cmdEquipos.ExecuteReader();
            while (rEquipos.Read())
            {
                equipos.Add(new SelectListItem(rEquipos["Info"].ToString(), rEquipos["Id"].ToString()));
            }
        }

        using (var connection = new MySqlConnection(_connectionString))
        {
            connection.Open();

            //Tipos de Servicio
            string sqlTipos = "SELECT Id, CONCAT(Descripcion, ' (Base: $', CostoBaseEstimado, ')') AS Info FROM TiposServicio";
            using var cmdTipos = new MySqlCommand(sqlTipos, connection);
            using var rTipos = cmdTipos.ExecuteReader();
            while (rTipos.Read())
            {
                tipos.Add(new SelectListItem(rTipos["Info"].ToString(), rTipos["Id"].ToString()));
            }
        }

        using (var connection = new MySqlConnection(_connectionString))
        {
            connection.Open();

            //Usuarios técnicos activos
            string sqlTecnicos = "SELECT Id, CONCAT(Apellido, ', ', Nombre) AS NombreCompleto FROM Usuarios WHERE Activo = 1";
            using var cmdTecnicos = new MySqlCommand(sqlTecnicos, connection);
            using var rTecnicos = cmdTecnicos.ExecuteReader();
            while (rTecnicos.Read())
            {
                tecnicos.Add(new SelectListItem(rTecnicos["NombreCompleto"].ToString(), rTecnicos["Id"].ToString()));
            }
        }

        ViewBag.Equipos = equipos;
        ViewBag.TiposServicio = tipos;
        ViewBag.Tecnicos = tecnicos;

        // Lista de estados predefinidos
        ViewBag.Estados = new List<SelectListItem>
        {
            new("Recibido", "Recibido"),
            new("En Diagnóstico", "En Diagnóstico"),
            new("Esperando Repuesto", "Esperando Repuesto"),
            new("Reparado", "Reparado"),
            new("Entregado", "Entregado"),
            new("Sin Solución", "Sin Solución")
        };
    }



public IActionResult Index(string? busqueda, string? estado, int pagina = 1, int tamanio = 5)
{
    if (pagina < 1) pagina = 1;
    if (tamanio < 1) tamanio = 5;

    int totalRegistros = 0;
    var ordenes = new List<OrdenReparacion>();

    using (var connection = new MySqlConnection(_connectionString))
    {
        connection.Open();

        string whereClause = " WHERE 1=1 ";
        if (!string.IsNullOrWhiteSpace(busqueda))
        {
            whereClause += @" AND (e.Marca LIKE @busqueda 
                                OR e.Modelo LIKE @busqueda 
                                OR e.Imei LIKE @busqueda 
                                OR c.Nombre LIKE @busqueda 
                                OR c.Apellido LIKE @busqueda 
                                OR c.Dni LIKE @busqueda) ";
        }

        if (!string.IsNullOrWhiteSpace(estado))
        {
            whereClause += " AND o.Estado = @estado ";
        }

        string sqlCount = $@"SELECT COUNT(1) 
                            FROM OrdenesReparacion o
                            INNER JOIN Equipos e ON o.EquipoId = e.Id
                            INNER JOIN Clientes c ON e.ClienteId = c.Id
                            {whereClause}";

        using (var cmdCount = new MySqlCommand(sqlCount, connection))
        {
            if (!string.IsNullOrWhiteSpace(busqueda))
                cmdCount.Parameters.AddWithValue("@busqueda", $"%{busqueda}%");
            if (!string.IsNullOrWhiteSpace(estado))
                cmdCount.Parameters.AddWithValue("@estado", estado);

            totalRegistros = Convert.ToInt32(cmdCount.ExecuteScalar());
        }
        
        int offset = (pagina - 1) * tamanio;
        string sqlDatos = $@"SELECT o.Id, o.FechaIngreso, o.FechaPrometida, o.FechaEntregaReal,
                                   o.FallaDeclarada, o.CostoRepuestos, o.CostoManoObra, o.Estado,
                                   e.Marca, e.Modelo, e.Imei,
                                   c.Nombre AS CliNombre, c.Apellido AS CliApellido,
                                   ts.Descripcion AS TipoServicioDesc,
                                   u.Nombre AS TecNombre, u.Apellido AS TecApellido
                            FROM OrdenesReparacion o
                            INNER JOIN Equipos e ON o.EquipoId = e.Id
                            INNER JOIN Clientes c ON e.ClienteId = c.Id
                            INNER JOIN TiposServicio ts ON o.TipoServicioId = ts.Id
                            LEFT JOIN Usuarios u ON o.UsuarioTecnicoId = u.Id
                            {whereClause}
                            ORDER BY o.Id DESC
                            LIMIT @tamanio OFFSET @offset";

        using (var cmdDatos = new MySqlCommand(sqlDatos, connection))
        {
            if (!string.IsNullOrWhiteSpace(busqueda))
                cmdDatos.Parameters.AddWithValue("@busqueda", $"%{busqueda}%");
            if (!string.IsNullOrWhiteSpace(estado))
                cmdDatos.Parameters.AddWithValue("@estado", estado);

            cmdDatos.Parameters.AddWithValue("@tamanio", tamanio);
            cmdDatos.Parameters.AddWithValue("@offset", offset);

            using var reader = cmdDatos.ExecuteReader();
            while (reader.Read())
            {
                ordenes.Add(new OrdenReparacion
                {
                    Id = Convert.ToInt32(reader["Id"]),
                    FechaIngreso = Convert.ToDateTime(reader["FechaIngreso"]),
                    FechaPrometida = reader["FechaPrometida"] != DBNull.Value ? Convert.ToDateTime(reader["FechaPrometida"]) : null,
                    FechaEntregaReal = reader["FechaEntregaReal"] != DBNull.Value ? Convert.ToDateTime(reader["FechaEntregaReal"]) : null,
                    FallaDeclarada = reader["FallaDeclarada"].ToString() ?? "",
                    CostoRepuestos = Convert.ToDecimal(reader["CostoRepuestos"]),
                    CostoManoObra = Convert.ToDecimal(reader["CostoManoObra"]),
                    Estado = reader["Estado"].ToString() ?? "Recibido",
                    Equipo = new Equipo
                    {
                        Marca = reader["Marca"].ToString() ?? "",
                        Modelo = reader["Modelo"].ToString() ?? "",
                        Imei = reader["Imei"].ToString() ?? "",
                        Cliente = new Cliente
                        {
                            Nombre = reader["CliNombre"].ToString() ?? "",
                            Apellido = reader["CliApellido"].ToString() ?? ""
                        }
                    },
                    TipoServicio = new TipoServicio
                    {
                        Descripcion = reader["TipoServicioDesc"].ToString() ?? ""
                    },
                    Tecnico = reader["TecNombre"] != DBNull.Value ? new Usuario
                    {
                        Nombre = reader["TecNombre"].ToString() ?? "",
                        Apellido = reader["TecApellido"].ToString() ?? ""
                    } : null
                });
            }
        }
    }

    var modeloPaginado = new PaginacionRespuesta<OrdenReparacion>
    {
        Elementos = ordenes,
        PaginaActual = pagina,
        TamanioPagina = tamanio,
        TotalRegistros = totalRegistros,
        TotalPaginas = (int)Math.Ceiling(totalRegistros / (double)tamanio),
        Busqueda = busqueda,
        EstadoFiltro = estado
    };

    return View(modeloPaginado);
}


    public IActionResult Details(int id)
    {
        OrdenReparacion? orden = null;

        using (var connection = new MySqlConnection(_connectionString))
        {
            connection.Open();
            string sql = @"SELECT o.Id, o.EquipoId, o.TipoServicioId, o.UsuarioTecnicoId,
                                  o.FechaIngreso, o.FechaPrometida, o.FechaEntregaReal,
                                  o.FallaDeclarada, o.DiagnosticoTecnico, o.CostoRepuestos, o.CostoManoObra, o.Estado,
                                  e.Marca, e.Modelo, e.Imei, e.ClaveDesbloqueo, e.ObservacionesEsteticas,
                                  c.Nombre AS CliNombre, c.Apellido AS CliApellido, c.Dni AS CliDni, c.Telefono AS CliTel, c.Email AS CliEmail,
                                  ts.Descripcion AS TipoServicioDesc,
                                  u.Nombre AS TecNombre, u.Apellido AS TecApellido
                           FROM OrdenesReparacion o
                           INNER JOIN Equipos e ON o.EquipoId = e.Id
                           INNER JOIN Clientes c ON e.ClienteId = c.Id
                           INNER JOIN TiposServicio ts ON o.TipoServicioId = ts.Id
                           LEFT JOIN Usuarios u ON o.UsuarioTecnicoId = u.Id
                           WHERE o.Id = @id";

            using var command = new MySqlCommand(sql, connection);
            command.Parameters.AddWithValue("@id", id);
            using var reader = command.ExecuteReader();

            if (reader.Read())
            {
                orden = new OrdenReparacion
                {
                    Id = Convert.ToInt32(reader["Id"]),
                    EquipoId = Convert.ToInt32(reader["EquipoId"]),
                    TipoServicioId = Convert.ToInt32(reader["TipoServicioId"]),
                    UsuarioTecnicoId = reader["UsuarioTecnicoId"] != DBNull.Value ? Convert.ToInt32(reader["UsuarioTecnicoId"]) : null,
                    FechaIngreso = Convert.ToDateTime(reader["FechaIngreso"]),
                    FechaPrometida = reader["FechaPrometida"] != DBNull.Value ? Convert.ToDateTime(reader["FechaPrometida"]) : null,
                    FechaEntregaReal = reader["FechaEntregaReal"] != DBNull.Value ? Convert.ToDateTime(reader["FechaEntregaReal"]) : null,
                    FallaDeclarada = reader["FallaDeclarada"].ToString() ?? "",
                    DiagnosticoTecnico = reader["DiagnosticoTecnico"] != DBNull.Value ? reader["DiagnosticoTecnico"].ToString() : null,
                    CostoRepuestos = Convert.ToDecimal(reader["CostoRepuestos"]),
                    CostoManoObra = Convert.ToDecimal(reader["CostoManoObra"]),
                    Estado = reader["Estado"].ToString() ?? "Recibido",
                    Equipo = new Equipo
                    {
                        Id = Convert.ToInt32(reader["EquipoId"]),
                        Marca = reader["Marca"].ToString() ?? "",
                        Modelo = reader["Modelo"].ToString() ?? "",
                        Imei = reader["Imei"].ToString() ?? "",
                        ClaveDesbloqueo = reader["ClaveDesbloqueo"] != DBNull.Value ? reader["ClaveDesbloqueo"].ToString() : null,
                        ObservacionesEsteticas = reader["ObservacionesEsteticas"] != DBNull.Value ? reader["ObservacionesEsteticas"].ToString() : null,
                        Cliente = new Cliente
                        {
                            Nombre = reader["CliNombre"].ToString() ?? "",
                            Apellido = reader["CliApellido"].ToString() ?? "",
                            Dni = reader["CliDni"].ToString() ?? "",
                            Telefono = reader["CliTel"] != DBNull.Value ? reader["CliTel"].ToString() : null,
                            Email = reader["CliEmail"] != DBNull.Value ? reader["CliEmail"].ToString() : null
                        }
                    },
                    TipoServicio = new TipoServicio
                    {
                        Id = Convert.ToInt32(reader["TipoServicioId"]),
                        Descripcion = reader["TipoServicioDesc"].ToString() ?? ""
                    },
                    Tecnico = reader["TecNombre"] != DBNull.Value ? new Usuario
                    {
                        Nombre = reader["TecNombre"].ToString() ?? "",
                        Apellido = reader["TecApellido"].ToString() ?? ""
                    } : null
                };
            }
        }

        if (orden == null)
        {
            return NotFound();
        }

        orden.Adjuntos = ObtenerAdjuntos(orden.Id);
        return View(orden);
    }


    public IActionResult Create()
    {
        CargarSelectLists();
        return View(new OrdenReparacion { FechaIngreso = DateTime.Today });
    }


    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Create(OrdenReparacion orden)
    {
        if (orden.FechaPrometida.HasValue && orden.FechaPrometida.Value < orden.FechaIngreso)
        {
            ModelState.AddModelError(nameof(OrdenReparacion.FechaPrometida), "La fecha prometida no puede ser anterior al ingreso.");
        }

        if (!ModelState.IsValid)
        {
            CargarSelectLists();
            return View(orden);
        }

        using (var connection = new MySqlConnection(_connectionString))
        {
            connection.Open();
            string sql = @"INSERT INTO OrdenesReparacion 
                           (EquipoId, TipoServicioId, UsuarioTecnicoId, FechaIngreso, FechaPrometida, FechaEntregaReal, 
                            FallaDeclarada, DiagnosticoTecnico, CostoRepuestos, CostoManoObra, Estado) 
                           VALUES (@equipoId, @tipoServicioId, @tecnicoId, @ingreso, @prometida, @entrega, 
                                   @falla, @diag, @repuestos, @manoObra, @estado)";

            using var command = new MySqlCommand(sql, connection);
            command.Parameters.AddWithValue("@equipoId", orden.EquipoId);
            command.Parameters.AddWithValue("@tipoServicioId", orden.TipoServicioId);
            command.Parameters.AddWithValue("@tecnicoId", (object?)orden.UsuarioTecnicoId ?? DBNull.Value);
            command.Parameters.AddWithValue("@ingreso", orden.FechaIngreso);
            command.Parameters.AddWithValue("@prometida", (object?)orden.FechaPrometida ?? DBNull.Value);
            command.Parameters.AddWithValue("@entrega", (object?)orden.FechaEntregaReal ?? DBNull.Value);
            command.Parameters.AddWithValue("@falla", orden.FallaDeclarada);
            command.Parameters.AddWithValue("@diag", (object?)orden.DiagnosticoTecnico ?? DBNull.Value);
            command.Parameters.AddWithValue("@repuestos", orden.CostoRepuestos);
            command.Parameters.AddWithValue("@manoObra", orden.CostoManoObra);
            command.Parameters.AddWithValue("@estado", orden.Estado);

            command.ExecuteNonQuery();
        }

        return RedirectToAction(nameof(Index));
    }


    public IActionResult Edit(int id)
    {
        OrdenReparacion? orden = ObtenerPorId(id);
        if (orden == null)
        {
            return NotFound();
        }

        CargarSelectLists();
        return View(orden);
    }


    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Edit(int id, OrdenReparacion orden)
    {
        if (id != orden.Id)
        {
            return BadRequest();
        }

        if (orden.FechaPrometida.HasValue && orden.FechaPrometida.Value < orden.FechaIngreso)
        {
            ModelState.AddModelError(nameof(OrdenReparacion.FechaPrometida), "La fecha prometida no puede ser anterior al ingreso.");
        }

        if (!ModelState.IsValid)
        {
            CargarSelectLists();
            return View(orden);
        }

        using (var connection = new MySqlConnection(_connectionString))
        {
            connection.Open();
            string sql = @"UPDATE OrdenesReparacion 
                           SET EquipoId = @equipoId, TipoServicioId = @tipoServicioId, UsuarioTecnicoId = @tecnicoId, 
                               FechaIngreso = @ingreso, FechaPrometida = @prometida, FechaEntregaReal = @entrega, 
                               FallaDeclarada = @falla, DiagnosticoTecnico = @diag, 
                               CostoRepuestos = @repuestos, CostoManoObra = @manoObra, Estado = @estado 
                           WHERE Id = @id";

            using var command = new MySqlCommand(sql, connection);
            command.Parameters.AddWithValue("@id", id);
            command.Parameters.AddWithValue("@equipoId", orden.EquipoId);
            command.Parameters.AddWithValue("@tipoServicioId", orden.TipoServicioId);
            command.Parameters.AddWithValue("@tecnicoId", (object?)orden.UsuarioTecnicoId ?? DBNull.Value);
            command.Parameters.AddWithValue("@ingreso", orden.FechaIngreso);
            command.Parameters.AddWithValue("@prometida", (object?)orden.FechaPrometida ?? DBNull.Value);
            command.Parameters.AddWithValue("@entrega", (object?)orden.FechaEntregaReal ?? DBNull.Value);
            command.Parameters.AddWithValue("@falla", orden.FallaDeclarada);
            command.Parameters.AddWithValue("@diag", (object?)orden.DiagnosticoTecnico ?? DBNull.Value);
            command.Parameters.AddWithValue("@repuestos", orden.CostoRepuestos);
            command.Parameters.AddWithValue("@manoObra", orden.CostoManoObra);
            command.Parameters.AddWithValue("@estado", orden.Estado);

            int rows = command.ExecuteNonQuery();
            if (rows == 0)
            {
                return NotFound();
            }
        }

        return RedirectToAction(nameof(Index));
    }


    [Authorize(Policy = "SoloAdmin")]
    public IActionResult Delete(int id)
    {
        var orden = ObtenerPorId(id);
        if (orden == null)
        {
            return NotFound();
        }

        return View(orden);
    }


    [HttpPost]
    [ActionName("Delete")]
    [Authorize(Policy = "SoloAdmin")]
    [ValidateAntiForgeryToken]
    public IActionResult DeleteConfirmed(int id)
    {
        using (var connection = new MySqlConnection(_connectionString))
        {
            connection.Open();

            const string referenciasSql = @"SELECT
                (SELECT COUNT(1) FROM Pagos WHERE OrdenId = @id) +
                (SELECT COUNT(1) FROM OrdenAdjuntos WHERE OrdenId = @id)";
            using (var referenciasCommand = new MySqlCommand(referenciasSql, connection))
            {
                referenciasCommand.Parameters.AddWithValue("@id", id);
                if (Convert.ToInt32(referenciasCommand.ExecuteScalar()) > 0)
                {
                    var ordenReferenciada = ObtenerPorId(id);
                    if (ordenReferenciada == null)
                    {
                        return NotFound();
                    }

                    ViewBag.Error = "No se puede eliminar la orden porque tiene pagos o adjuntos asociados.";
                    return View("Delete", ordenReferenciada);
                }
            }

            string sql = "DELETE FROM OrdenesReparacion WHERE Id = @id";

            using var command = new MySqlCommand(sql, connection);
            command.Parameters.AddWithValue("@id", id);
            command.ExecuteNonQuery();
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SubirAdjunto(int id, IFormFile? archivo, CancellationToken cancellationToken)
    {
        if (archivo == null)
        {
            TempData["Error"] = "Seleccioná una imagen para adjuntar.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var ordenExiste = ObtenerPorId(id);
        if (ordenExiste == null)
        {
            return NotFound();
        }

        string rutaArchivo;
        try
        {
            rutaArchivo = await ImageUploadHelper.SavePrivateAsync(
                archivo,
                _environment.ContentRootPath,
                "ordenes",
                cancellationToken);
        }
        catch (InvalidDataException exception)
        {
            TempData["Error"] = exception.Message;
            return RedirectToAction(nameof(Details), new { id });
        }

        try
        {
            using var connection = new MySqlConnection(_connectionString);
            connection.Open();
            const string sql = "INSERT INTO OrdenAdjuntos (OrdenId, RutaArchivo) VALUES (@ordenId, @ruta)";
            using var command = new MySqlCommand(sql, connection);
            command.Parameters.AddWithValue("@ordenId", id);
            command.Parameters.AddWithValue("@ruta", rutaArchivo);
            command.ExecuteNonQuery();
        }
        catch (MySqlException)
        {
            ImageUploadHelper.DeletePrivateFile(_environment.ContentRootPath, rutaArchivo, "ordenes");
            throw;
        }

        TempData["Mensaje"] = "La imagen se adjuntó correctamente.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [Authorize(Policy = "SoloAdmin")]
    [ValidateAntiForgeryToken]
    public IActionResult EliminarAdjunto(int id)
    {
        string? rutaArchivo;
        int ordenId;

        using (var connection = new MySqlConnection(_connectionString))
        {
            connection.Open();
            const string selectSql = "SELECT OrdenId, RutaArchivo FROM OrdenAdjuntos WHERE Id = @id";
            using var selectCommand = new MySqlCommand(selectSql, connection);
            selectCommand.Parameters.AddWithValue("@id", id);
            using var reader = selectCommand.ExecuteReader();
            if (!reader.Read())
            {
                return NotFound();
            }

            ordenId = Convert.ToInt32(reader["OrdenId"]);
            rutaArchivo = reader["RutaArchivo"].ToString();
        }

        using (var connection = new MySqlConnection(_connectionString))
        {
            connection.Open();
            const string deleteSql = "DELETE FROM OrdenAdjuntos WHERE Id = @id";
            using var command = new MySqlCommand(deleteSql, connection);
            command.Parameters.AddWithValue("@id", id);
            command.ExecuteNonQuery();
        }

        ImageUploadHelper.DeletePrivateFile(_environment.ContentRootPath, rutaArchivo, "ordenes");
        return RedirectToAction(nameof(Details), new { id = ordenId });
    }

    public IActionResult VerAdjunto(int id)
    {
        using var connection = new MySqlConnection(_connectionString);
        connection.Open();
        const string sql = "SELECT RutaArchivo FROM OrdenAdjuntos WHERE Id = @id";
        using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@id", id);
        var fileName = Convert.ToString(command.ExecuteScalar());
        if (string.IsNullOrWhiteSpace(fileName) || Path.GetFileName(fileName) != fileName)
        {
            return NotFound();
        }

        var path = Path.Combine(_environment.ContentRootPath, "App_Data", "uploads", "ordenes", fileName);
        if (!System.IO.File.Exists(path))
        {
            return NotFound();
        }

        var contentType = Path.GetExtension(fileName).ToLowerInvariant() switch
        {
            ".jpg" => "image/jpeg",
            ".png" => "image/png",
            ".webp" => "image/webp",
            _ => "application/octet-stream"
        };
        return PhysicalFile(path, contentType);
    }

    private List<OrdenAdjunto> ObtenerAdjuntos(int ordenId)
    {
        var adjuntos = new List<OrdenAdjunto>();
        using var connection = new MySqlConnection(_connectionString);
        connection.Open();
        const string sql = "SELECT Id, OrdenId, RutaArchivo, FechaSubida FROM OrdenAdjuntos WHERE OrdenId = @ordenId ORDER BY FechaSubida DESC";
        using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@ordenId", ordenId);
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            adjuntos.Add(new OrdenAdjunto
            {
                Id = Convert.ToInt32(reader["Id"]),
                OrdenId = Convert.ToInt32(reader["OrdenId"]),
                RutaArchivo = reader["RutaArchivo"].ToString() ?? string.Empty,
                FechaSubida = Convert.ToDateTime(reader["FechaSubida"])
            });
        }

        return adjuntos;
    }

    private OrdenReparacion? ObtenerPorId(int id)
    {
        using var connection = new MySqlConnection(_connectionString);
        connection.Open();
        string sql = "SELECT * FROM OrdenesReparacion WHERE Id = @id";

        using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@id", id);
        using var reader = command.ExecuteReader();

        if (reader.Read())
        {
            return new OrdenReparacion
            {
                Id = Convert.ToInt32(reader["Id"]),
                EquipoId = Convert.ToInt32(reader["EquipoId"]),
                TipoServicioId = Convert.ToInt32(reader["TipoServicioId"]),
                UsuarioTecnicoId = reader["UsuarioTecnicoId"] != DBNull.Value ? Convert.ToInt32(reader["UsuarioTecnicoId"]) : null,
                FechaIngreso = Convert.ToDateTime(reader["FechaIngreso"]),
                FechaPrometida = reader["FechaPrometida"] != DBNull.Value ? Convert.ToDateTime(reader["FechaPrometida"]) : null,
                FechaEntregaReal = reader["FechaEntregaReal"] != DBNull.Value ? Convert.ToDateTime(reader["FechaEntregaReal"]) : null,
                FallaDeclarada = reader["FallaDeclarada"].ToString() ?? "",
                DiagnosticoTecnico = reader["DiagnosticoTecnico"] != DBNull.Value ? reader["DiagnosticoTecnico"].ToString() : null,
                CostoRepuestos = Convert.ToDecimal(reader["CostoRepuestos"]),
                CostoManoObra = Convert.ToDecimal(reader["CostoManoObra"]),
                Estado = reader["Estado"].ToString() ?? "Recibido"
            };
        }

        return null;
    }
}