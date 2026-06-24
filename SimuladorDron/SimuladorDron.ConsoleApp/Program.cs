using System;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Configuration;
using SimuladorDron.Application;
using SimuladorDron.Domain.Entidades;
using SimuladorDron.Infrastructure;

namespace SimuladorDron.ConsoleApp
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("================================================================================");
            Console.WriteLine("SIMULADOR DE TRAYECTORIA DE DRON");
            Console.WriteLine("================================================================================");

            // 1. Cargar Configuración
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

            IConfiguration configuration = builder.Build();

            // Usamos la cadena de Admin por defecto para poder crear la BD
            string connectionString = configuration.GetConnectionString("AdminConnection");

            if (string.IsNullOrEmpty(connectionString))
            {
                Console.WriteLine("Error: No se encontró la cadena de conexión 'AdminConnection' en appsettings.json.");
                return;
            }

            var persistencia = new PersistenciaDron(connectionString);

            try
            {
                // Inicializamos BD (Crea tablas si no existen)
                persistencia.InicializarBaseDeDatos();
                Console.WriteLine("[INFO] Base de datos verificada/inicializada correctamente.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Falló la conexión o creación de base de datos: {ex.Message}");
                return;
            }

            // 2. Solicitar Parámetros
            int n = Utils.SolicitarEntero("Ingrese la dimensión del terreno N (ej. 8): ", min: 1);
            int inicioX = Utils.SolicitarEntero($"Ingrese la fila de despegue X [0 a {n - 1}]: ", min: 0, max: n - 1);
            int inicioY = Utils.SolicitarEntero($"Ingrese la columna de despegue Y [0 a {n - 1}]: ", min: 0, max: n - 1);

            // 3. Ejecutar Algoritmo
            Console.WriteLine("\n[INFO] Calculando ruta...");
            var motor = new MotorVuelo();
            var resultado = motor.ExplorarTerreno(n, inicioX, inicioY);

            // 4. Mostrar Matriz
            Console.WriteLine("\n=== RESULTADO DEL RECORRIDO ===");
            for (int i = 0; i < n; i++)
            {
                for (int j = 0; j < n; j++)
                {
                    if (resultado.MatrizRecorrido[i, j] == -1)
                    {
                        Console.Write("  . ");
                    }
                    else
                    {
                        Console.Write($"{resultado.MatrizRecorrido[i, j],3} ");
                    }
                }
                Console.WriteLine();
            }

            if (resultado.Exito)
            {
                Console.WriteLine("\n[ÉXITO] El dron logró recorrer todas las parcelas alcanzables.");
                
                // 5. Guardar en PostgreSQL
                var cabecera = new ControlMaestro
                {
                    FechaEjecucion = DateTime.Now,
                    DimensionN = n,
                    InicioX = inicioX,
                    InicioY = inicioY
                };

                var secuenciaLogs = resultado.Traza.Select((coords, index) => new DetalleLog
                {
                    EtiquetaPaso = index,
                    X = coords.X,
                    Y = coords.Y
                }).ToList();

                try
                {
                    int idGenerado = persistencia.GuardarResultados(cabecera, secuenciaLogs);
                    Console.WriteLine($"\n[INFO] Resultados guardados en PostgreSQL correctamente. ID Ejecución: {idGenerado}");

                    // 6. Reporte Inverso
                    Console.WriteLine("\n=== REPORTE INVERSO (ÚLTIMOS 5 PASOS) ===");
                    var ultimosPasos = persistencia.ObtenerUltimosCincoPasos(idGenerado);
                    
                    if (ultimosPasos.Count == 0)
                    {
                        Console.WriteLine("No se encontraron pasos registrados.");
                    }
                    else
                    {
                        foreach (var paso in ultimosPasos)
                        {
                            Console.WriteLine($"ID Registro: {paso.Id} | Paso Real Desofuscado: {paso.EtiquetaPaso} | Coordenadas: ({paso.X}, {paso.Y})");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"\n[ERROR] Al guardar o consultar en BD: {ex.Message}");
                }
            }
            else
            {
                Console.WriteLine("\n[SIN SOLUCIÓN] El dron alcanza las parcelas pero no existe ruta que las cubra sin repetir.");
            }

            Console.WriteLine("\nPresione cualquier tecla para finalizar...");
            Console.ReadKey();
        }
    }
}
