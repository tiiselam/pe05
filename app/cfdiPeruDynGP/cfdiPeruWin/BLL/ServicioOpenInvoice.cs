using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Windows.Forms;
using OpenInvoicePeru.Comun.Dto;
using OpenInvoicePeru.Comun.Dto.Modelos;
using OpenInvoicePeru.Comun.Dto.Intercambio;
using System.IO;

namespace cfd.FacturaElectronica
{
    public class ServicioOpenInvoice
    {
        private string _rutaArchivo;

        private DocumentoElectronico _docElectronico;

        public DocumentoElectronico DocElectronico
        {
            get
            {
                return _docElectronico;
            }

            set
            {
                _docElectronico = value;
            }
        }

        public string RutaArchivo
        {
            get
            {
                return _rutaArchivo;
            }

            set
            {
                _rutaArchivo = value;
            }
        }

        public async void GeneraCFDI()
        {
            try
            {
                Cursor.Current = Cursors.WaitCursor;

                var proxy = new HttpClient { BaseAddress = new Uri(ConfigurationManager.AppSettings["UrlOpenInvoicePeruApi"]) };

                string metodoApi;
                switch (_docElectronico.TipoDocumento)
                {
                    case "07":
                        metodoApi = "api/GenerarNotaCredito";
                        break;
                    case "08":
                        metodoApi = "api/GenerarNotaDebito";
                        break;
                    default:
                        metodoApi = "api/GenerarFactura";
                        break;
                }

                var response = await proxy.PostAsJsonAsync(metodoApi, _docElectronico);

                //response.EnsureSuccessStatusCode();
                var respuesta = await response.Content.ReadAsAsync<DocumentoResponse>();
                if (!respuesta.Exito)
                    throw new ApplicationException(respuesta.MensajeError);

                _rutaArchivo = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"{_docElectronico.IdDocumento}.xml");

                File.WriteAllBytes(RutaArchivo, Convert.FromBase64String(respuesta.TramaXmlSinFirma));

                //IdDocumento = _docElectronico.IdDocumento;

                //DialogResult = DialogResult.OK;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            finally
            {
                Cursor.Current = Cursors.Default;
            }

        }
    }
}
