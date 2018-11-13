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
        Task<string> ObtienePDFdelOSEAsync(string ruc, string tipoDoc, string serie, string correlativo, string ruta, string nombreArchivo, string extension);
        Tuple<string, string> ResumenDiario(string ruc, string usuario, string usuarioPassword, string texto);
        Task<string> ObtieneCDRdelOSEAsync(string ruc, string tipoDoc, string serie, string correlativo, string rutaYNomArchivoCfdi);
        string ObtieneCDRdelOSE(string ruc, string tipoDoc, string serie, string correlativo);
        string ObtieneXMLdelOSE(string ruc, string tipoDoc, string serie, string correlativo);
        Tuple<string, string> Baja(string ruc, string usuario, string usuarioPassword, string nroDocumento);
    }

}
