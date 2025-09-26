using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using SistemaVenta.BLL.Implementacion;
using System.IO;
using System.Threading.Tasks;

namespace SistemaVenta.AplicacionWeb.Controllers
{
    [Authorize]
    public class RespaldoController : Controller
    {
        private readonly RespaldoServicio _respaldoServicio;
        private readonly string _backupFolder;

        public RespaldoController(IConfiguration config)
        {
            // lee connection string del appsettings.json
            string conn = config.GetConnectionString("DefaultConnection");
            _respaldoServicio = new RespaldoServicio(conn, "DBVENTAS");

            // Carpeta por defecto para respaldos (puedes cambiarla)
            _backupFolder = Path.Combine(Directory.GetCurrentDirectory(), "Backups");
        }

        public IActionResult Index()
        {
            return View();
        }
        
        public IActionResult Backup()
        {
            return View();
        }

        public IActionResult Restore()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> BackupPost()
        {
            var (ok, message, backupFile) = await _respaldoServicio.BackupDatabaseAsync(_backupFolder);
            if (ok)
            {
                // devolver nombre de archivo para que el usuario lo descargue si quiere
                TempData["Success"] = $"{message} Archivo: {Path.GetFileName(backupFile)}";
            }
            else
            {
                TempData["Error"] = message;
            }
            return RedirectToAction("Backup");
        }

        [HttpPost]
        public async Task<IActionResult> RestorePost()
        {
            var file = Request.Form.Files["backupFile"];
            if (file == null)
            {
                TempData["Error"] = "No se subió ningún archivo .bak";
                return RedirectToAction("Index");
            }

            // Guardar temporalmente el bak
            string tempFolder = Path.Combine(Path.GetTempPath(), "RespaldoTemp");
            if (!Directory.Exists(tempFolder)) Directory.CreateDirectory(tempFolder);
            string tempPath = Path.Combine(tempFolder, Path.GetFileName(file.FileName));

            using (var stream = System.IO.File.Create(tempPath))
            {
                await file.CopyToAsync(stream);
            }

            var (ok, message) = await _respaldoServicio.RestoreDatabaseAsync(tempPath);

            // opcional: borrar temp
            try { System.IO.File.Delete(tempPath); } catch { }

            if (ok) TempData["Success"] = message; else TempData["Error"] = message;
            return RedirectToAction("Restore");
        }
    }
}
