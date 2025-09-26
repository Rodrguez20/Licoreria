using Microsoft.Data.SqlClient;
using System;
using System.Data;
using System.IO;
using System.Threading.Tasks;

namespace SistemaVenta.BLL.Implementacion
{
    public class RespaldoServicio
    {
        private readonly string _connectionString;
        private readonly string _databaseName;

        public RespaldoServicio(string connectionString, string databaseName = "DBVENTAS")
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
            _databaseName = databaseName;
        }

        public async Task<(bool ok, string message, string backupFile)> BackupDatabaseAsync(string targetFolder)
        {
            if (string.IsNullOrWhiteSpace(targetFolder))
                return (false, "Carpeta destino no indicada.", null);

            try
            {
                if (!Directory.Exists(targetFolder))
                    Directory.CreateDirectory(targetFolder);

                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string backupFile = Path.Combine(targetFolder, $"{_databaseName}_backup_{timestamp}.bak");

                string sql = $"BACKUP DATABASE [{_databaseName}] TO DISK = @backupPath WITH INIT, STATS = 10";

                using (var conn = new SqlConnection(_connectionString))
                {
                    await conn.OpenAsync();
                    using (var cmd = new SqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@backupPath", backupFile);
                        cmd.CommandTimeout = 0;
                        await cmd.ExecuteNonQueryAsync();
                    }
                }

                return (true, "Respaldo creado correctamente.", backupFile);
            }
            catch (Exception ex)
            {
                return (false, $"Error al respaldar: {ex.Message}", null);
            }
        }

        public async Task<(bool ok, string message)> RestoreDatabaseAsync(string backupFilePath)
        {
            if (!File.Exists(backupFilePath))
                return (false, "Archivo .bak no existe.");

            try
            {
                // Conexión a master para ejecutar comandos que afectan la BD
                var builder = new SqlConnectionStringBuilder(_connectionString);
                builder.InitialCatalog = "master";
                string masterConnStr = builder.ToString();

                using (var conn = new SqlConnection(masterConnStr))
                {
                    await conn.OpenAsync();

                    // 1) Obtener nombres lógicos del backup
                    DataTable fileList = new DataTable();
                    using (var cmd = new SqlCommand("RESTORE FILELISTONLY FROM DISK = @bak", conn))
                    {
                        cmd.Parameters.AddWithValue("@bak", backupFilePath);
                        using (var da = new SqlDataAdapter(cmd))
                        {
                            da.Fill(fileList);
                        }
                    }

                    if (fileList.Rows.Count < 1)
                        return (false, "No se pudo leer la lista de archivos dentro del backup.");

                    // nombres lógicos
                    string logicalDataName = fileList.Rows[0]["LogicalName"].ToString();
                    string logicalLogName = fileList.Rows.Count > 1 ? fileList.Rows[1]["LogicalName"].ToString() : logicalDataName + "_log";

                    // 2) Obtener ruta física actual de los archivos de DB (para LocalDB)
                    string dataFolder = await GetDatabaseFilesFolder(conn, _databaseName);
                    if (string.IsNullOrEmpty(dataFolder))
                    {
                        // si no existe la DB (o no encontramos la ruta), usamos la carpeta del backup
                        dataFolder = Path.GetDirectoryName(backupFilePath);
                    }

                    string targetMdf = Path.Combine(dataFolder, $"{_databaseName}.mdf");
                    string targetLdf = Path.Combine(dataFolder, $"{_databaseName}_log.ldf");

                    // 3) Poner la DB en single_user con rollback immediate
                    string singleUser = $"ALTER DATABASE [{_databaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE";
                    using (var cmd = new SqlCommand(singleUser, conn)) { cmd.CommandTimeout = 0; await cmd.ExecuteNonQueryAsync(); }

                    // 4) Ejecutar RESTORE con MOVE y REPLACE
                    string restoreSql =
                        $"RESTORE DATABASE [{_databaseName}] FROM DISK = @bak WITH REPLACE, " +
                        $"MOVE @logicalData TO @mdfPath, MOVE @logicalLog TO @ldfPath";

                    using (var cmd = new SqlCommand(restoreSql, conn))
                    {
                        cmd.Parameters.AddWithValue("@bak", backupFilePath);
                        cmd.Parameters.AddWithValue("@logicalData", logicalDataName);
                        cmd.Parameters.AddWithValue("@logicalLog", logicalLogName);
                        cmd.Parameters.AddWithValue("@mdfPath", targetMdf);
                        cmd.Parameters.AddWithValue("@ldfPath", targetLdf);
                        cmd.CommandTimeout = 0;
                        await cmd.ExecuteNonQueryAsync();
                    }

                    // 5) Volver a multi_user
                    string multiUser = $"ALTER DATABASE [{_databaseName}] SET MULTI_USER";
                    using (var cmd = new SqlCommand(multiUser, conn)) { cmd.CommandTimeout = 0; await cmd.ExecuteNonQueryAsync(); }
                }

                return (true, "Restauración completada correctamente.");
            }
            catch (Exception ex)
            {
                return (false, $"Error al restaurar: {ex.Message}");
            }
        }

        private async Task<string> GetDatabaseFilesFolder(SqlConnection conn, string databaseName)
        {
            try
            {
                // Intentamos obtener una ruta física de los archivos actuales de la BD (si existe)
                string sql = @"SELECT physical_name FROM sys.master_files mf
                               JOIN sys.databases d ON mf.database_id = d.database_id
                               WHERE d.name = @dbName AND mf.type_desc = 'ROWS'";

                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@dbName", databaseName);
                    var result = await cmd.ExecuteScalarAsync();
                    if (result != null && result != DBNull.Value)
                    {
                        string physical = result.ToString();
                        string folder = Path.GetDirectoryName(physical);
                        return folder;
                    }
                }
            }
            catch
            {
                // Ignoramos; devolveremos null si falla
            }

            return null;
        }
    }
}
