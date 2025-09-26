using Microsoft.EntityFrameworkCore;
using SistemaVenta.BLL.Implementacion;
using SistemaVenta.BLL.Interfaces;
using SistemaVenta.DAL.Interfaces;
using SistemaVenta.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace SistemaVenta.BLL.Implementacion
{
    public class UsuarioService : IUsuarioService
    {
        private readonly IGenericRepository<Usuario> _repositorio;
        private readonly IFireBaseService _fireBaseService;
        private readonly IUtilidadesService _utilidadesService;
        private readonly ICorreoService _correoService;

        public UsuarioService(
            IGenericRepository<Usuario> repositorio,
            IFireBaseService fireBaseService,
            IUtilidadesService utilidadesService,
            ICorreoService correoService)
        {
            _repositorio = repositorio;
            _fireBaseService = fireBaseService;
            _utilidadesService = utilidadesService;
            _correoService = correoService;
        }

        public async Task<List<Usuario>> Lista()
        {
            IQueryable<Usuario> query = await _repositorio.Consultar();
            return await query.Include(r => r.IdRolNavigation).ToListAsync();
        }


        public async Task<Usuario> Crear(Usuario entidad, Stream Foto = null, string NombreFoto = "", string UrlPlantillaCorreo = "")
        {
            var usuarioExistente = await _repositorio.Obtener(u => u.Correo == entidad.Correo);
            if (usuarioExistente != null)
                throw new InvalidOperationException("El correo ya existe");

            string claveGenerada = _utilidadesService.GenerarClave();
            entidad.Clave = _utilidadesService.ConvertirSha256(claveGenerada);
            entidad.NombreFoto = NombreFoto;

            if (Foto != null)
                entidad.UrlFoto = await _fireBaseService.SubirStorage(Foto, "carpeta_usuario", NombreFoto);

            var usuarioCreado = await _repositorio.Crear(entidad);

            if (usuarioCreado.IdUsuario == 0)
                throw new Exception("No se pudo crear el usuario");

            if (!string.IsNullOrWhiteSpace(UrlPlantillaCorreo))
            {
                var htmlCorreo = await ObtenerHtmlCorreo(UrlPlantillaCorreo, usuarioCreado.Correo, claveGenerada);
                if (!string.IsNullOrEmpty(htmlCorreo))
                    await _correoService.EnviarCorreo(usuarioCreado.Correo, "Cuenta creada correctamente", htmlCorreo);
            }

            var query = await _repositorio.Consultar(u => u.IdUsuario == usuarioCreado.IdUsuario);
            usuarioCreado = query.Include(r => r.IdRolNavigation).First();
            return usuarioCreado;
        }

        private async Task<string> ObtenerHtmlCorreo(string url, string correo, string clave)   
        {
            using var http = new HttpClient();
            var finalUrl = url.Replace("[correo]", correo).Replace("[clave]", clave);
            return await http.GetStringAsync(finalUrl);
        }


        public async Task<Usuario> Editar(Usuario entidad, Stream Foto = null, string NombreFoto = "")
        {
            Usuario usuario_existe = await _repositorio.Obtener(u => u.Correo == entidad.Correo && u.IdUsuario != entidad.IdUsuario);

            if (usuario_existe != null)
                throw new TaskCanceledException("El correo ya existe");

            try
            {
                IQueryable<Usuario> queryUsuario = await _repositorio.Consultar(u => u.IdUsuario == entidad.IdUsuario);

                Usuario usuario_editar = queryUsuario.First();
                usuario_editar.Nombre = entidad.Nombre;
                usuario_editar.Correo = entidad.Correo;
                usuario_editar.Telefono = entidad.Telefono;
                usuario_editar.IdRol = entidad.IdRol;

                usuario_editar.EsActivo = entidad.EsActivo;

                if (usuario_editar.NombreFoto == "")
                    usuario_editar.NombreFoto = NombreFoto;
                
                if (Foto != null)
                {
                    string urlFoto = await _fireBaseService.SubirStorage(Foto, "carpeta_usuario", usuario_editar.NombreFoto);
                    usuario_editar.UrlFoto = urlFoto;
                }
                bool respuesta = await _repositorio.Editar(usuario_editar);

                if (!respuesta)
                    throw new TaskCanceledException("No se pudo editar el usuario");

                Usuario usuario_editado = queryUsuario.Include(r => r.IdRolNavigation).First();
                return usuario_editado;
            }
            catch (Exception)
            {
                throw; 
            }
        }

        public async Task<bool> Eliminar(int IdUsuario)
        {
            try
            {
                Usuario usuario_encontrado = await _repositorio.Obtener(u => u.IdUsuario == IdUsuario);

                if (usuario_encontrado == null)
                    throw new TaskCanceledException("El Usuario no existe");

                string nombreFoto = usuario_encontrado.NombreFoto;
                bool respuesta = await _repositorio.Eliminar(usuario_encontrado);

                if (respuesta)
                    await _fireBaseService.EliminarStorage("carpeta_usuario", nombreFoto);
                return true;
            }
            catch (Exception ex) {
                throw;
            }
        }

        public async Task<Usuario> ObtenerPorCredenciales(string correo, string clave)
        {
            string clave_encriptada = _utilidadesService.ConvertirSha256(clave);

            Usuario usuario_encontrado = await _repositorio.Obtener(u => u.Correo.Equals(correo)
            && u.Clave.Equals(clave_encriptada));

            return usuario_encontrado;
        }

        public async Task<Usuario> ObtenerPorId(int IdUsuario)
        {
            // Incluimos la navegación para obtener el nombre del rol
            IQueryable<Usuario> query = await _repositorio.Consultar(u => u.IdUsuario == IdUsuario);
            Usuario resultado = query.Include(u => u.IdRolNavigation).FirstOrDefault();
            return resultado;
        }


        public async Task<bool> GuardarPerfil(Usuario entidad)
        {
            try
            {
                Usuario usuario_encontrado = await _repositorio.Obtener(u => u.IdUsuario == entidad.IdUsuario);

                if(usuario_encontrado == null)
                    throw new TaskCanceledException("El Usuario no existe");

                usuario_encontrado.Correo = entidad.Correo;
                usuario_encontrado.Telefono = entidad.Telefono;

                bool respuesta = await _repositorio.Editar(usuario_encontrado);
                return respuesta;
            }
            catch (Exception ex) {
                throw;
            }
        }

        public async Task<bool> CambiarClave(int IdUsuario, string ClaveActual, string ClaveNueva)
        {
            try
            {
                Usuario usuario_encontrado = await _repositorio.Obtener(u => u.IdUsuario == IdUsuario);

                if (usuario_encontrado == null)
                    throw new TaskCanceledException("El Usuario no existe");

                if (usuario_encontrado.Clave != _utilidadesService.ConvertirSha256(ClaveActual))
                    throw new TaskCanceledException("La clave actual no es correcta");

                usuario_encontrado.Clave = _utilidadesService.ConvertirSha256(ClaveNueva);
                bool respuesta = await _repositorio.Editar(usuario_encontrado);
                return respuesta;
            }
            catch (Exception ex) {
                throw;
            }
        }

        public async Task<bool> RestablecerClave(string Correo, string UrlPlantillaCorreo)
        {
            try
            {
                Usuario usuario_encontrado = await _repositorio.Obtener(u => u.Correo == Correo);
                if (usuario_encontrado == null)
                    throw new TaskCanceledException("No encontramos ningun usuario asociado al correo");

                string clave_generada = _utilidadesService.GenerarClave();
                usuario_encontrado.Clave = _utilidadesService.ConvertirSha256(clave_generada);


                UrlPlantillaCorreo = UrlPlantillaCorreo.Replace("[clave]", clave_generada);

                string htmlCorreo = "";

                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(UrlPlantillaCorreo);
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();

                if (response.StatusCode == HttpStatusCode.OK)
                {

                    using (Stream dataStream = response.GetResponseStream())
                    {

                        StreamReader readerStream = null;

                        if (response.CharacterSet == null)
                            readerStream = new StreamReader(dataStream);
                        else
                            readerStream = new StreamReader(dataStream, Encoding.GetEncoding(response.CharacterSet));

                        htmlCorreo = readerStream.ReadToEnd();
                        response.Close();
                        readerStream.Close();

                    }
                }
                bool correo_enviado = await _repositorio.Editar(usuario_encontrado);
                if (htmlCorreo != "")
                    correo_enviado = await _correoService.EnviarCorreo(Correo, "Contraseña restablecida", htmlCorreo);

                if(!correo_enviado)
                    throw new TaskCanceledException("Tenemos problemas, porfavor intente mas tarde");

                bool respuesta = await _repositorio.Editar(usuario_encontrado);
                return respuesta;
            }
            catch (Exception ex)
            {
                throw;
            }
        }
    }
}