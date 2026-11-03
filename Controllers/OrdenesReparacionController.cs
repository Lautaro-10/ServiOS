using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using MySqlConnector;
using ServiOS.Models;

namespace ServiOS.Controllers;

public class OrdenesReparacionController : Controller
{
    private readonly string _connectionString;

    public OrdenesReparacionController(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection") ?? "";
    }

    private void CargarSelectLists()
    {
        var equipos = new List<SelectListItem>();
        var tipos = new List<SelectListItem>();
        var tecnicos = new List<SelectListItem>();

        using (var connection = new MySqlConnection(_connectionString))
        {
            connection.Open();

            // 1. Equipos activos con datos de su dueño
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

            // 2. Tipos de Servicio
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

            // 3. Usuarios técnicos activos
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

    // GET: OrdenesReparacion
    public IActionResult Index()
    {
        var ordenes = new List<OrdenReparacion>();

        using (var connection = new MySqlConnection(_connectionString))
        {
            connection.Open();
            string sql = @"SELECT o.Id, o.FechaIngreso, o.FechaPrometida, o.FechaEntregaReal,
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
                           ORDER BY o.FechaIngreso DESC";

            using var command = new MySqlCommand(sql, connection);
            using var reader = command.ExecuteReader();
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

        return View(ordenes);
    }

    // GET: OrdenesReparacion/Details/5
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

        return View(orden);
    }

    // GET: OrdenesReparacion/Create
    public IActionResult Create()
    {
        CargarSelectLists();
        return View(new OrdenReparacion { FechaIngreso = DateTime.Today });
    }

    // POST: OrdenesReparacion/Create
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

    // GET: OrdenesReparacion/Edit/5
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

    // POST: OrdenesReparacion/Edit/5
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

    // GET: OrdenesReparacion/Delete/5
    public IActionResult Delete(int id)
    {
        return Details(id);
    }

    // POST: OrdenesReparacion/Delete/5
    [HttpPost]
    [ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public IActionResult DeleteConfirmed(int id)
    {
        using (var connection = new MySqlConnection(_connectionString))
        {
            connection.Open();
            string sql = "DELETE FROM OrdenesReparacion WHERE Id = @id";

            using var command = new MySqlCommand(sql, connection);
            command.Parameters.AddWithValue("@id", id);
            command.ExecuteNonQuery();
        }

        return RedirectToAction(nameof(Index));
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