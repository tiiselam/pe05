using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cfdiPeruInterfaces
{
    public interface ICfdiMetodosWebService
    {
        string TimbraYEnviaASunat(string ruc, string usuario, string usuarioPassword, string texto);
        Task<string> ObtieneYGuardaPDFAsync(string ruc, string tipoDoc, string serie, string correlativo, string ruta, string nombreArchivo, string extension);
        Tuple<string, string> ResumenDiario(string ruc, string usuario, string usuarioPassword, string texto);
        Task<string> ObtieneYGuardaCDRAsync(string ruc, string tipoDoc, string serie, string correlativo, string rutaYNomArchivoCfdi);

    }

}
