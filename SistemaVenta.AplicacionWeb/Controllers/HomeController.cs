using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaVenta.AplicacionWeb.Models;
using System.Diagnostics;

using System.Security.Claims;
using AutoMapper;
using SistemaVenta.AplicacionWeb.Models.ViewModels;
using SistemaVenta.AplicacionWeb.Utilidades.Response;
using SistemaVenta.BLL.Interfaces;
using SistemaVenta.Entity;


namespace SistemaVenta.AplicacionWeb.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {

        private readonly IUsuarioService _usuarioServicio;
        private readonly IMapper _mapper;

        public HomeController(IUsuarioService usuarioServicio, IMapper mapper)
        {
            _usuarioServicio = usuarioServicio;
            _mapper = mapper;
        }
        public IActionResult Index()
        {
            var nombreUsuario = User.Identity.IsAuthenticated ? User.Identity.Name : "Invitado";

            // O si tenés un claim con el nombre completo
            // var nombreUsuario = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value;

            ViewData["nombreUsuario"] = nombreUsuario;

            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }
        public IActionResult Perfil()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> ObtenerUsuario()
        {
            GenericRespose<VMUsuario> reponse = new GenericRespose<VMUsuario>();

            try
            {
                ClaimsPrincipal claimUser = HttpContext.User;

                string idUsuario = claimUser.Claims
                    .Where(c => c.Type == ClaimTypes.NameIdentifier)
                    .Select(c => c.Value).SingleOrDefault();

                VMUsuario usuario = _mapper.Map<VMUsuario>(await _usuarioServicio.ObtenerPorId(int.Parse(idUsuario)));

                reponse.Estado = true;
                reponse.Objeto = usuario;
            }
            catch (Exception ex)
            {
                reponse.Estado = false;
                reponse.Mensaje = ex.Message;
            }
            return StatusCode(StatusCodes.Status200OK, reponse);
        }

        [HttpPost]
        public async Task<IActionResult> GuardarPerfil([FromBody] VMUsuario modelo)
        {
            GenericRespose<VMUsuario> reponse = new GenericRespose<VMUsuario>();

            try
            {
                ClaimsPrincipal claimUser = HttpContext.User;

                string idUsuario = claimUser.Claims
                    .Where(c => c.Type == ClaimTypes.NameIdentifier)
                    .Select(c => c.Value).SingleOrDefault();

                Usuario entidad = _mapper.Map<Usuario>(modelo);

                entidad.IdUsuario = int.Parse(idUsuario);

                bool resultado = await _usuarioServicio.GuardarPerfil(entidad);

                reponse.Estado = resultado;
            }
            catch (Exception ex)
            {
                reponse.Estado = false;
                reponse.Mensaje = ex.Message;
            }
            return StatusCode(StatusCodes.Status200OK, reponse);
        }

        [HttpPost]
        public async Task<IActionResult> CambiarClave([FromBody] VmCambiarClave modelo)
        {
            GenericRespose<bool> reponse = new GenericRespose<bool>();

            try
            {
                ClaimsPrincipal claimUser = HttpContext.User;

                string idUsuario = claimUser.Claims
                    .Where(c => c.Type == ClaimTypes.NameIdentifier)
                    .Select(c => c.Value).SingleOrDefault();

                bool resultado = await _usuarioServicio.CambiarClave(
                        int.Parse(idUsuario),
                        modelo.claveActual,
                        modelo.claveNueva
                    );

                reponse.Estado = resultado; 
            }
            catch (Exception ex)
            {
                reponse.Estado = false;
                reponse.Mensaje = ex.Message;
            }
            return StatusCode(StatusCodes.Status200OK, reponse);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }


        public async Task<IActionResult> Salir()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login", "Acceso");
        }
    }
}
