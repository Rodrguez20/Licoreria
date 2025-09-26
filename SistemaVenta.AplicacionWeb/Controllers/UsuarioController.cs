using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using SistemaVenta.AplicacionWeb.Models.ViewModels;
using SistemaVenta.AplicacionWeb.Utilidades.Automapper;
using SistemaVenta.AplicacionWeb.Utilidades.Response;
using SistemaVenta.BLL.Implementacion;
using SistemaVenta.BLL.Interfaces;
using SistemaVenta.DAL.Implementacion;
using SistemaVenta.Entity;


namespace SistemaVenta.AplicacionWeb.Controllers
{
    [Authorize]
    public class UsuarioController : Controller
    {

        private readonly IUsuarioService _usuariosServicio;
        private readonly IRolService _rolServicio;
        private readonly IMapper _mapper;
        public UsuarioController(IUsuarioService usuariosServicio, IRolService rolServicio, IMapper mapper)
        {
            _usuariosServicio = usuariosServicio;
            _rolServicio = rolServicio;
            _mapper = mapper;
        }

        public IActionResult Index()
        {
            return View();
        }
        [HttpGet]

        public async Task<IActionResult> ListaRoles()
        {
            List<VMRol> vmListaRoles = _mapper.Map<List<VMRol>>(await _rolServicio.ListarRoles());
            return StatusCode(StatusCodes.Status200OK, vmListaRoles);
        }

        [HttpGet]

        public async Task<IActionResult> Lista()
        {
            List<VMUsuario> vmUsuarioLista = _mapper.Map<List<VMUsuario>>(await _usuariosServicio.Lista());
            return StatusCode(StatusCodes.Status200OK, new { data = vmUsuarioLista });
        }
        [HttpPost]

        public async Task<IActionResult> Crear([FromForm] IFormFile foto, [FromForm] string modelo)
        {
            GenericRespose<VMUsuario> gRespose = new GenericRespose<VMUsuario>();

            try
            {
                VMUsuario vmUsuario = JsonConvert.DeserializeObject<VMUsuario>(modelo);

                string nombreFoto = "";
                Stream fotoStream = null;

                if (foto != null)
                {
                    string nombre_en_codigo = Guid.NewGuid().ToString("N");
                    string extension = Path.GetExtension(foto.FileName);
                    nombreFoto = string.Concat(nombre_en_codigo, extension);
                    fotoStream = foto.OpenReadStream();
                }

                string urlPlantillaCorreo = $"{this.Request.Scheme}://{this.Request.Host}/Plantilla/EnviarClave?correo=[correo]&clave=[clave]";

                Usuario usuario_creado = await _usuariosServicio.Crear(_mapper.Map<Usuario>(vmUsuario), fotoStream, nombreFoto, urlPlantillaCorreo);

                vmUsuario = _mapper.Map<VMUsuario>(usuario_creado);

                gRespose.Estado = true;
                gRespose.Objeto = vmUsuario;
            }
            catch (Exception ex)
            {
                gRespose.Estado = false;
                gRespose.Mensaje = ex.Message;
            }
            return StatusCode(StatusCodes.Status200OK, gRespose);
        }

        [HttpPut]

        public async Task<IActionResult> Editar([FromForm] IFormFile foto, [FromForm] string modelo)
        {
            GenericRespose<VMUsuario> gRespose = new GenericRespose<VMUsuario>();

            try
            {
                VMUsuario vmUsuario = JsonConvert.DeserializeObject<VMUsuario>(modelo);

                string nombreFoto = "";
                Stream fotoStream = null;

                if (foto != null)
                {
                    string nombre_en_codigo = Guid.NewGuid().ToString("N");
                    string extension = Path.GetExtension(foto.FileName);
                    nombreFoto = string.Concat(nombre_en_codigo, extension);
                    fotoStream = foto.OpenReadStream();
                }

                Usuario usuario_editado = await _usuariosServicio.Editar(_mapper.Map<Usuario>(vmUsuario), fotoStream, nombreFoto);

                vmUsuario = _mapper.Map<VMUsuario>(usuario_editado);

                gRespose.Estado = true;
                gRespose.Objeto = vmUsuario;
            }
            catch (Exception ex)
            {
                gRespose.Estado = false;
                gRespose.Mensaje = ex.Message;
            }
            return StatusCode(StatusCodes.Status200OK, gRespose);
        }

        [HttpDelete]

        public async Task<IActionResult> Eliminar(int idUsuario)
        {
            GenericRespose<string> gRespose = new GenericRespose<string>();

            try
            {
                // Si Eliminar devuelve un objeto con una propiedad Exito
                gRespose.Estado = await _usuariosServicio.Eliminar(idUsuario);


            }
            catch (Exception ex)
            {
                gRespose.Estado = false;
                gRespose.Mensaje = ex.Message;
            }
            return StatusCode(StatusCodes.Status200OK, gRespose);
        }



    }
}
