using System.Collections.Generic;
using SimuladorDron.Domain.Entidades;

namespace SimuladorDron.Domain.Interfaces
{
    public interface IPersistenciaDron
    {
        void InicializarBaseDeDatos();
        int GuardarResultados(ControlMaestro cabecera, List<DetalleLog> secuencia);
        List<DetalleLog> ObtenerUltimosCincoPasos(int idMasterControl);
    }
}
