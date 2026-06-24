using System;

namespace SimuladorDron.Domain.Entidades
{
    public class ControlMaestro
    {
        public int Id { get; set; }
        public DateTime FechaEjecucion { get; set; }
        public int DimensionN { get; set; }
        public int InicioX { get; set; }
        public int InicioY { get; set; }
    }
}
