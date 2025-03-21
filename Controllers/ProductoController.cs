using IronMongeryTools.Data;
using IronMongeryTools.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Text.RegularExpressions;
using IronMongeryTools.Services;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Text.Json; 

namespace IronMongeryTools.Controllers
{
    public class ProductoController : Controller
    {
        // GET: ProductoController
        private readonly IMemoryCache MemoriaCache;

        private readonly IAccesoService _accesoService;
        private readonly IEmailService emailService;

        public ProductoController(IMemoryCache cache, IAccesoService accesoService, IEmailService emailService)
        {
            MemoriaCache = cache;
            _accesoService = accesoService;
            this.emailService = emailService;
        }

        private List<Productos> ObtenerProductos()
        {
            List<Productos> EfectoProducto;

            if (EstaVacioElCache())
            {
                // Cargar productos desde la base de datos
                EfectoProducto = CargarProductoDesdeBaseDeDatos();

                // Almacenar la lista cargada en el cache
                MemoriaCache.Set("ListaProducto", EfectoProducto);
            }
            else
            {
                // Recuperar los productos desde el cache
                EfectoProducto = (List<Productos>)MemoriaCache.Get("ListaProducto");
            }

            return EfectoProducto;
        }

        private List<Productos> CargarProductoDesdeBaseDeDatos()
        {
            var listaProducto = new List<Productos>();

            using (SqlConnection oConexion = new SqlConnection(Conexion.rutaConexion))
            {
                SqlCommand cmd = new SqlCommand("usp_Productos_Obtener", oConexion)
                {
                    CommandType = CommandType.StoredProcedure
                };

                try
                {
                    oConexion.Open();

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            listaProducto.Add(new Productos
                            {
                                ProductoID = reader["ProductoID"]?.ToString(),
                                Nombre = reader["Nombre"]?.ToString(),
                                CategoriaID = reader["CategoriaID"]?.ToString(),
                                Precio = (double)(reader["Precio"]?.GetType() == typeof(decimal) ? (decimal)reader["Precio"] : 0),
                                Stock = reader["Stock"]?.GetType() == typeof(int) ? (int)reader["Stock"] : 0,
                                ProveedorID = reader["ProveedorID"]?.ToString()
                            });
                        }
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception("Error al cargar los productos desde la base de datos", ex);
                }
            }

            return listaProducto;
        }

        private Productos ObtenerProductos(string id)
        {

            List<Productos> laLista;
            laLista = ObtenerProductos();

            foreach (Productos productos in laLista)
            {
                if (productos.ProductoID == id)
                    return productos;
            }

            return null;

        }
        private bool EstaVacioElCache()
        {
            if (MemoriaCache.Get("ListaProducto") is null)
                return true;
            else
                return false;
        }

        // GET: ProductoController
        public ActionResult Index()
        {
            List<Productos> laLista = ObtenerProductos();
            int totalproductos = laLista.Count;
            ViewData["TotalProductos"] = totalproductos;

            var proveedorController = new ProveedorController(MemoriaCache, _accesoService, emailService);
            List<Proveedores> listaProveedores = proveedorController.CargarProveedorDesdeBaseDeDatos();

            // Crear lista de proveedores para el dropdown
            ViewBag.ProveedorList = listaProveedores.Select(p => new SelectListItem
            {
                Value = p.ProveedorID,
                Text = $"{p.ProveedorID} - {p.Nombre}"
            }).ToList();

            // Cargar todas las categorías en un diccionario
            Dictionary<int, string> categoriasDict = CargarCategoriasDesdeBaseDeDatos();

            // Asignar el nombre de la categoría a cada producto
            foreach (var producto in laLista)
            {
                if (int.TryParse(producto.CategoriaID, out int categoriaID) && categoriasDict.ContainsKey(categoriaID))
                {
                    producto.CategoriaNombre = categoriasDict[categoriaID];
                }
                else
                {
                    producto.CategoriaNombre = "Sin categoría"; // Manejar productos sin categoría
                }
            }

            return View(laLista);
        }

