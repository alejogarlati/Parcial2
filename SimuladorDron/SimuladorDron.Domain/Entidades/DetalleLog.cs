namespace SimuladorDron.Domain.Entidades
{
    public class DetalleLog
    {
        public int Id { get; set; }
        public int IdMasterControl { get; set; }
        public int EtiquetaPaso { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
    }
}
