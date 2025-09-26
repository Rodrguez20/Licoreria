using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using SistemaVenta.AplicacionWeb.Models.ViewModels;
using SistemaVenta.AplicacionWeb.Utilidades.Response;
using SistemaVenta.BLL.Interfaces;

namespace SistemaVenta.AplicacionWeb.Controllers
{
    [Authorize]
    public class DashBoardController : Controller
    {
        private readonly IDashBoardService _dashBoarServicio;

        public DashBoardController(IDashBoardService dashBoarServicio)
        {
            _dashBoarServicio = dashBoarServicio;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> ObtenerResumen()
        {
            GenericRespose<VMDashBoard> gResponse = new GenericRespose<VMDashBoard>();
            try
            {
                VMDashBoard vmDashBoard = new VMDashBoard();

                vmDashBoard.TotalVentas = await _dashBoarServicio.TotalVentasUltimaSemana();
                vmDashBoard.TotalIngresos = await _dashBoarServicio.TotalIngresosUltimaSemana();
                vmDashBoard.TotalProductos = await _dashBoarServicio.TotalProductos();
                vmDashBoard.TotalCategorias = await _dashBoarServicio.TotalCategorias();

                List<VMVentasSemana> listaVentasSemana = new List<VMVentasSemana>();
                List<VMProductosSemana> listaProductosSemana = new List<VMProductosSemana>();

                foreach(KeyValuePair<string, int> intem in await _dashBoarServicio.VentasUltimaSemana())
                {
                    listaVentasSemana.Add(new VMVentasSemana()
                    {
                        Fecha = intem.Key,
                        Total = intem.Value
                    });
                }

                foreach (KeyValuePair<string, int> intem in await _dashBoarServicio.ProductosTopUltimaSemana())
                {
                    listaProductosSemana.Add(new VMProductosSemana()
                    {
                        Producto = intem.Key,
                        Cantidad = intem.Value
                    });
                }
                vmDashBoard.VentasUltimaSemana = listaVentasSemana;
                vmDashBoard.ProductosTopUltimaSemana = listaProductosSemana;

                gResponse.Estado = true;
                gResponse.Objeto = vmDashBoard;

            }
            catch (Exception ex) 
            {
                gResponse.Estado = false;
                gResponse.Mensaje = ex.Message;
            }
            return StatusCode(StatusCodes.Status200OK, gResponse);
        }
    }
}
