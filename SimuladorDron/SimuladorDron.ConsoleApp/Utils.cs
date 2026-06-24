using System;

namespace SimuladorDron.ConsoleApp
{
    public static class Utils
    {
        public static int SolicitarEntero(string mensaje, int min, int max = int.MaxValue)
        {
            int valor;
            while (true)
            {
                Console.Write(mensaje);
                if (int.TryParse(Console.ReadLine(), out valor) && valor >= min && valor <= max)
                {
                    return valor;
                }
                Console.WriteLine($"Error: Ingrese un número válido entre {min} y {max}.");
            }
        }
    }
}
