using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualBasic;

namespace SimuladorDron.Application
{
    public class MotorVuelo
    {
        public int cantidadOperaciones = 0;
        private static readonly Random _random = new Random();
        private readonly int[] _desplazamientoFila = { -2, -2, 2, 2, -1, -1, 1, 1 };
        private readonly int[] _desplazamientoColumna = { -1, 1, -1, 1, -2, 2, -2, 2 };

        private int _maxPasosEncontrados = 0;
        private int[,] _mejorTablero;
        private List<(int X, int Y)> _mejorTraza;

        public (int[,] MatrizRecorrido, List<(int X, int Y)> Traza, bool Exito) ExplorarTerreno(int n, int inicioX, int inicioY)
        {
            cantidadOperaciones = 0;
            _maxPasosEncontrados = 0;
            _mejorTablero = null;
            _mejorTraza = null;

            int[,] tablero = new int[n, n];
            for (int i = 0; i < n; i++)
            {
                for (int j = 0; j < n; j++)
                {
                    tablero[i, j] = -1; // -1 significa no visitado
                }
            }

            int parcelasAlcanzables = ContarParcelasAlcanzables(n, inicioX, inicioY);

            List<(int X, int Y)> traza = new List<(int X, int Y)>();
            
            tablero[inicioX, inicioY] = 0;
            traza.Add((inicioX, inicioY));

            bool exito = ResolverRecursivo(n, inicioX, inicioY, 1, parcelasAlcanzables, tablero, traza);

            if (!exito && _mejorTraza != null)
            {
                return (_mejorTablero, _mejorTraza, false);
            }

            return (tablero, traza, exito);
        }

        private bool ResolverRecursivo(int n, int actualX, int actualY, int pasoActual, int objetivoPasos, int[,] tablero, List<(int X, int Y)> traza)
        {
            if (pasoActual > _maxPasosEncontrados)
            {
                _maxPasosEncontrados = pasoActual;
                _mejorTablero = (int[,])tablero.Clone();
                _mejorTraza = new List<(int X, int Y)>(traza);
            }

            cantidadOperaciones++;
            
            if(cantidadOperaciones >= 5000000)
            {
                return false;
            }
            
            if (pasoActual == objetivoPasos)
            {
                return true;
            }

            var candidatos = ObtenerCandidatosOrdenadosPorGrado(n, actualX, actualY, tablero);

            foreach (var candidato in candidatos)
            {
                int sigX = candidato.X;
                int sigY = candidato.Y;

                tablero[sigX, sigY] = pasoActual;
                traza.Add((sigX, sigY));

                if (ResolverRecursivo(n, sigX, sigY, pasoActual + 1, objetivoPasos, tablero, traza))
                {
                    return true;
                }

                // Backtracking
                tablero[sigX, sigY] = -1;
                traza.RemoveAt(traza.Count - 1);
            }

            return false;
        }

        private List<(int X, int Y)> ObtenerCandidatosOrdenadosPorGrado(int n, int actualX, int actualY, int[,] tablero)
        {
            var candidatos = new List<(int X, int Y, int Grado, int GradoSiguiente)>();

            for (int i = 0; i < 8; i++)
            {
                int sigX = actualX + _desplazamientoFila[i];
                int sigY = actualY + _desplazamientoColumna[i];

                if (EsMovimientoValido(n, sigX, sigY, tablero))
                {
                    int grado = CalcularGrado(n, sigX, sigY, tablero);
                    
                    int gradoSiguiente = 0;
                    tablero[sigX, sigY] = -2; // temporal
                    for (int j = 0; j < 8; j++)
                    {
                        int vecX = sigX + _desplazamientoFila[j];
                        int vecY = sigY + _desplazamientoColumna[j];
                        if (EsMovimientoValido(n, vecX, vecY, tablero))
                        {
                            gradoSiguiente += CalcularGrado(n, vecX, vecY, tablero);
                        }
                    }
                    tablero[sigX, sigY] = -1; // restauro

                    candidatos.Add((sigX, sigY, grado, gradoSiguiente));
                }
            }

            return candidatos
                .OrderBy(c => c.Grado)
                .ThenBy(c => _random.Next())
                .Select(c => (c.X, c.Y))
                .ToList();
        }

        private int CalcularGrado(int n, int x, int y, int[,] tablero)
        {
            int grado = 0;
            for (int i = 0; i < 8; i++)
            {
                int sigX = x + _desplazamientoFila[i];
                int sigY = y + _desplazamientoColumna[i];

                if (EsMovimientoValido(n, sigX, sigY, tablero))
                {
                    grado++;
                }
            }
            return grado;
        }

        private bool EsMovimientoValido(int n, int x, int y, int[,] tablero)
        {
            return x >= 0 && x < n && y >= 0 && y < n && tablero[x, y] == -1;
        }

        private int ContarParcelasAlcanzables(int n, int inicioX, int inicioY)
        {
            bool[,] visitado = new bool[n, n];
            Queue<(int X, int Y)> cola = new Queue<(int X, int Y)>();

            cola.Enqueue((inicioX, inicioY));
            visitado[inicioX, inicioY] = true;

            int contador = 0;

            while (cola.Count > 0)
            {
                var (x, y) = cola.Dequeue();
                contador++;

                for (int i = 0; i < 8; i++)
                {
                    int sigX = x + _desplazamientoFila[i];
                    int sigY = y + _desplazamientoColumna[i];

                    if (sigX >= 0 && sigX < n && sigY >= 0 && sigY < n && !visitado[sigX, sigY])
                    {
                        visitado[sigX, sigY] = true;
                        cola.Enqueue((sigX, sigY));
                    }
                }
            }

            return contador;
        }
    }
}
