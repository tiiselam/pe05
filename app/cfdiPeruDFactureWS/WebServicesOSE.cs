using cfdiPeruInterfaces;
using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.Zip;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cfdiPeruOperadorServiciosElectronicos
{
    public class WebServicesOSE : ICfdiMetodosWebService
    {
        pe.dfacture.ws.MetodosClient Servicio = new pe.dfacture.ws.MetodosClient();
        public WebServicesOSE(string urlWebService)
        {
            Servicio.Url = urlWebService;
        }

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
                if (respuestaT.NumeroError.Equals("0016"))
                    throw new ArgumentException(respuestaT.NumeroError + " - " + respuestaT.MensajeError);
                else
                    throw new TimeoutException("Excepción al conectarse con el Web Service de Facturacion de DFacture. -" + respuestaT.NumeroError + "- " + respuestaT.MensajeError);
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
                throw new TimeoutException("Excepción al conectarse con el Web Service de Facturacion de DFactur-e. [ResumenDiario] " + respuestaT.NumeroError + " - " + respuestaT.MensajeError);
            }
            return Tuple.Create(respuestaT.Nroticketdeatencion, respuestaT.XmlResumen);

        }

        public string DescargaCDR(string ruc, string tipoDoc, string serie, string correlativo)
        {
            string serieCorrelativo = string.Concat(tipoDoc, "-", serie, "-", correlativo);
            string archivoCDR = Servicio.DescargaCDR(ruc, serieCorrelativo);
            if (archivoCDR == "0")
            {
                throw new ArgumentException(string.Concat("No se puede descargar el CDR del portal. Verifique la numeración: tipo - serie - correlativo: ", serieCorrelativo, " e intente nuevamente más tarde. "));
            }
            return archivoCDR;
        }

        public string ObtieneXMLdelOSE(string ruc, string tipoDoc, string serie, string correlativo)
        {
            string archivoXML = DescargaXML(ruc, tipoDoc, serie, correlativo);
            string docxml = string.Empty;
            var data = Convert.FromBase64String(archivoXML);
            docxml = System.Text.Encoding.UTF8.GetString(data);
            //using (var compressedStream = new MemoryStream(data))
            //{
            //    docxml = UnzipFromStream(compressedStream);
            //}

            return docxml;

        }

        public string DescargaXML(string ruc, string tipoDoc, string serie, string correlativo)
        {
            string serieCorrelativo = string.Concat(tipoDoc, "-", serie, "-", correlativo);
            string archivoXML = Servicio.DescargaXML(ruc, serieCorrelativo);
            if (archivoXML == "0")
            {
                throw new ArgumentException(string.Concat("No se puede descargar el XML del portal. Verifique la numeración: tipo - serie - correlativo: ", serieCorrelativo, " e intente nuevamente más tarde. "));
            }
            return archivoXML;
        }

        private string UnzipFromStream(Stream zipStream)
        {
            
            string documento = string.Empty;
            ZipInputStream zipInputStream = new ZipInputStream(zipStream);
            ZipEntry zipEntry = zipInputStream.GetNextEntry();
            while (zipEntry != null)
            {
                String entryFileName = zipEntry.Name;
                // to remove the folder from the entry:- entryFileName = Path.GetFileName(entryFileName);
                // Optionally match entrynames against a selection list here to skip as desired.
                // The unpacked length is available in the zipEntry.Size property.

                byte[] buffer = new byte[4096];     // 4K is optimum

                // Manipulate the output filename here as desired.
                //String fullZipToPath = Path.Combine(outFolder, entryFileName);
                //string directoryName = Path.GetDirectoryName(fullZipToPath);
                //if (directoryName.Length > 0)
                //    Directory.CreateDirectory(directoryName);

                // Skip directory entry
                //string fileName = Path.GetFileName(fullZipToPath);
                //if (fileName.Length == 0)
                //{
                //    zipEntry = zipInputStream.GetNextEntry();
                //    continue;
                //}

                // Unzip file in buffered chunks. This is just as fast as unpacking to a buffer the full size
                // of the file, but does not waste memory.
                // The "using" will close the stream even if an exception occurs.
                //using (FileStream streamWriter = File.Create(fullZipToPath))
                //{
                    using (var cdr = new MemoryStream())
                    {
                        StreamUtils.Copy(zipInputStream, cdr, buffer);
                        byte[] datos = cdr.ToArray();
                        //streamWriter.Seek(0, SeekOrigin.End);
                        //streamWriter.Write(datos, 0, datos.Length);

                        documento = Encoding.UTF8.GetString(datos);
                    }
                //}

                zipEntry = zipInputStream.GetNextEntry();
            }
            return documento;
        }

        public async Task<string> ObtieneCDRdelOSEAsync(string ruc, string tipoDoc, string serie, string correlativo, string rutaYNomArchivoCfdi) //string ruta, string nombreArchivo, string extension)
        {
            throw new NotImplementedException();

        }

        public string ObtieneCDRdelOSE(string ruc, string tipoDoc, string serie, string correlativo) 
        {
            string archivoCDR = DescargaCDR(ruc, tipoDoc, serie, correlativo);
            string cdr = string.Empty;
            byte[] data = Convert.FromBase64String(archivoCDR);

            using (var compressedStream = new MemoryStream(data))
            {
                cdr = UnzipFromStream(compressedStream);
            }

            return cdr;

        }

        public string DescargaPDF(string ruc, string tipoDoc, string serie, string correlativo)
        {
            string archivoPdf = "0";
            string serieCorrelativo = string.Empty;
            try
            {
                serieCorrelativo = string.Concat(tipoDoc, "-", serie, "-", correlativo);
                archivoPdf = Servicio.DescargaPDF(ruc, serieCorrelativo);
                if (archivoPdf == "0")
                {
                    throw new ArgumentException(string.Concat("No se puede descargar el PDF. Verifique la numeración: tipo - serie - correlativo: ", serieCorrelativo, " e intente nuevamente más tarde. "));
                }

            }
            catch (Exception)
            {
                throw new TimeoutException(string.Concat("Probable timeout. No se puede descargar el PDF. Puede descargarlo desde el portal."));
            }
            return archivoPdf;
        }

        public async Task<string> ObtienePDFdelOSEAsync(string ruc, string tipoDoc, string serie, string correlativo, string ruta, string nombreArchivo, string extension)
        {
            string rutaYNomArchivoCfdi = Path.Combine(ruta, nombreArchivo + extension);
            string archivoPdf = DescargaPDF(ruc, tipoDoc, serie, correlativo);

            byte[] data = Convert.FromBase64String(archivoPdf);
            using (FileStream SourceStream = File.Open(rutaYNomArchivoCfdi, FileMode.OpenOrCreate))
            {
                SourceStream.Seek(0, SeekOrigin.End);
                await SourceStream.WriteAsync(data, 0, data.Length);
            }

            return rutaYNomArchivoCfdi;

        }

        public Tuple<string, string> Baja(string ruc, string usuario, string usuarioPassword, string nroDocumento)
        {
            pe.dfacture.ws.Cancelacion cancela = new pe.dfacture.ws.Cancelacion();

            cancela = Servicio.ComunicacionBaja(ruc, usuario, usuarioPassword, nroDocumento);

            if (cancela.Procesado || cancela.NumeroError.Equals("95"))
            {
                Debug.WriteLine(" Baja solicitada con el número de ticket: " + cancela.Nroticketdeatencion);
            }
            else
            {
                throw new TimeoutException("Excepción al conectarse con el Web Service de Facturacion de DFactur-e. [Baja] " + cancela.NumeroError + " - " + cancela.MensajeError);
            }
            return Tuple.Create(cancela.Nroticketdeatencion, cancela?.XmlBaja);
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