        public Dictionary<int, string> CargarCategoriasDesdeBaseDeDatos()
        {
            var categorias = new Dictionary<int, string>();

            using (SqlConnection conn = new SqlConnection(Conexion.rutaConexion))
            {
                conn.Open();
                string query = "SELECT CategoriaID, Nombre FROM Categorias";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            int categoriaID = Convert.ToInt32(reader["CategoriaID"]);
                            string nombre = reader["Nombre"].ToString();
                            categorias[categoriaID] = nombre;
                        }
                    }
                }
            }

            return categorias;
        }

        // GET: ProductosController/Details/
        public ActionResult Details(string id)
        {
            Productos elproducto;
            elproducto = ObtenerProductos(id);
            return View(elproducto);
        }

        public ActionResult Create()
        {
            var proveedorController = new ProveedorController(MemoriaCache, _accesoService, emailService);
            List<Proveedores> listaProveedores = proveedorController.CargarProveedorDesdeBaseDeDatos();

            // Lista de proveedores
            ViewBag.ProveedorList = listaProveedores.Select(p => new SelectListItem
            {
                Value = p.ProveedorID,
                Text = $"{p.ProveedorID} - {p.Nombre}"
            }).ToList();

            // Diccionario para agrupar las categorías por proveedor
            var categoriasPorProveedor = listaProveedores.ToDictionary(
                p => p.ProveedorID,
                p => p.Categorias.Select(c => new { c.CategoriaID, c.Nombre }).ToList()
            );

            // Convertir a JSON con System.Text.Json
            ViewBag.CategoriasPorProveedorJson = JsonSerializer.Serialize(categoriasPorProveedor);

            return View();
        }
        public static bool ProbarConexion(out string mensaje)
        {
            mensaje = string.Empty;
            try
            {
                using (SqlConnection oConexion = new SqlConnection(Conexion.rutaConexion))
                {
                    oConexion.Open();
                    mensaje = "Conexión exitosa a la base de datos.";
                    return true;
                }
            }
            catch (Exception ex)
            {
                mensaje = $"Error al conectar a la base de datos: {ex.Message}";
                return false;
            }
        }
        private bool Registrar(Productos productos, out string mensaje)
        {
            mensaje = string.Empty;
            try
            {
                using (SqlConnection oConexion = new SqlConnection(Conexion.rutaConexion))
                {
                    oConexion.Open();

                    string query = @"
                INSERT INTO Productos (ProductoID, Nombre, CategoriaID, Precio, Stock, 
                                      ProveedorID)

                VALUES (@ProductoID, @Nombre, @CategoriaID, @Precio, @Stock, @ProveedorID)";

                    using (SqlCommand cmd = new SqlCommand(query, oConexion))
                    {
                        cmd.Parameters.AddWithValue("@ProductoID", productos.ProductoID);
                        cmd.Parameters.AddWithValue("@Nombre", productos.Nombre);
                        cmd.Parameters.AddWithValue("@CategoriaID", productos.CategoriaID);
                        cmd.Parameters.AddWithValue("@Precio", productos.Precio);
                        cmd.Parameters.AddWithValue("@Stock", productos.Stock);
                        cmd.Parameters.AddWithValue("@ProveedorID", productos.ProveedorID);
                        int filasAfectadas = cmd.ExecuteNonQuery();

                        return filasAfectadas > 0;
                    }

                }
            }
            catch (SqlException ex)
            {
                mensaje = $"Error al registrar el producto: {ex.Message}";
                return false;
            }
        }


        // POST: ProductoController/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create(Productos productos)
        {
            try
            {
                List<Productos> laLista;
                laLista = ObtenerProductos();
                laLista.Add(productos);
                string mensaje;
                bool r = ProbarConexion(out mensaje);
                bool conexionValida = ProbarConexion(out mensaje);
                if (!conexionValida)
                {
                    ModelState.AddModelError("", mensaje);
                    return View(productos);
                }
                bool registroExitoso = Registrar(productos, out mensaje);

                if (!registroExitoso)
                {
                    ModelState.AddModelError(string.Empty, mensaje);
                    return View(productos);
                }
                else
                {
                    return RedirectToAction(nameof(Index));
                }

            }
            catch
            {
                return View();
            }
        }

        private bool IsValidEmail(string email)
        {
            var emailPattern = new Regex(@"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$");
            return emailPattern.IsMatch(email);
        }

        // GET: ProductoController/Edit/5
        public ActionResult Edit(string id)
        {
            Productos elproducto;
            elproducto = ObtenerProductos(id);
            var proveedorController = new ProveedorController(MemoriaCache, _accesoService, emailService);
            List<Proveedores> listaProveedores = proveedorController.CargarProveedorDesdeBaseDeDatos(); // Implementa este método para devolver los proveedores.

            // Crear una lista de SelectListItem para usar en el dropdown
            ViewBag.ProveedorList = listaProveedores.Select(p => new SelectListItem
            {
                Value = p.ProveedorID,
                Text = $"{p.ProveedorID} - {p.Nombre}"
            }).ToList();

            // Diccionario para agrupar las categorías por proveedor
            var categoriasPorProveedor = listaProveedores.ToDictionary(
                p => p.ProveedorID,
                p => p.Categorias.Select(c => new { c.CategoriaID, c.Nombre }).ToList()
            );

            // Convertir a JSON con System.Text.Json
            ViewBag.CategoriasPorProveedorJson = JsonSerializer.Serialize(categoriasPorProveedor);

            return View(elproducto);
        }

        // POST: ProductoController/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(string id, Productos Productos)
        {
            try
            {
                var elproducto = ObtenerProductos(id); // Obtener administrador
                if (elproducto == null)
                {
                    return NotFound(); // Retorna un error 404 si no se encuentra el administrador
                }

                // Actualizar datos
                elproducto.ProductoID = Productos.ProductoID;
                elproducto.Nombre = Productos.Nombre;
                elproducto.CategoriaID = Productos.CategoriaID;
                elproducto.Precio = Productos.Precio;
                elproducto.Stock = Productos.Stock;
                elproducto.ProveedorID = Productos.ProveedorID;

                // Aquí debes guardar los cambios en la base de datos
                bool actualizado = ActualizarProductoEnBaseDeDatos(elproducto);
                if (actualizado)
                {
                    return RedirectToAction(nameof(Index)); // Redirige a la lista de productos
                }
                else
                {
                    ModelState.AddModelError("", "Error al actualizar el proveedor.");
                    return View(Productos); // Devuelve la vista si no se puede actualizar
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Error inesperado: " + ex.Message);
                return View(Productos); // Devuelve la vista si hay un error
            }
        }

        private bool ActualizarProductoEnBaseDeDatos(Productos productos)
        {
            using (SqlConnection oConexion = new SqlConnection(Conexion.rutaConexion))
            {
                SqlCommand cmd = new SqlCommand("usp_Productos_Modificar", oConexion)
                {
                    CommandType = CommandType.StoredProcedure
                };

                // Aquí pasas todos los parámetros necesarios
                cmd.Parameters.AddWithValue("@productoid", productos.ProductoID);
                cmd.Parameters.AddWithValue("@nombre", productos.Nombre);
                cmd.Parameters.AddWithValue("@categoriaid", productos.CategoriaID);
                cmd.Parameters.AddWithValue("@precio", productos.Precio);
                cmd.Parameters.AddWithValue("@stock", productos.Stock);
                cmd.Parameters.AddWithValue("@proveedorid", productos.ProveedorID);
                try
                {
                    oConexion.Open();
                    int filasAfectadas = cmd.ExecuteNonQuery();
                    return filasAfectadas > 0; // Devuelve true si se actualizó al menos una fila
                }
                catch (Exception ex)
                {
                    // Aquí podrías loguear el error
                    return false; // Retorna false si hubo un error
                }
            }
        }

        // GET: ProductosController/Delete/5
        public ActionResult Delete(string id)
        {
            Productos elproducto;
            elproducto = ObtenerProductos(id);
            return View(elproducto);
        }

        // POST: ProductosController/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(string id, IFormCollection collection, Productos cliente)
        {
            try
            {
                List<Productos> laLista;
                Productos elproducto;
                elproducto = ObtenerProductos(id);

                if (elproducto == null)
                {
                    ModelState.AddModelError("", "El producto no fue encontrado.");
                    return View();
                }

                // Asignar la categoría del producto a ViewBag antes de eliminarlo
                ViewBag.Categoria = elproducto.CategoriaID;

                laLista = ObtenerProductos();
                laLista.Remove(elproducto);

                // Eliminar el producto en la base de datos
                EliminarProductoEnBaseDeDatos(id);

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Error al eliminar el producto: " + ex.Message);

                // Mantener la categoría en caso de error
                ViewBag.Categoria = cliente?.CategoriaID;

                return View(cliente);
            }
        }

        // Método para eliminar un producto de la base de datos usando el procedimiento almacenado
        private void EliminarProductoEnBaseDeDatos(string id)
        {
            using (SqlConnection conexion = new SqlConnection(Conexion.rutaConexion))
            {
                using (SqlCommand cmd = new SqlCommand("usp_Productos_Eliminar", conexion))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    // Agregar parámetro al procedimiento
                    cmd.Parameters.AddWithValue("@ProductoID", id);

                    conexion.Open();
                    cmd.ExecuteNonQuery();
                }
            }
        }

    }
}