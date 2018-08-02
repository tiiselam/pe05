using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cfdiPeruDFactureWS
{
    public class WSDFacture
    {
        pe.dfacture.ws.MetodosClient Servicio = new pe.dfacture.ws.MetodosClient();

        public string TimbraYEnviaASunat(string ruc, string usuario, string usuarioPassword, string texto)
        {
            pe.dfacture.ws.RespuestaTimbradoTXT respuestaT = new pe.dfacture.ws.RespuestaTimbradoTXT();

            byte[] archivo = System.Text.Encoding.UTF8.GetBytes(texto);
            
            respuestaT = Servicio.timbraEnviaTXT32(ruc, usuario, usuarioPassword, archivo);

            if (respuestaT.Procesado)
            {
                Debug.WriteLine(" generada con el número: " + respuestaT.UUID);
            }
            else
            {
                throw new TimeoutException("Excepción al intentar conectarse con el Web Service de Facturacion de DFactur-e. N° Error: " + respuestaT.NumeroError + ", Mensaje Error: " + respuestaT.MensajeError);
            }
            return respuestaT.XMLTimbrado;
        }
    }
}
