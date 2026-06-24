using System;
using System.Collections.Generic;
using Npgsql;
using SimuladorDron.Domain.Entidades;
using SimuladorDron.Domain.Interfaces;

namespace SimuladorDron.Infrastructure
{
    public class PersistenciaDron : IPersistenciaDron
    {
        private readonly string _connectionString;

        public PersistenciaDron(string connectionString)
        {
            _connectionString = connectionString;
        }

        public void InicializarBaseDeDatos()
        {
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                conn.Open();

                string ddlMaster = @"
                    CREATE TABLE IF NOT EXISTS tb_master_control (
                        id SERIAL PRIMARY KEY,
                        fecha_ejecucion TIMESTAMP NOT NULL,
                        dimension_n INTEGER NOT NULL,
                        inicio_x INTEGER NOT NULL,
                        inicio_y INTEGER NOT NULL
                    );";

                using (var cmd = new NpgsqlCommand(ddlMaster, conn))
                {
                    cmd.ExecuteNonQuery();
                }

                string ddlLog = @"
                    CREATE TABLE IF NOT EXISTS tb_det_log (
                        id SERIAL PRIMARY KEY,
                        fk_master_control INTEGER NOT NULL,
                        etiqueta_paso INTEGER NOT NULL,
                        x INTEGER NOT NULL,
                        y INTEGER NOT NULL,
                        FOREIGN KEY (fk_master_control) REFERENCES tb_master_control (id)
                    );";

                using (var cmd = new NpgsqlCommand(ddlLog, conn))
                {
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public int GuardarResultados(ControlMaestro cabecera, List<DetalleLog> secuencia)
        {
            int idGenerado = 0;

            using (var conn = new NpgsqlConnection(_connectionString))
            {
                conn.Open();
                using (var tx = conn.BeginTransaction())
                {
                    try
                    {
                        // 1. Guardar Cabecera y obtener ID
                        string sqlMaster = @"
                            INSERT INTO tb_master_control (fecha_ejecucion, dimension_n, inicio_x, inicio_y) 
                            VALUES (@fecha, @dim, @x, @y) RETURNING id;";

                        using (var cmdMaster = new NpgsqlCommand(sqlMaster, conn, tx))
                        {
                            cmdMaster.Parameters.AddWithValue("fecha", cabecera.FechaEjecucion);
                            cmdMaster.Parameters.AddWithValue("dim", cabecera.DimensionN);
                            cmdMaster.Parameters.AddWithValue("x", cabecera.InicioX);
                            cmdMaster.Parameters.AddWithValue("y", cabecera.InicioY);

                            idGenerado = Convert.ToInt32(cmdMaster.ExecuteScalar());
                        }

                        // 2. Guardar Detalles con ofuscación y bucle while manual (SIN for/foreach)
                        string sqlLog = @"
                            INSERT INTO tb_det_log (fk_master_control, etiqueta_paso, x, y) 
                            VALUES (@fk, @paso, @x, @y);";

                        int i = 0;
                        while (i < secuencia.Count)
                        {
                            var detalle = secuencia[i];
                            int pasoOriginal = detalle.EtiquetaPaso;
                            int pasoOfuscado;

                            // Regla de ofuscación
                            if (pasoOriginal % 2 == 0)
                            {
                                pasoOfuscado = pasoOriginal * 2;
                            }
                            else
                            {
                                pasoOfuscado = -pasoOriginal;
                            }

                            using (var cmdLog = new NpgsqlCommand(sqlLog, conn, tx))
                            {
                                cmdLog.Parameters.AddWithValue("fk", idGenerado);
                                cmdLog.Parameters.AddWithValue("paso", pasoOfuscado);
                                cmdLog.Parameters.AddWithValue("x", detalle.X);
                                cmdLog.Parameters.AddWithValue("y", detalle.Y);

                                cmdLog.ExecuteNonQuery();
                            }

                            i++; // Avance manual del índice
                        }

                        tx.Commit();
                    }
                    catch (Exception)
                    {
                        tx.Rollback();
                        throw;
                    }
                }
            }

            return idGenerado;
        }

        public List<DetalleLog> ObtenerUltimosCincoPasos(int idMasterControl)
        {
            var resultados = new List<DetalleLog>();

            using (var conn = new NpgsqlConnection(_connectionString))
            {
                conn.Open();
                string sql = "SELECT id, fk_master_control, etiqueta_paso, x, y FROM tb_det_log WHERE fk_master_control = @fk ORDER BY id DESC LIMIT 5;";

                using (var cmd = new NpgsqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("fk", idMasterControl);

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            int pasoOfuscado = reader.GetInt32(2);
                            int pasoReal;

                            // Ingeniería inversa
                            if (pasoOfuscado < 0)
                            {
                                pasoReal = -pasoOfuscado;
                            }
                            else
                            {
                                pasoReal = pasoOfuscado / 2;
                            }

                            resultados.Add(new DetalleLog
                            {
                                Id = reader.GetInt32(0),
                                IdMasterControl = reader.GetInt32(1),
                                EtiquetaPaso = pasoReal,
                                X = reader.GetInt32(3),
                                Y = reader.GetInt32(4)
                            });
                        }
                    }
                }
            }

            return resultados;
        }
    }
}
