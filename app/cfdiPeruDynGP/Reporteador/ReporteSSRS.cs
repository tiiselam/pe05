using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using Comun;
using Reporteador.RepServerWSExec;
using System.Net;
using Microsoft.Reporting.WinForms;

namespace Reporteador
{
    public class ReporteSSRS
    {
        public string ultimoMensaje = "";
        public int numError = 0;
        private string _SrvrName = "";
        private string _DbName = "";
        private string _IdUsuario = "";
        private string _Password = "";
        private bool _IntegratedSecurity = false;
        private string _rutaYReporteSSRS = "";
        private List<PrmtrsReporte> _listaDefParamReporte;
        private Parametros _param;
        ExecutionInfo _ei;
        string _historyID = null;
        private ReportExecutionService _rsExec;
        //private rs2005.ReportingService2005 rs;

        public ReporteSSRS(ConexionAFuenteDatos Conexion, Parametros Param)
        {
            _SrvrName = Conexion.ServerAddress;
            _DbName = Conexion.Intercompany;
            _IdUsuario = Conexion.Usuario;
            _Password = Conexion.Password;
            _IntegratedSecurity = Conexion.IntegratedSecurity;
            _rutaYReporteSSRS = Param.rutaReporteSSRS;
            _listaDefParamReporte = Param.ListaParametrosRepSSRS;
            _param = Param;

            try
            {
                _rsExec = new ReportExecutionService();                          // Create a new proxy to the web service
                _rsExec.Credentials = CredentialCache.DefaultCredentials;        // Authenticate to the Web service using Windows credentials
                if (Param.reporteador.Equals("SSRS"))
                {
                    _rsExec.Url = Param.SSRSServer + "/ReportExecution2005.asmx";    // Assign the URL of the Web service
                    _ei = _rsExec.LoadReport(_rutaYReporteSSRS, _historyID);           // Load the selected report.
                }
            }
            catch (Exception Rp)
            {
                ultimoMensaje = "Error al inicializar el reporte [ReporteSSRS]" + Rp.Message;
                numError++;
            }

        }

        /// <summary>
        /// Guarda el archivo pdf usando Reporting Services. Si el archivo ya existe, devuelve error.
        /// </summary>
        /// <param name="ValoresParametros">Valores de los parámetros del reporte. El orden debe ser el mismo que el archivo de configuración ParámetrosCfd.xml.</param>
        /// <param name="RutaPDF">Ruta donde se guarda el pdf. Incluye el nombre del archivo.</param>
        /// <returns></returns>
        public void renderPDF(List<string> ValoresParametros, string RutaPDF)
        {
            //Stream outputFile = File.Create(@"C:\gpusuario\renderpdf.txt");
            //TextWriterTraceListener textListener = new TextWriterTraceListener(outputFile);
            //TraceSource trace = new TraceSource("trSource", SourceLevels.All);
            //trace.Listeners.Clear();
            //trace.Listeners.Add(textListener);
            //trace.TraceInformation("render pdf " + RutaPDF);

            ultimoMensaje = "";
            numError = 0;
            // Prepare Render arguments
            string deviceInfo = null;
            string format = "PDF";
            Byte[] results;
            string encoding = String.Empty;
            string mimeType = String.Empty;
            string extension = String.Empty;
            Reporteador.RepServerWSExec.Warning[] warnings = null;
            string[] streamIDs = null;

            try
            {
                // Get if any parameters needed.
                //_parameters = rs.GetReportParameters(_report, _historyID, _forRendering, _values, _credentials);

                // Prepare report parameter. Set the parameters for the report needed.
                ParameterValue[] parameters = new ParameterValue[_listaDefParamReporte.Count];
                int i = 0;
                foreach (PrmtrsReporte pr in _listaDefParamReporte)
                {
                    parameters[i] = new ParameterValue();
                    parameters[i].Name = pr.nombre.Trim();
                    parameters[i].Value = ValoresParametros[i];
                    i++;
                }

                _rsExec.SetExecutionParameters(parameters, "en-us");
                results = _rsExec.Render(format, deviceInfo, out extension, out encoding, out mimeType, out warnings, out streamIDs);

                if (File.Exists(RutaPDF))
                {
                    numError++;
                    ultimoMensaje = "El archivo pdf ya existe. Primero elimínelo y luego puede regenerarlo.[renderPDF]";
                }
                else
                {
                    // Create a file stream and write the report to it
                    using (FileStream stream = File.OpenWrite(RutaPDF))
                    {
                        stream.Write(results, 0, results.Length);
                    }

                }
            }
            catch (DirectoryNotFoundException)
            {
                ultimoMensaje = "Verifique la existencia de la ruta indicada en la configuración de Ruta de archivos Xml. [renderPDF] La ruta no pudo ser encontrada: " + RutaPDF;
                numError++;
            }
            catch (IOException)
            {
                ultimoMensaje = "Verifique permisos de escritura en: " + RutaPDF + ". No se pudo guardar el archivo xml ni registrar el documento en la bitácora. [renderPDF]";
                numError++;
            }
            catch (Exception exPdf)
            {
                if (exPdf.Message.Contains("denied"))
                    ultimoMensaje = "Elimine el archivo antes de volver a generar uno nuevo. Luego vuelva a intentar. [renderPDF] " + exPdf.Message;
                else
                    ultimoMensaje = "Advertencia. No se guardó el reporte PDF. [renderPDF] " + exPdf.Message;
                numError++;
            }
        }

        /// <summary>
        /// Muestra el reporte en pantalla usando Reporting Services en modo remoto.
        /// </summary>
        /// <param name="ssrsRepView">Visor del reporte.</param>
        /// <param name="ValoresParametros">Valores de los parámetros del reporte. El orden debe ser el mismo que el archivo de configuración ParámetrosCfd.xml.</param>
        /// <returns></returns>
        public void muestraEnVisor(ReportViewer ssrsRepView, List<string> ValoresParametros)
        {
            ultimoMensaje = "";
            numError = 0;
            try
            {
                ssrsRepView.Visible = true;
                ssrsRepView.Reset();
                ssrsRepView.ShowCredentialPrompts = false;
                ssrsRepView.ServerReport.ReportServerCredentials.NetworkCredentials = CredentialCache.DefaultCredentials;
                ssrsRepView.ServerReport.ReportServerUrl = new Uri(_param.SSRSServer);
                ssrsRepView.ServerReport.ReportPath = _param.rutaReporteSSRS;

                // Set the parameters for the report needs.
                int i = 0;
                List<Microsoft.Reporting.WinForms.ReportParameter> parameters = new List<Microsoft.Reporting.WinForms.ReportParameter>();
                foreach (PrmtrsReporte pr in _listaDefParamReporte)
                {
                    parameters.Add(new Microsoft.Reporting.WinForms.ReportParameter(pr.nombre.Trim(), ValoresParametros[i]));
                    i++;
                }

                ssrsRepView.ServerReport.SetParameters(parameters);
                ssrsRepView.ProcessingMode = ProcessingMode.Remote;
                ssrsRepView.ShowParameterPrompts = false;
                ssrsRepView.ShowPromptAreaButton = false;
                ssrsRepView.RefreshReport();
            }
            catch (Exception mv)
            {
                ultimoMensaje = "Error al mostrar el reporte en pantalla. [muestraEnVisor] " + mv.Message;
                numError++;
            }
        }

    }
}
