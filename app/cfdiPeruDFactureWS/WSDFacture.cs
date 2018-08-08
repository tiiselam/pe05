using cfdiPeruInterfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cfdiPeruDFactureWS
{
    public class WSDFacture : ICfdiMetodosWebService
    {
        pe.dfacture.ws.MetodosClient Servicio = new pe.dfacture.ws.MetodosClient();

        public String TimbraYEnviaASunat(string ruc, string usuario, string usuarioPassword, string texto)
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
                throw new TimeoutException("Excepción al conectarse con el Web Service de Facturacion de DFactur-e. " + respuestaT.NumeroError + " - " + respuestaT.MensajeError);
            }
            return respuestaT.XMLTimbrado;
        }

        public Tuple<string, string> ResumenDiario(string ruc, string usuario, string usuarioPassword, string texto)
        {
            pe.dfacture.ws.Resumen respuestaT = new pe.dfacture.ws.Resumen();

            byte[] archivo = System.Text.Encoding.UTF8.GetBytes(texto);

            respuestaT = Servicio.ResumenDiario(ruc, usuario, usuarioPassword, archivo);

            if (respuestaT.Procesado)
            {
                Debug.WriteLine(" generada con el número de ticket: " + respuestaT.Nroticketdeatencion);
            }
            else
            {
                throw new TimeoutException("Excepción al conectarse con el Web Service de Facturacion de DFactur-e. " + respuestaT.NumeroError + " - " + respuestaT.MensajeError);
            }
            return Tuple.Create(respuestaT.Nroticketdeatencion, respuestaT.XmlResumen);

        }

        public string DescargaCDR(string ruc, string tipoDoc, string serie, string correlativo)
        {
            string serieCorrelativo = string.Concat(tipoDoc, "-", serie, "-", correlativo);
            string archivoCDR = Servicio.DescargaCDR(ruc, serieCorrelativo);
            if (archivoCDR == "0")
            {
                throw new ArgumentException(string.Concat("No se puede descargar el CDR. Verifique la numeración: tipo - serie - correlativo: ", serieCorrelativo, " e intente nuevamente más tarde. "));
            }
            return archivoCDR;
        }

        public async Task<string> ObtieneYGuardaCDRAsync(string ruc, string tipoDoc, string serie, string correlativo, string rutaYNomArchivoCfdi) //string ruta, string nombreArchivo, string extension)
        {
            //string rutaYNomArchivoCfdi = Path.Combine(ruta, nombreArchivo, extension);
            string archivoCDR = DescargaCDR(ruc, tipoDoc, serie, correlativo);

            byte[] data = Convert.FromBase64String(archivoCDR);
            using (FileStream SourceStream = File.Open(rutaYNomArchivoCfdi, FileMode.OpenOrCreate))
            {
                SourceStream.Seek(0, SeekOrigin.End);
                await SourceStream.WriteAsync(data, 0, data.Length);
            }

            return Encoding.UTF8.GetString(data);

        }

        public string DescargaPDF(string ruc, string tipoDoc, string serie, string correlativo)
        {
            string serieCorrelativo = string.Concat(tipoDoc, "-", serie, "-", correlativo);
            string archivoPdf = Servicio.DescargaPDF(ruc, serieCorrelativo);
            if (archivoPdf == "0")
            {
                throw new ArgumentException(string.Concat("No se puede descargar el PDF. Verifique la numeración: tipo - serie - correlativo: ", serieCorrelativo, " e intente nuevamente más tarde. "));
            }
            return archivoPdf;
        }

        public async Task<string> ObtieneYGuardaPDFAsync(string ruc, string tipoDoc, string serie, string correlativo, string ruta, string nombreArchivo, string extension)
        {
            string rutaYNomArchivoCfdi = Path.Combine(ruta, nombreArchivo, extension);
            string archivoPdf = DescargaPDF(ruc, tipoDoc, serie, correlativo);

            byte[] data = Convert.FromBase64String(archivoPdf);
            using (FileStream SourceStream = File.Open(rutaYNomArchivoCfdi, FileMode.OpenOrCreate))
            {
                SourceStream.Seek(0, SeekOrigin.End);
                await SourceStream.WriteAsync(data, 0, data.Length);
            }

            return rutaYNomArchivoCfdi;

        }

        //public async Task DescargaPDFAsync(string ruc, string tipoDoc, string serie, string correlativo)
        //{
        //    Servicio.DescargaPDFCompleted += Servicio_DescargaPDFCompleted;
        //    await Servicio.DescargaPDFAsync(ruc, string.Concat(tipoDoc ,"-" , serie ,"-" ,correlativo));
        //    //return ;
        //}

        //private void Servicio_DescargaPDFCompleted(object sender, pe.dfacture.ws.DescargaPDFCompletedEventArgs e)
        //{

        //    if (e.Result == "0")
        //    {
        //        throw new ArgumentException(string.Concat("No se puede descargar el PDF, verifique la numeración: tipo - serie - correlativo e intente nuevamente. ", e.UserState));
        //    }

        //    byte[] data = Convert.FromBase64String(e.Result);
        //    using (FileStream Writer = new System.IO.FileStream(e.UserState + ".pdf", FileMode.Create, FileAccess.Write))
        //    {
        //        Writer.Write(data, 0, data.Length);
        //    }

        //}
    }
}
