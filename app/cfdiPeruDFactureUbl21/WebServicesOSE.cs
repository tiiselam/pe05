using cfdiEntidadesGP;
using cfdiPeruInterfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace cfdiPeruOperadorServiciosElectronicos
{
    public class WebServicesOSE : ICfdiMetodosWebService
    {
        public WebServicesOSE(string URLwebServPAC)
        {

        }

        public string TimbraYEnviaASunat(string ruc, string usuario, string usuarioPassword, DocumentoVentaGP documentoGP)
        {
            throw new NotImplementedException();
        }

        public Task<string> ObtienePDFdelOSEAsync(string ruc, string tipoDoc, string serie, string correlativo, string ruta, string nombreArchivo, string extension)
        {
            throw new NotImplementedException();
        }

        public string ObtieneXMLdelOSE(string ruc, string tipoDoc, string serie, string correlativo)
        {
            throw new NotImplementedException();
        }

        ///////////////////////////////////////////////////////

        public string TimbraYEnviaASunat(string ruc, string usuario, string usuarioPassword, string texto)
        {
            throw new NotImplementedException();
        }

        public Tuple<string, string> ResumenDiario(string ruc, string usuario, string usuarioPassword, string texto)
        {
            throw new NotImplementedException();
        }

        public Task<string> ObtieneCDRdelOSEAsync(string ruc, string tipoDoc, string serie, string correlativo, string rutaYNomArchivoCfdi)
        {
            throw new NotImplementedException();
        }

        public string ObtieneCDRdelOSE(string ruc, string tipoDoc, string serie, string correlativo)
        {
            throw new NotImplementedException();
        }

        public Tuple<string, string> Baja(string ruc, string usuario, string usuarioPassword, string nroDocumento)
        {
            throw new NotImplementedException();
        }

    }
}
