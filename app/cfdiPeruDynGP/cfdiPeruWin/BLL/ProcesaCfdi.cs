using cfdiPeru;
using Comun;
using MaquinaDeEstados;
using OpenInvoicePeru.Comun.Dto.Intercambio;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using cfdiPeruInterfaces;
using System.Xml.Linq;

namespace cfd.FacturaElectronica
{
    public class ProcesaCfdi
    {
        private Parametros _Param;
        private ConexionAFuenteDatos _Conex;
        //private String tramaXmlFirmado;
        //private String tramaZipCdr;
        //private String nombreArchivoCdr;
        private String nroTicket=String.Empty;
        private String _mensajeSunat = String.Empty;
        //private bool _consultaCDRExito = false;
        //private String _codigoRespuesta;
        //private readonly HttpClient _client;

        public string ultimoMensaje = "";
        vwCfdTransaccionesDeVenta trxVenta;

        internal vwCfdTransaccionesDeVenta TrxVenta
        {
            get
            {
                return trxVenta;
            }

            set
            {
                trxVenta = value;
            }
        }
        public delegate void LogHandler(int iAvance, string sMsj);
        public event LogHandler Progreso;

        /// <summary>
        /// Dispara el evento para actualizar la barra de progreso
        /// </summary>
        /// <param name="iProgreso"></param>
        public void OnProgreso(int iAvance, string sMsj)
        {
            if (Progreso != null)
                Progreso(iAvance, sMsj);
        }

        public ProcesaCfdi(ConexionAFuenteDatos Conex, Parametros Param)
        {
            _Param = Param;
            _Conex = Conex;
            //_client = new HttpClient { BaseAddress = new Uri(ConfigurationManager.AppSettings["UrlOpenInvoicePeruApi"]) };

        }

        /// <summary>
        /// Genera documentos xml: factura, boleta, nc, nd
        /// </summary>
        public async Task GeneraDocumentoXmlAsync(ICfdiMetodosWebService servicioTimbre)
        {
            string xmlFactura = string.Empty;
            string rutaYNom = string.Empty;
            try
            {
                String msj = String.Empty;
                trxVenta.Rewind();                                                          //move to first record
                string leyendas = await vwCfdTransaccionesDeVenta.ObtieneLeyendasAsync();
                int errores = 0; int i = 1;
                cfdReglasFacturaXml DocVenta = new cfdReglasFacturaXml(_Conex, _Param);     //log de facturas xml emitidas y anuladas
                ReglasME maquina = new ReglasME(_Param);
                ValidadorXML validadorxml = new ValidadorXML(_Param);
                TransformerXML loader = new TransformerXML();
                OnProgreso(1, "INICIANDO EMISION DE COMPROBANTES DE VENTA...");
                do
                {
                    msj = "ok";
                    String accion = "EMITE XML Y PDF";
                    try
                    {
                        if (trxVenta.Estado.Equals("no emitido") &&
                            maquina.ValidaTransicion(_Param.tipoDoc, accion, trxVenta.EstadoActual) &&
                            trxVenta.EstadoContabilizado.Equals("contabilizado"))
                        { 
                            if (trxVenta.Voidstts == 0)  //documento no anulado
                            {
                                trxVenta.ArmarDocElectronico(leyendas);
                                String[] serieCorrelativo = trxVenta.DocGP.DocVenta.idDocumento.Split(new char[] { '-' });
                                string nombreArchivo = Utiles.FormatoNombreArchivo(trxVenta.Docid + trxVenta.Sopnumbe + "_" + trxVenta.s_CUSTNMBR, trxVenta.s_NombreCliente, 20) + "_" + accion.Substring(0, 2);
                                //validaciones
                                switch (trxVenta.DocGP.DocVenta.tipoDocumento)
                                {
                                    case "07":
                                        if (trxVenta.DocGP.LDocVentaRelacionados.Count() == 0)
                                        {
                                            msj = "La nota de crédito no está aplicada.";
                                            continue;
                                        }
                                        else
                                        {
                                            if (trxVenta.DocGP.LDocVentaRelacionados
                                                                        .Where(f => f.sopnumbeTo.Substring(0, 1) == trxVenta.DocGP.DocVenta.idDocumento.Substring(0, 1))
                                                                        .Count() 
                                                != trxVenta.DocGP.LDocVentaRelacionados.Count())
                                            {
                                                msj = "La serie de la nota de crédito y de la factura aplicada deben empezar con la misma letra: F o B.";
                                                continue;
                                            }
                                        }
                                        if (string.IsNullOrEmpty(trxVenta.DocGP.DocVenta.infoRelNotasCodigoTipoNota))
                                        {
                                            msj = "No ha informado la causa de la discrepancia en la nota de crédito.";
                                            continue;
                                        }
                                        if (string.IsNullOrEmpty(trxVenta.DocGP.DocVenta.tipoOperacion))
                                        {
                                            msj = "No ha informado el tipo de operación en la nota de crédito.";
                                            continue;
                                        }
                                        break;
                                    case "08":
                                        if (string.IsNullOrEmpty(trxVenta.DocGP.DocVenta.tipoOperacion))
                                        {
                                            msj = "No ha informado el tipo de operación en la nota de débito.";
                                            continue;
                                        }
                                        break;
                                    case "01":
                                        if (string.IsNullOrEmpty(trxVenta.DocGP.DocVenta.tipoOperacion))
                                        {
                                            msj = "No ha informado el tipo de operación en la factura.";
                                            continue;
                                        }
                                        break;
                                    case "03":
                                        if (string.IsNullOrEmpty(trxVenta.DocGP.DocVenta.tipoOperacion))
                                        {
                                            msj = "No ha informado el tipo de operación en la boleta.";
                                            continue;
                                        }
                                        break;
                                    default:
                                        msj = "No se puede emitir porque el tipo de documento: " + trxVenta.DocGP.DocVenta.tipoDocumento + " no está configurado.";
                                        throw new ApplicationException(msj);
                                }

                                string extension = ".xml";
                                rutaYNom = Path.Combine(trxVenta.RutaXml.Trim(), nombreArchivo + extension);
                                try
                                {
                                    xmlFactura = await servicioTimbre.TimbraYEnviaASunatAsync(trxVenta.DocGP.DocVenta.emisorNroDoc, trxVenta.Ruta_certificadoPac, trxVenta.Contrasenia_clavePac, trxVenta.DocGP);
                                    DocVenta.RegistraLogDeArchivoXML(trxVenta.Soptype, trxVenta.Sopnumbe, rutaYNom, "FAC", _Conex.Usuario, xmlFactura.Replace("encoding=\"utf-8\"", "").Replace("encoding=\"iso-8859-1\"", ""), maquina.DestinoStatusBase, maquina.DestinoEBinario, maquina.DestinoMensaje);

                                }
                                catch (ArgumentException ae)    //202 ó 207
                                {
                                    msj = ae.Message;
                                    xmlFactura = await servicioTimbre.ObtieneXMLdelOSEAsync(trxVenta.DocGP.DocVenta.emisorNroDoc, trxVenta.Ruta_certificadoPac, trxVenta.Contrasenia_clavePac, trxVenta.DocGP.DocVenta.tipoDocumento, serieCorrelativo[0], serieCorrelativo[1]);
                                    DocVenta.RegistraLogDeArchivoXML(trxVenta.Soptype, trxVenta.Sopnumbe, rutaYNom, "FAC", _Conex.Usuario, xmlFactura.Replace("encoding=\"utf-8\"", "").Replace("encoding=\"iso-8859-1\"", ""), maquina.DestinoStatusBase, maquina.DestinoEBinario, maquina.DestinoMensaje);
                                }
                                catch(XmlException xm)
                                {
                                    msj = "Verifique la configuración de leyendas para la impresión PDF. [GeneraDocumentoXmlAsync] " + xm.Message + Environment.NewLine + xm.StackTrace;
                                    DocVenta.RegistraLogDeArchivoXML(trxVenta.Soptype, trxVenta.Sopnumbe, "GeneraDocumentoXmlAsync " + xm.Message, "errLeyendas", _Conex.Usuario, string.Empty, "error", maquina.DestinoEBinario, xm.StackTrace);
                                    errores++;
                                }
                                catch (Exception lo)
                                {
                                    msj = "[GeneraDocumentoXmlAsync] " + lo.Message + Environment.NewLine + lo.StackTrace;
                                    DocVenta.RegistraLogDeArchivoXML(trxVenta.Soptype, trxVenta.Sopnumbe, "GeneraDocumentoXmlAsync " + lo.Message, "errDFacture", _Conex.Usuario, string.Empty, "error", maquina.DestinoEBinario, lo.StackTrace);
                                    errores++;
                                }

                                if (!string.IsNullOrEmpty(xmlFactura))
                                {
                                    rutaYNom = await DocVenta.GuardaArchivoAsync(trxVenta, xmlFactura, nombreArchivo, extension, false);
                                    string tPdf = await servicioTimbre.ObtienePDFdelOSEAsync(trxVenta.DocGP.DocVenta.emisorNroDoc,  trxVenta.Ruta_certificadoPac, trxVenta.Contrasenia_clavePac, trxVenta.DocGP.DocVenta.tipoDocumento, serieCorrelativo[0], serieCorrelativo[1], trxVenta.RutaXml.Trim(), nombreArchivo, ".pdf");
                                }
                            }
                            else //si el documento está anulado en gp, agregar al log como emitido
                            {
                                maquina.ValidaTransicion("FACTURA", "ANULA VENTA", trxVenta.EstadoActual, "emitido");
                                msj = "Anulado en GP y marcado como emitido.";
                                OnProgreso(1, msj);
                                DocVenta.RegistraLogDeArchivoXML(trxVenta.Soptype, trxVenta.Sopnumbe, "Anulado en GP", "0", _Conex.Usuario, "", "emitido", maquina.eBinarioNuevo, msj.Trim());
                            }
                        }
                    }
                    catch (HttpRequestException he)
                    {
                        msj = string.Concat(he.Message, Environment.NewLine, he.StackTrace);
                        errores++;
                    }
                    catch (ArgumentException ae)
                    {
                        msj = ae.Message + Environment.NewLine + ae.StackTrace;
                        errores++;
                    }
                    catch (TimeoutException ae)
                    {
                        string imsj = ae.InnerException == null ? "" : ae.InnerException.ToString();
                        msj = ae.Message + " " + imsj + Environment.NewLine + ae.StackTrace;
                        errores++;
                    }
                    catch (DirectoryNotFoundException dnf)
                    {
                        msj = "El comprobante fue emitido, pero no se pudo guardar el archivo en: " + trxVenta.Ruta_clave + " Verifique si existe la carpeta." + Environment.NewLine;
                        DocVenta.RegistraLogDeArchivoXML(trxVenta.Soptype, trxVenta.Sopnumbe, msj, "errCarpeta", _Conex.Usuario, string.Empty, "error", maquina.DestinoEBinario, dnf.Message);
                        msj += dnf.Message + Environment.NewLine;
                        errores++;
                    }
                    catch (IOException io)
                    {
                        msj = "El comprobante fue emitido, pero no se pudo guardar el archivo en: " + trxVenta.Ruta_clave + " Verifique permisos a la carpeta." + Environment.NewLine;
                        DocVenta.RegistraLogDeArchivoXML(trxVenta.Soptype, trxVenta.Sopnumbe, msj, "errIO", _Conex.Usuario, string.Empty, "error", maquina.DestinoEBinario, io.Message);
                        msj += io.Message + Environment.NewLine;
                        errores++;
                    }
                    catch (Exception lo)
                    {
                        string imsj = lo.InnerException == null ? "" : lo.InnerException.ToString();
                        msj = lo.Message + " " + imsj + Environment.NewLine + lo.StackTrace;
                        DocVenta.RegistraLogDeArchivoXML(trxVenta.Soptype, trxVenta.Sopnumbe, lo.Message, "errDesconocido", _Conex.Usuario, string.Empty, "error", maquina.DestinoEBinario, lo.StackTrace);
                        errores++;
                    }
                    finally
                    {
                        OnProgreso(i * 100 / trxVenta.RowCount, "Doc:" + trxVenta.Sopnumbe + " " + msj.Trim() + Environment.NewLine);              //Notifica al suscriptor
                        i++;
                    }
                } while (trxVenta.MoveNext() && errores < 10);
            }
            catch (Exception xw)
            {
                string imsj = xw.InnerException == null ? "" : xw.InnerException.ToString();
                this.ultimoMensaje = xw.Message + " " + imsj + Environment.NewLine + xw.StackTrace;
            }
            finally
            {
                OnProgreso(100, ultimoMensaje);
            }
            OnProgreso(100, "Proceso finalizado!");
        }

        /// <summary>
        /// Ejecuta la generación de documentos xml: factura, boleta, nc, nd
        /// </summary>
        //public async Task GeneraDocumentoXmlAsync(ICfdiMetodosWebService servicioTimbre, ICfdiPeruDocumento servicioEstructuraDoc)
        //{
        //    string xmlFactura = string.Empty;
        //    string rutaYNom = string.Empty;
        //    //Task t = null;
        //    try
        //    {
        //        String msj = String.Empty;
        //        trxVenta.Rewind();                                                          //move to first record

        //        int errores = 0; int i = 1;
        //        cfdReglasFacturaXml DocVenta = new cfdReglasFacturaXml(_Conex, _Param);     //log de facturas xml emitidas y anuladas
        //        ReglasME maquina = new ReglasME(_Param);
        //        ValidadorXML validadorxml = new ValidadorXML(_Param);
        //        TransformerXML loader = new TransformerXML();
        //        OnProgreso(1, "INICIANDO EMISION DE COMPROBANTES DE VENTA...");             //Notifica al suscriptor
        //        do
        //        {
        //            msj = String.Empty;
        //            String accion = "EMITE XML Y PDF";
        //            try
        //            {
        //                if (trxVenta.Estado.Equals("no emitido") &&
        //                    maquina.ValidaTransicion(_Param.tipoDoc, accion, trxVenta.EstadoActual) &&
        //                    trxVenta.EstadoContabilizado.Equals("contabilizado"))
        //                    if (trxVenta.Voidstts == 0)  //documento no anulado
        //                    {
        //                        trxVenta.ArmarDocElectronico();
        //                        String[] serieCorrelativo = trxVenta.DocElectronico.IdDocumento.Split(new char[] { '-' });
        //                        string nombreArchivo = Utiles.FormatoNombreArchivo(trxVenta.Docid + trxVenta.Sopnumbe + "_" + trxVenta.s_CUSTNMBR, trxVenta.s_NombreCliente, 20) + "_" + accion.Substring(0, 2);

        //                        //validaciones
        //                        switch (trxVenta.DocElectronico.TipoDocumento)
        //                        {
        //                            case "07":
        //                                if (trxVenta.DocElectronico.Relacionados.Count() == 0)
        //                                {
        //                                    msj = "La nota de crédito no está aplicada.";
        //                                    continue;
        //                                }
        //                                else
        //                                {
        //                                    if (trxVenta.DocElectronico.Relacionados
        //                                                                .Where(f => f.NroDocumento.Substring(0, 1) == trxVenta.DocElectronico.IdDocumento.Substring(0, 1)).Count() != trxVenta.DocElectronico.Relacionados.Count())
        //                                    {
        //                                        msj = "La serie de la nota de crédito y de la factura aplicada deben empezar con la misma letra: F o B.";
        //                                        continue;
        //                                    }
        //                                }
        //                                if (trxVenta.DocElectronico.Discrepancias.Count() == 0)
        //                                {
        //                                    msj = "No ha informado la causa de la discrepancia en la nota de crédito.";
        //                                    continue;
        //                                }

        //                                break;
        //                            case "08":
        //                                msj = "ok";
        //                                break;
        //                            case "01":
        //                                msj = "ok";
        //                                break;
        //                            case "03":
        //                                msj = "ok";
        //                                break;
        //                            default:
        //                                msj = "No se puede emitir porque el tipo de documento: " + trxVenta.DocElectronico.TipoDocumento + " no está configurado.";
        //                                throw new ApplicationException(msj);
        //                        }

        //                        string tramaDocumento = servicioEstructuraDoc.FormatearDocElectronico(trxVenta.DocElectronico.TipoDocumento, trxVenta.DocElectronico);
        //                        string extension = ".xml";
        //                        rutaYNom = Path.Combine(trxVenta.RutaXml.Trim(), nombreArchivo + extension);
        //                        try
        //                        {
        //                            xmlFactura = servicioTimbre.TimbraYEnviaASunat(trxVenta.DocElectronico.Emisor.NroDocumento, trxVenta.Ruta_certificadoPac, trxVenta.Contrasenia_clavePac, tramaDocumento);
        //                            DocVenta.RegistraLogDeArchivoXML(trxVenta.Soptype, trxVenta.Sopnumbe, rutaYNom, "FAC", _Conex.Usuario, xmlFactura.Replace("encoding=\"utf-8\"", "").Replace("encoding=\"ISO-8859-1\"", ""), maquina.DestinoStatusBase, maquina.DestinoEBinario, maquina.DestinoMensaje);
        //                        }
        //                        catch (ArgumentException ae)
        //                        {
        //                            msj = ae.Message;
        //                            xmlFactura = servicioTimbre.ObtieneXMLdelOSE(trxVenta.DocElectronico.Emisor.NroDocumento, trxVenta.DocElectronico.TipoDocumento, serieCorrelativo[0], serieCorrelativo[1]);
        //                            DocVenta.RegistraLogDeArchivoXML(trxVenta.Soptype, trxVenta.Sopnumbe, rutaYNom, "FAC", _Conex.Usuario, xmlFactura.Replace("encoding=\"utf-8\"", "").Replace("encoding=\"ISO-8859-1\"", ""), maquina.DestinoStatusBase, maquina.DestinoEBinario, maquina.DestinoMensaje);
        //                        }
        //                        catch (Exception lo)
        //                        {
        //                            msj = "GeneraDocumentoXmlAsync " + lo.Message + Environment.NewLine + lo.StackTrace;
        //                            DocVenta.RegistraLogDeArchivoXML(trxVenta.Soptype, trxVenta.Sopnumbe, "GeneraDocumentoXmlAsync " + lo.Message, "errDFacture", _Conex.Usuario, string.Empty, "error", maquina.DestinoEBinario, lo.StackTrace);
        //                            errores++;
        //                        }

        //                        if (!string.IsNullOrEmpty(xmlFactura))
        //                        {
        //                            rutaYNom = await DocVenta.GuardaArchivoAsync(trxVenta, xmlFactura, nombreArchivo, extension, false);
        //                            var tPdf = await servicioTimbre.ObtienePDFdelOSEAsync(trxVenta.DocElectronico.Emisor.NroDocumento, trxVenta.DocElectronico.TipoDocumento, serieCorrelativo[0], serieCorrelativo[1], trxVenta.RutaXml.Trim(), nombreArchivo, ".pdf");
        //                        }
        //                    }
        //                    else //si el documento está anulado en gp, agregar al log como emitido
        //                    {
        //                        maquina.ValidaTransicion("FACTURA", "ANULA VENTA", trxVenta.EstadoActual, "emitido");
        //                        msj = "Anulado en GP y marcado como emitido.";
        //                        DocVenta.RegistraLogDeArchivoXML(trxVenta.Soptype, trxVenta.Sopnumbe, "Anulado en GP", "0", _Conex.Usuario, "", "emitido", maquina.eBinarioNuevo, msj.Trim());
        //                    }
        //            }
        //            catch (HttpRequestException he)
        //            {
        //                msj = string.Concat(he.Message, Environment.NewLine, he.StackTrace);
        //                errores++;
        //            }
        //            catch (ArgumentException ae)
        //            {
        //                msj = ae.Message + Environment.NewLine + ae.StackTrace;
        //                errores++;
        //            }
        //            catch (TimeoutException ae)
        //            {
        //                string imsj = ae.InnerException == null ? "" : ae.InnerException.ToString();
        //                msj = ae.Message + " " + imsj + Environment.NewLine + ae.StackTrace;
        //                errores++;
        //            }
        //            catch (DirectoryNotFoundException dnf)
        //            {
        //                msj = "El comprobante fue emitido, pero no se pudo guardar el archivo en: " + trxVenta.Ruta_clave + " Verifique si existe la carpeta." + Environment.NewLine;
        //                DocVenta.RegistraLogDeArchivoXML(trxVenta.Soptype, trxVenta.Sopnumbe, msj, "errCarpeta", _Conex.Usuario, string.Empty, "error", maquina.DestinoEBinario, dnf.Message);
        //                msj += dnf.Message + Environment.NewLine;
        //                errores++;
        //            }
        //            catch (IOException io)
        //            {
        //                msj = "El comprobante fue emitido, pero no se pudo guardar el archivo en: " + trxVenta.Ruta_clave + " Verifique permisos a la carpeta." + Environment.NewLine;
        //                DocVenta.RegistraLogDeArchivoXML(trxVenta.Soptype, trxVenta.Sopnumbe, msj, "errIO", _Conex.Usuario, string.Empty, "error", maquina.DestinoEBinario, io.Message);
        //                msj += io.Message + Environment.NewLine; 
        //                errores++;
        //            }
        //            catch (Exception lo)
        //            {
        //                string imsj = lo.InnerException == null ? "" : lo.InnerException.ToString();
        //                msj = lo.Message + " " + imsj + Environment.NewLine + lo.StackTrace;
        //                DocVenta.RegistraLogDeArchivoXML(trxVenta.Soptype, trxVenta.Sopnumbe, lo.Message, "errDesconocido", _Conex.Usuario, string.Empty, "error", maquina.DestinoEBinario, lo.StackTrace);
        //                errores++;
        //            }
        //            finally
        //            {
        //                OnProgreso(i * 100 / trxVenta.RowCount, "Doc:" + trxVenta.Sopnumbe + " " + msj.Trim() + Environment.NewLine);              //Notifica al suscriptor
        //                i++;
        //            }
        //        } while (trxVenta.MoveNext() && errores < 10);
        //        //Task.WaitAll(t);
        //    }
        //    catch (Exception xw)
        //    {
        //        string imsj = xw.InnerException == null ? "" : xw.InnerException.ToString();
        //        this.ultimoMensaje = xw.Message + " " + imsj + Environment.NewLine + xw.StackTrace;
        //    }
        //    finally
        //    {
        //        OnProgreso(100, ultimoMensaje);
        //    }
        //    OnProgreso(100, "Proceso finalizado!");
        //}

        /// <summary>
        /// Ejecuta la generación del resumen de boletas
        /// </summary>
        //public async Task GeneraResumenXmlAsync(ICfdiMetodosWebService servicioTimbre, ICfdiPeruDocumento servicioEstructuraDoc)
        //{
        //    try
        //    {
        //        String msj = String.Empty;
        //        trxVenta.Rewind();                                                          //move to first record

        //        int errores = 0; int i = 1;
        //        cfdReglasFacturaXml DocVenta = new cfdReglasFacturaXml(_Conex, _Param);     //log de facturas xml emitidas y anuladas
        //        ReglasME maquina = new ReglasME(_Param);

        //        OnProgreso(1, "INICIANDO ENVIO DE RESUMEN...");              //Notifica al suscriptor
        //        do
        //        {
        //            msj = String.Empty;
        //            try
        //            {
        //                String accion = "ENVIA RESUMEN";
        //                if (maquina.ValidaTransicion("RESUMEN", accion, trxVenta.EstadoActual))
        //                    if (trxVenta.Voidstts == 0)  //documento no anulado
        //                    {
        //                        trxVenta.ArmarResumenElectronico();

        //                        var cAgrupados = trxVenta.ResumenElectronico.Resumenes.GroupBy(y => y.IdDocumento, (key, num) => new { id = key, cantidad = num.Count() });
        //                        var c = cAgrupados.Where(q => q.cantidad > 1);
        //                        if (c.Count() > 0)
        //                            throw new ApplicationException("La siguiente nota de crédito o débito aplica a más de un comprobante: " + c.First().id + " Ingrese a GP y aplique un solo comprobante.");

        //                        string nombreArchivo = Utiles.FormatoNombreArchivo(trxVenta.Docid + trxVenta.Sopnumbe + "_" + trxVenta.s_CUSTNMBR, trxVenta.s_NombreCliente, 20) + "_" + accion.Substring(0, 2);

        //                        string tramaResumen = servicioEstructuraDoc.FormatearResumenElectronico(string.Empty, trxVenta.ResumenElectronico);

        //                        var resultado = servicioTimbre.ResumenDiario(trxVenta.ResumenElectronico.Emisor.NroDocumento, trxVenta.Ruta_certificadoPac, trxVenta.Contrasenia_clavePac, tramaResumen);

        //                        var rutaYNom = await DocVenta.GuardaArchivoAsync(trxVenta, resultado.Item2, nombreArchivo, ".xml", false);

        //                        DocVenta.RegistraLogDeArchivoXML(trxVenta.Soptype, trxVenta.Sopnumbe, rutaYNom, resultado.Item1, _Conex.Usuario, resultado.Item2.Replace("encoding=\"utf-8\"", "").Replace("encoding=\"ISO-8859-1\"", ""), maquina.DestinoStatusBase, maquina.DestinoEBinario, maquina.DestinoMensaje);

        //                    }
        //            }
        //            catch (HttpRequestException he)
        //            {
        //                msj = string.Concat(he.Message, Environment.NewLine, he.StackTrace);
        //                errores++;
        //            }
        //            catch (ApplicationException ae)
        //            {
        //                msj = ae.Message + Environment.NewLine + ae.StackTrace;
        //                errores++;
        //            }
        //            catch (IOException io)
        //            {
        //                msj = "Excepción al revisar la carpeta/archivo: " + trxVenta.Ruta_clave + " Verifique su existencia y privilegios." + Environment.NewLine + io.Message + Environment.NewLine;
        //                errores++;
        //            }
        //            catch (Exception lo)
        //            {
        //                string imsj = lo.InnerException == null ? "" : lo.InnerException.ToString();
        //                msj = lo.Message + " " + imsj + Environment.NewLine + lo.StackTrace;
        //                errores++;
        //            }
        //            finally
        //            {
        //                OnProgreso(i * 100 / trxVenta.RowCount, "Doc:" + trxVenta.Sopnumbe + " " + msj.Trim() + " " + maquina.ultimoMensaje + Environment.NewLine);              //Notifica al suscriptor
        //                i++;
        //            }
        //        } while (trxVenta.MoveNext() && errores < 10);
        //    }
        //    catch (Exception xw)
        //    {
        //        string imsj = xw.InnerException == null ? "" : xw.InnerException.ToString();
        //        this.ultimoMensaje = xw.Message + " " + imsj + Environment.NewLine + xw.StackTrace;
        //    }
        //    finally
        //    {
        //        OnProgreso(100, ultimoMensaje);
        //    }
        //    OnProgreso(100, "Proceso finalizado!");
        //}

        public async Task<string> ProcesaConsultaStatusAsync(ICfdiMetodosWebService servicioTimbre)
        {
            string resultadoSunat = string.Empty;
            try
            {
                String msj = String.Empty;
                String eBinario = String.Empty;
                trxVenta.Rewind();                                                          //move to first record

                int errores = 0;
                int i = 1;
                cfdReglasFacturaXml DocVenta = new cfdReglasFacturaXml(_Conex, _Param);     //log de facturas xml emitidas y anuladas
                ReglasME maquina = new ReglasME(_Param);
                String accion = "CONSULTA STATUS";

                OnProgreso(1, "INICIANDO CONSULTA DE STATUS...");              //Notifica al suscriptor
                do
                {
                    msj = String.Empty;
                    String claseDocumento = !trxVenta.Docid.Equals("RESUMEN") ? _Param.tipoDoc : trxVenta.Docid;
                    try
                    {
                        String[] serieCorrelativo = trxVenta.Sopnumbe.Split(new char[] { '-' });

                        if (maquina.ValidaTransicion(claseDocumento, accion, trxVenta.EstadoActual))
                            if (trxVenta.Voidstts == 0 && trxVenta.EstadoContabilizado.Equals("contabilizado"))  //documento no anulado
                            {
                                string tipoDoc = string.Empty;
                                string serie = string.Empty;
                                string correlativo = string.Empty;

                                trxVenta.ArmarDocElectronico(string.Empty);
                                tipoDoc = trxVenta.DocGP.DocVenta.tipoDocumento;
                                serie = serieCorrelativo[0];
                                correlativo = serieCorrelativo[1];

                                resultadoSunat = await servicioTimbre.ConsultaStatusAlOSEAsync(trxVenta.DocGP.DocVenta.emisorNroDoc, trxVenta.Ruta_certificadoPac, trxVenta.Contrasenia_clavePac, tipoDoc, serie, correlativo);
                                String[] codigoYMensaje = resultadoSunat.Split(new char[] { '-' });
                                maquina.DestinoAceptado = codigoYMensaje[0] == "0" ? true : false;
                                maquina.ActualizarNodoDestinoStatusBase();
                                DocVenta.RegistraLogDeArchivoXML(trxVenta.Soptype, trxVenta.Sopnumbe, codigoYMensaje[1], codigoYMensaje[0], _Conex.Usuario, accion, maquina.DestinoStatusBase, maquina.DestinoEBinario, accion + ":" + codigoYMensaje[0]);

                                if (codigoYMensaje[0].Equals("0") || int.Parse(codigoYMensaje[0])>1000)
                                {
                                    DocVenta.ActualizaFacturaEmitida(trxVenta.Soptype, trxVenta.Sopnumbe, _Conex.Usuario, "emitido", "emitido", maquina.DestinoEBinario, maquina.DestinoMensaje, codigoYMensaje[0]);
                                }
                                msj = "Mensaje del OCE: " + resultadoSunat;

                            }
                    }
                    catch (Exception lo)
                    {
                        string imsj = lo.InnerException == null ? "" : lo.InnerException.ToString();
                        msj = lo.Message + " " + imsj + Environment.NewLine + lo.StackTrace;
                        errores++;
                    }
                    finally
                    {
                        OnProgreso(i * 100 / trxVenta.RowCount, "Doc:" + trxVenta.Sopnumbe + " " + msj.Trim() + " " + maquina.ultimoMensaje + Environment.NewLine);              //Notifica al suscriptor
                        i++;
                    }
                } while (trxVenta.MoveNext() && errores < 10);
            }
            catch (Exception xw)
            {
                string imsj = xw.InnerException == null ? "" : xw.InnerException.ToString();
                this.ultimoMensaje = xw.Message + " " + imsj + Environment.NewLine + xw.StackTrace;
            }
            finally
            {
                OnProgreso(100, ultimoMensaje);
            }
            OnProgreso(100, "PROCESO FINALIZADO!");
            return resultadoSunat;
        }

        //public async Task ProcesaConsultaCDRAsync(ICfdiMetodosWebService servicioTimbre)
        //{
        //    try
        //    {
        //        String msj = String.Empty;
        //        String eBinario = String.Empty;
        //        trxVenta.Rewind();                                                          //move to first record

        //        int errores = 0;
        //        int i = 1;
        //        cfdReglasFacturaXml DocVenta = new cfdReglasFacturaXml(_Conex, _Param);     //log de facturas xml emitidas y anuladas
        //        ReglasME maquina = new ReglasME(_Param);
        //        String accion = "CONSULTA CDR";

        //        OnProgreso(1, "INICIANDO CONSULTA DE CDRs...");              //Notifica al suscriptor
        //        do
        //        {
        //            msj = String.Empty;
        //            String rutaNombreCDR = String.Empty;
        //            String ticket = trxVenta.Regimen;
        //            String claseDocumento = !trxVenta.Docid.Equals("RESUMEN") ? _Param.tipoDoc : trxVenta.Docid;
        //            try
        //            {
        //                String[] serieCorrelativo = trxVenta.Sopnumbe.Split(new char[] { '-' });

        //                string nombreArchivo = Utiles.FormatoNombreArchivo(trxVenta.Docid + trxVenta.Sopnumbe + "_" + trxVenta.s_CUSTNMBR, trxVenta.s_NombreCliente, 20) + "_CDR_";
                        
        //                if (maquina.ValidaTransicion(claseDocumento, accion, trxVenta.EstadoActual))
        //                    if (trxVenta.Voidstts == 0 && trxVenta.EstadoContabilizado.Equals("contabilizado"))  //documento no anulado
        //                    {
        //                        string tipoDoc = string.Empty;
        //                        string serie = string.Empty;
        //                        string correlativo = string.Empty;
        //                        if (!trxVenta.Docid.Equals("RESUMEN"))
        //                        {
        //                            trxVenta.ArmarDocElectronico(string.Empty);
        //                            tipoDoc = trxVenta.DocGP.DocVenta.tipoDocumento;
        //                            serie = serieCorrelativo[0];
        //                            correlativo = serieCorrelativo[1];
        //                        }
        //                        else
        //                        {
        //                            tipoDoc = serieCorrelativo[0];
        //                            serie = serieCorrelativo[1];
        //                            correlativo = serieCorrelativo[2];
        //                        }
        //                        var cdr = servicioTimbre.ObtieneCDRdelOSE(trxVenta.Rfc, tipoDoc, serie, correlativo);

        //                        var resCdr = ResultadoCDR(claseDocumento, cdr);
        //                        maquina.DestinoAceptado = resCdr.Item1;
        //                        rutaNombreCDR = Path.Combine(trxVenta.RutaXml.Trim(), nombreArchivo + maquina.DestinoStatusBase +".xml");
        //                        var rutaYNom = await DocVenta.GuardaArchivoAsync(trxVenta, cdr, nombreArchivo, maquina.DestinoStatusBase + ".xml", false);

        //                        DocVenta.RegistraLogDeArchivoXML(trxVenta.Soptype, trxVenta.Sopnumbe, rutaNombreCDR, ticket, _Conex.Usuario, accion, maquina.DestinoStatusBase, maquina.DestinoEBinario, accion+":"+resCdr.Item3);
        //                        DocVenta.ActualizaFacturaEmitida(trxVenta.Soptype, trxVenta.Sopnumbe, _Conex.Usuario, "emitido", "emitido", maquina.DestinoEBinario, maquina.DestinoMensaje, ticket);

        //                    }
        //            }
        //            catch (ArgumentException ae)
        //            {
        //                msj = ae.Message + Environment.NewLine;
        //                errores++;
        //            }
        //            catch (IOException io)
        //            {
        //                msj = "Excepción al revisar la carpeta/archivo: " + trxVenta.Ruta_clave + " Verifique su existencia y privilegios." + Environment.NewLine + io.Message + Environment.NewLine;
        //                errores++;
        //            }
        //            catch (Exception lo)
        //            {
        //                string imsj = lo.InnerException == null ? "" : lo.InnerException.ToString();
        //                msj = lo.Message + " " + imsj + Environment.NewLine + lo.StackTrace;
        //                errores++;
        //            }
        //            finally
        //            {
        //                OnProgreso(i * 100 / trxVenta.RowCount, "Doc:" + trxVenta.Sopnumbe + " " + msj.Trim() + " " + maquina.ultimoMensaje + Environment.NewLine);              //Notifica al suscriptor
        //                i++;
        //            }
        //        } while (trxVenta.MoveNext() && errores < 10);
        //    }
        //    catch (Exception xw)
        //    {
        //        string imsj = xw.InnerException == null ? "" : xw.InnerException.ToString();
        //        this.ultimoMensaje = xw.Message + " " + imsj + Environment.NewLine + xw.StackTrace;
        //    }
        //    finally
        //    {
        //        OnProgreso(100, ultimoMensaje);
        //    }
        //    OnProgreso(100, "PROCESO FINALIZADO!");
        //}

        private Tuple<bool, string, string> ResultadoCDR(string tipoDocumento, string cdr)
        {
            var xmlCdr = XElement.Parse(cdr);
            XNamespace nscac = "urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2";
            XNamespace nscbc = "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2";
            var responseCode = xmlCdr?.Elements(nscac + "DocumentResponse")?.Elements(nscac + "Response")?.Elements(nscbc + "ResponseCode")?.First().Value;
            var description = xmlCdr?.Elements(nscac + "DocumentResponse")?.Elements(nscac + "Response")?.Elements(nscbc + "Description")?.First().Value;
            //var refId = xmlCdr?.Elements(nscac + "DocumentResponse")?.Elements(nscac + "Response")?.Elements(nscbc + "ReferenceID")?.First().Value;

            if (string.IsNullOrEmpty(responseCode))
            {
                throw new ArgumentException("El archivo de respuesta no corresponde a un cdr. ");
            }

            return Tuple.Create(responseCode.Equals("0"), responseCode, description);
        }

        public async Task ProcesaObtienePDFAsync(ICfdiMetodosWebService servicioTimbre)
        {
            try
            {
                String msj = String.Empty;
                String eBinario = String.Empty;
                trxVenta.Rewind();                                                          //move to first record

                int errores = 0;
                int i = 1;
                cfdReglasFacturaXml DocVenta = new cfdReglasFacturaXml(_Conex, _Param);     //log de facturas xml emitidas y anuladas
                ReglasME maquina = new ReglasME(_Param);
                String accion = "IMPRIME PDF";

                OnProgreso(1, "INICIANDO CONSULTA DE PDFs...");              //Notifica al suscriptor
                do
                {
                    msj = String.Empty;
                    String rutaNombrePDF = String.Empty;
                    String ticket = trxVenta.Regimen;
                    try
                    {
                        String[] serieCorrelativo = trxVenta.Sopnumbe.Split(new char[] { '-' });
                        string nombreArchivo = Utiles.FormatoNombreArchivo(trxVenta.Docid + trxVenta.Sopnumbe + "_" + trxVenta.s_CUSTNMBR, trxVenta.s_NombreCliente, 20) + "_CDR_" + accion.Substring(0, 2);

                        if (maquina.ValidaTransicion(_Param.tipoDoc, accion, trxVenta.EstadoActual))
                            if (trxVenta.Voidstts == 0 && trxVenta.EstadoContabilizado.Equals("contabilizado"))  //no anulado y contabilizado
                            {
                                trxVenta.ArmarDocElectronico(string.Empty);
                                rutaNombrePDF = await servicioTimbre.ObtienePDFdelOSEAsync(trxVenta.Rfc, trxVenta.Ruta_certificadoPac, trxVenta.Contrasenia_clavePac, trxVenta.DocGP.DocVenta.tipoDocumento, serieCorrelativo[0], serieCorrelativo[1], trxVenta.RutaXml.Trim(), nombreArchivo, ".pdf");
                                DocVenta.RegistraLogDeArchivoXML(trxVenta.Soptype, trxVenta.Sopnumbe, rutaNombrePDF, ticket, _Conex.Usuario, accion, maquina.DestinoStatusBase, maquina.DestinoEBinario, maquina.DestinoMensaje);

                                DocVenta.ActualizaFacturaEmitida(trxVenta.Soptype, trxVenta.Sopnumbe, _Conex.Usuario, "emitido", "emitido", maquina.DestinoEBinario, maquina.DestinoMensaje, ticket);
                            }
                            else
                                msj = "No se puede generar porque no está Contabilizado o está Anulado.";

                    }
                    catch (ArgumentException ae)
                    {
                        msj = ae.Message + Environment.NewLine ;
                        //DocVenta.LogDocumento(trxVenta, msj, maquina, ticket, _Param.tipoDoc, accion, false, rutaNombrePDF);
                        //DocVenta.ActualizaFacturaEmitida(trxVenta.Soptype, trxVenta.Sopnumbe, _Conex.Usuario, "emitido", "emitido", maquina.eBinActualConError, maquina.EnLetras(maquina.eBinActualConError, _Param.tipoDoc), ticket);
                        errores++;
                    }
                    catch (IOException io)
                    {
                        msj = "Excepción al revisar la carpeta/archivo: " + trxVenta.Ruta_clave + " Verifique su existencia y privilegios." + Environment.NewLine + io.Message + Environment.NewLine;
                        errores++;
                    }
                    catch (Exception lo)
                    {
                        string imsj = lo.InnerException == null ? "" : lo.InnerException.ToString();
                        msj = lo.Message + " " + imsj + Environment.NewLine + lo.StackTrace;
                        errores++;
                    }
                    finally
                    {
                        OnProgreso(i * 100 / trxVenta.RowCount, "Doc:" + trxVenta.Sopnumbe + " " + msj.Trim() + " " + maquina.ultimoMensaje + Environment.NewLine);              //Notifica al suscriptor
                        i++;
                    }
                } while (trxVenta.MoveNext() && errores < 10);
            }
            catch (Exception xw)
            {
                string imsj = xw.InnerException == null ? "" : xw.InnerException.ToString();
                this.ultimoMensaje = xw.Message + " " + imsj + Environment.NewLine + xw.StackTrace;
            }
            finally
            {
                OnProgreso(100, ultimoMensaje);
            }
            OnProgreso(100, "PROCESO FINALIZADO!");
        }

        public async Task ProcesaBajaComprobanteAsync(String motivoBaja, ICfdiMetodosWebService servicioTimbre)
        {
            try
            {
                String msj = String.Empty;
                String eBinario = String.Empty;
                trxVenta.Rewind();                                                          //move to first record

                int errores = 0; int i = 1;
                cfdReglasFacturaXml DocVenta = new cfdReglasFacturaXml(_Conex, _Param);     //log de facturas xml emitidas y anuladas
                ReglasME maquina = new ReglasME(_Param);

                OnProgreso(1, "INICIANDO BAJA DE DOCUMENTO...");              //Notifica al suscriptor
                do
                {
                    msj = String.Empty;
                    try
                    {
                        String accion = "BAJA";
                        if (maquina.ValidaTransicion(_Param.tipoDoc, accion, trxVenta.EstadoActual))
                        {
                            eBinario = maquina.eBinarioNuevo;

                            trxVenta.ArmarBaja(motivoBaja);
                            String[] serieCorrelativo = trxVenta.Sopnumbe.Split(new char[] { '-' });
                            string numeroSunat = serieCorrelativo[0] + "-" + serieCorrelativo[1];
                            //string numeroSunat = serieCorrelativo[0] + "-" + long.Parse(serieCorrelativo[1]).ToString();

                            //validaciones
                            switch (trxVenta.DocumentoBaja.Bajas.First().TipoDocumento)
                            {
                                case "01":
                                    if (!trxVenta.Sopnumbe.Substring(0, 1).Equals("F"))
                                    {
                                        msj = "El folio de la Factura debe empezar con la letra F. ";
                                        throw new ApplicationException(msj);
                                    }
                                    break;
                                case "03":
                                    if (!trxVenta.Sopnumbe.Substring(0, 1).Equals("B"))
                                    {
                                        msj = "El folio de la Boleta debe empezar con la letra B. ";
                                        throw new ApplicationException(msj);
                                    }
                                    break;
                                default:
                                    msj = "ok";
                                    break;
                            }
                            string nombreArchivo = Utiles.FormatoNombreArchivo(trxVenta.Docid + trxVenta.Sopnumbe + "_" + trxVenta.s_CUSTNMBR, trxVenta.s_NombreCliente, 20) + "_" + accion.Substring(0, 4);

                            string resultadoBaja = await servicioTimbre.SolicitarBajaAsync(trxVenta.DocumentoBaja.Emisor.NroDocumento, trxVenta.Ruta_certificadoPac, trxVenta.Contrasenia_clavePac, string.Concat(trxVenta.DocumentoBaja.Bajas.First().TipoDocumento, "-", numeroSunat), Utiles.Izquierda(motivoBaja, 100));

                            //var rutaYNom = await DocVenta.GuardaArchivoAsync(trxVenta, resultado.Item2, nombreArchivo, ".xml", false);

                            DocVenta.RegistraLogDeArchivoXML(trxVenta.Soptype, trxVenta.Sopnumbe, resultadoBaja, "baja ok", _Conex.Usuario, string.Empty, maquina.DestinoStatusBase, maquina.DestinoEBinario, maquina.DestinoMensaje);

                            DocVenta.ActualizaFacturaEmitida(trxVenta.Soptype, trxVenta.Sopnumbe, _Conex.Usuario, "emitido", "emitido", maquina.DestinoEBinario, maquina.DestinoMensaje, "baja ok");

                        }
                    }
                    catch (HttpRequestException he)
                    {
                        msj = string.Concat(he.Message, Environment.NewLine, he.StackTrace);
                        errores++;
                    }
                    catch (ApplicationException ae)
                    {
                        msj = ae.Message + Environment.NewLine + ae.StackTrace;
                        errores++;
                    }
                    catch (IOException io)
                    {
                        msj = "Excepción al revisar la carpeta/archivo: " + trxVenta.Ruta_clave + " Verifique su existencia y privilegios." + Environment.NewLine + io.Message + Environment.NewLine;
                        errores++;
                    }
                    catch (Exception lo)
                    {
                        string imsj = lo.InnerException == null ? "" : lo.InnerException.ToString();
                        msj = lo.Message + " " + imsj + Environment.NewLine + lo.StackTrace;
                        errores++;
                    }
                    finally
                    {
                        OnProgreso(i * 100 / trxVenta.RowCount, "Doc:" + trxVenta.Sopnumbe + " " + msj.Trim() + " " + maquina.ultimoMensaje + Environment.NewLine);              //Notifica al suscriptor
                        i++;
                    }
                } while (trxVenta.MoveNext() && errores < 10 && i < 2); //Dar de baja uno por uno
            }
            catch (Exception xw)
            {
                string imsj = xw.InnerException == null ? "" : xw.InnerException.ToString();
                this.ultimoMensaje = xw.Message + " " + imsj + Environment.NewLine + xw.StackTrace;
            }
            finally
            {
                OnProgreso(100, ultimoMensaje);
            }
            OnProgreso(100, "Proceso finalizado!");
        }


        /// <summary>
        /// Ejecuta la generación de documentos xml: factura, boleta, nc, nd
        /// </summary>
        //public async Task GeneraDocumentoXmlAsync()
        //{
        //    try
        //    {
        //        String msj = String.Empty;
        //        trxVenta.Rewind();                                                          //move to first record

        //        int errores = 0; int i = 1;
        //        cfdReglasFacturaXml DocVenta = new cfdReglasFacturaXml(_Conex, _Param);     //log de facturas xml emitidas y anuladas
        //        ReglasME maquina = new ReglasME(_Param);
        //        ValidadorXML validadorxml = new ValidadorXML(_Param);
        //        TransformerXML loader = new TransformerXML();
        //        OnProgreso(1, "INICIANDO EMISION DE COMPROBANTES DE VENTA...");              //Notifica al suscriptor
        //        do
        //        {
        //            msj = String.Empty;
        //            try
        //            {
        //                String accion = "EMITE XML Y PDF";
        //                if (trxVenta.Estado.Equals("no emitido") &&
        //                    maquina.ValidaTransicion(_Param.tipoDoc, accion, trxVenta.EstadoActual, "emitido/impreso") &&
        //                    trxVenta.EstadoContabilizado.Equals("contabilizado"))
        //                    if (trxVenta.Voidstts == 0)  //documento no anulado
        //                    {
        //                        trxVenta.ArmarDocElectronico();

        //                        var proxy = new HttpClient { BaseAddress = new Uri(ConfigurationManager.AppSettings["UrlOpenInvoicePeruApi"]) };

        //                        string metodoApi = string.Empty;
        //                        switch (trxVenta.DocElectronico.TipoDocumento)
        //                        {
        //                            case "07":
        //                                metodoApi = "api/GenerarNotaCredito";
        //                                if (trxVenta.DocElectronico.Relacionados.Count() == 0)
        //                                {
        //                                    msj = "La nota de crédito no está aplicada.";
        //                                    continue;
        //                                }
        //                                else
        //                                {
        //                                    if (trxVenta.DocElectronico.Relacionados
        //                                                                .Where(f => f.NroDocumento.Substring(0, 1) == trxVenta.DocElectronico.IdDocumento.Substring(0, 1)).Count() != trxVenta.DocElectronico.Relacionados.Count())
        //                                    {
        //                                        msj = "La serie de la nota de crédito y de la factura aplicada deben empezar con la misma letra: F o B.";
        //                                        continue;
        //                                    }
        //                                }
        //                                if (trxVenta.DocElectronico.Discrepancias.Count() == 0)
        //                                {
        //                                    msj = "No ha informado la causa de la discrepancia en la nota de crédito.";
        //                                    continue;
        //                                }

        //                                break;
        //                            case "08":
        //                                metodoApi = "api/GenerarNotaDebito";
        //                                break;
        //                            case "01":
        //                                metodoApi = "api/GenerarFactura";
        //                                break;
        //                            case "03":  //boleta
        //                                metodoApi = "api/GenerarFactura";
        //                                break;
        //                            default:
        //                                metodoApi = "No existe API para el tipo de documento: " + trxVenta.DocElectronico.TipoDocumento;
        //                                throw new ApplicationException(metodoApi);
        //                                //break;
        //                        }

        //                        var response = await proxy.PostAsJsonAsync(metodoApi, trxVenta.DocElectronico);
        //                        response.EnsureSuccessStatusCode();

        //                        var respuesta = await response.Content.ReadAsAsync<DocumentoResponse>();
        //                        if (!respuesta.Exito)
        //                            throw new ApplicationException(respuesta.MensajeError);

        //                        if (!_Param.seguridadIntegrada)
        //                        {
        //                            String RutaArchivo = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"{trxVenta.DocElectronico.IdDocumento}sf.xml");
        //                            byte[] bTramaXmlSinFirma = Convert.FromBase64String(respuesta.TramaXmlSinFirma);
        //                            File.WriteAllBytes(RutaArchivo, bTramaXmlSinFirma);
        //                        }

        //                        await FirmaYEnviaASunat(respuesta.TramaXmlSinFirma, trxVenta.DocElectronico.TipoDocumento, trxVenta.DocElectronico.IdDocumento, trxVenta.DocElectronico.Emisor.NroDocumento, false, false, false);

        //                        //if (!_Param.seguridadIntegrada)
        //                        //{
        //                        //    String RutaArchivox = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"{trxVenta.DocElectronico.IdDocumento}.xml");
        //                        //    byte[] bTramaXmlFirmado = Convert.FromBase64String(tramaXmlFirmado);
        //                        //    File.WriteAllBytes(RutaArchivox, bTramaXmlFirmado);
        //                        //}

        //                        //Guardar el comprobante como emitido rechazado
        //                        bool ebinarioErr = int.Parse(_codigoRespuesta) >= 2000 && int.Parse(_codigoRespuesta) < 4000;

        //                        //Guarda el archivo xml, genera el pdf. 
        //                        //Luego anota en la bitácora la factura emitida o el error al generar cbb o pdf.
        //                        DocVenta.AlmacenaEnRepositorio(trxVenta, Encoding.UTF8.GetString(Convert.FromBase64String(tramaXmlFirmado)), maquina, tramaXmlFirmado, tramaZipCdr, "FAC", nombreArchivoCdr,
        //                                                    _Param.tipoDoc, accion, !ebinarioErr);

        //                    }
        //                    else //si el documento está anulado en gp, agregar al log como emitido
        //                    {
        //                        maquina.ValidaTransicion("FACTURA", "ANULA VENTA", trxVenta.EstadoActual, "emitido");
        //                        msj = "Anulado en GP y marcado como emitido.";
        //                        DocVenta.RegistraLogDeArchivoXML(trxVenta.Soptype, trxVenta.Sopnumbe, "Anulado en GP", "0", _Conex.Usuario, "", "emitido", maquina.eBinarioNuevo, msj.Trim());
        //                    }
        //            }
        //            catch (HttpRequestException he)
        //            {
        //                msj = string.Concat(he.Message, Environment.NewLine, he.StackTrace);
        //                errores++;
        //            }
        //            catch (ApplicationException ae)
        //            {
        //                msj = ae.Message + Environment.NewLine + ae.StackTrace;
        //                errores++;
        //            }
        //            catch (IOException io)
        //            {
        //                msj = "Excepción al revisar la carpeta/archivo: " + trxVenta.Ruta_clave + " Verifique su existencia y privilegios." + Environment.NewLine + io.Message + Environment.NewLine;
        //                errores++;
        //            }
        //            catch (Exception lo)
        //            {
        //                string imsj = lo.InnerException == null ? "" : lo.InnerException.ToString();
        //                msj = lo.Message + " " + imsj + Environment.NewLine + lo.StackTrace;
        //                errores++;
        //            }
        //            finally
        //            {
        //                OnProgreso(i * 100 / trxVenta.RowCount, "Doc:" + trxVenta.Sopnumbe + " " + msj.Trim() + Environment.NewLine);              //Notifica al suscriptor
        //                i++;
        //            }
        //        } while (trxVenta.MoveNext() && errores < 10);
        //    }
        //    catch (Exception xw)
        //    {
        //        string imsj = xw.InnerException == null ? "" : xw.InnerException.ToString();
        //        this.ultimoMensaje = xw.Message + " " + imsj + Environment.NewLine + xw.StackTrace;
        //    }
        //    finally
        //    {
        //        OnProgreso(100, ultimoMensaje);
        //    }
        //    OnProgreso(100, "Proceso finalizado!");
        //}

        /// <summary>
        /// Ejecuta la generación del resumen de boletas
        /// </summary>
        //public async Task GeneraResumenXmlAsync()
        //{
        //    try
        //    {
        //        String msj = String.Empty;
        //        trxVenta.Rewind();                                                          //move to first record

        //        int errores = 0; int i = 1;
        //        cfdReglasFacturaXml DocVenta = new cfdReglasFacturaXml(_Conex, _Param);     //log de facturas xml emitidas y anuladas
        //        ReglasME maquina = new ReglasME(_Param);

        //        OnProgreso(1, "INICIANDO ENVIO DE RESUMEN...");              //Notifica al suscriptor
        //        do
        //        {
        //            msj = String.Empty;
        //            try
        //            {
        //                String accion = "ENVIA RESUMEN";
        //                if (maquina.ValidaTransicion("RESUMEN", accion, trxVenta.EstadoActual, "emitido/enviado a la sunat"))
        //                    if (trxVenta.Voidstts == 0)  //documento no anulado
        //                    {
        //                        trxVenta.ArmarResumenElectronico();

        //                        var cAgrupados = trxVenta.ResumenElectronico.Resumenes.GroupBy(y => y.IdDocumento, (key, num) => new { id = key, cantidad = num.Count()});
        //                        var c = cAgrupados.Where(q => q.cantidad > 1);
        //                        if (c.Count() > 0)
        //                            throw new ApplicationException("La siguiente nota de crédito o débito aplica a más de un comprobante: " + c.First().id + " Ingrese a GP y aplique un solo comprobante.");

        //                        var proxy = new HttpClient { BaseAddress = new Uri(ConfigurationManager.AppSettings["UrlOpenInvoicePeruApi"]) };

        //                        var response = await proxy.PostAsJsonAsync("api/GenerarResumenDiario/v2", trxVenta.ResumenElectronico);
        //                        response.EnsureSuccessStatusCode();

        //                        var respuesta = await response.Content.ReadAsAsync<DocumentoResponse>();
        //                        if (!respuesta.Exito)
        //                            throw new ApplicationException(respuesta.MensajeError);

        //                        if (!_Param.seguridadIntegrada)
        //                        {
        //                            String RutaArchivo = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"{trxVenta.ResumenElectronico.IdDocumento}sf.xml");
        //                            byte[] bTramaXmlSinFirma = Convert.FromBase64String(respuesta.TramaXmlSinFirma);
        //                            File.WriteAllBytes(RutaArchivo, bTramaXmlSinFirma);
        //                        }

        //                        await FirmaYEnviaASunat(respuesta.TramaXmlSinFirma, String.Empty, trxVenta.ResumenElectronico.IdDocumento, trxVenta.ResumenElectronico.Emisor.NroDocumento, true, false, false);

        //                        //Guarda el archivo xml, genera el cbb y el pdf. 
        //                        //Luego anota en la bitácora la factura emitida o el error al generar cbb o pdf.
        //                        DocVenta.AlmacenaEnRepositorio(trxVenta, Encoding.UTF8.GetString(Convert.FromBase64String(respuesta.TramaXmlSinFirma)), maquina, tramaXmlFirmado, String.Empty, nroTicket, String.Empty, 
        //                                                       "RESUMEN", accion, true);

        //                    }
        //            }
        //            catch (HttpRequestException he)
        //            {
        //                msj = string.Concat(he.Message, Environment.NewLine, he.StackTrace);
        //                errores++;
        //            }
        //            catch (ApplicationException ae)
        //            {
        //                msj = ae.Message + Environment.NewLine + ae.StackTrace;
        //                errores++;
        //            }
        //            catch (IOException io)
        //            {
        //                msj = "Excepción al revisar la carpeta/archivo: " + trxVenta.Ruta_clave + " Verifique su existencia y privilegios." + Environment.NewLine + io.Message + Environment.NewLine;
        //                errores++;
        //            }
        //            catch (Exception lo)
        //            {
        //                string imsj = lo.InnerException == null ? "" : lo.InnerException.ToString();
        //                msj = lo.Message + " " + imsj + Environment.NewLine + lo.StackTrace;
        //                errores++;
        //            }
        //            finally
        //            {
        //                OnProgreso(i * 100 / trxVenta.RowCount, "Doc:" + trxVenta.Sopnumbe + " " + msj.Trim() + " " + maquina.ultimoMensaje + Environment.NewLine);              //Notifica al suscriptor
        //                i++;
        //            }
        //        } while (trxVenta.MoveNext() && errores < 10);
        //    }
        //    catch (Exception xw)
        //    {
        //        string imsj = xw.InnerException == null ? "" : xw.InnerException.ToString();
        //        this.ultimoMensaje = xw.Message + " " + imsj + Environment.NewLine + xw.StackTrace;
        //    }
        //    finally
        //    {
        //        OnProgreso(100, ultimoMensaje);
        //    }
        //    OnProgreso(100, "Proceso finalizado!");
        //}

        //public async Task ProcesaConsultaCDR()
        //{
        //    try
        //    {
        //        String msj = String.Empty;
        //        String eBinario = String.Empty;
        //        trxVenta.Rewind();                                                          //move to first record

        //        int errores = 0; int i = 1;
        //        cfdReglasFacturaXml DocVenta = new cfdReglasFacturaXml(_Conex, _Param);     //log de facturas xml emitidas y anuladas
        //        ReglasME maquina = new ReglasME(_Param);

        //        OnProgreso(1, "INICIANDO CONSULTA DE CDRs...");              //Notifica al suscriptor
        //        do
        //        {
        //            msj = String.Empty;
        //            try
        //            {
        //                String ticket = trxVenta.Regimen;
        //                String td = !trxVenta.Docid.Equals("RESUMEN") ? _Param.tipoDoc : trxVenta.Docid;
        //                String accion = "CONSULTA CDR";

        //                if (maquina.ValidaTransicion(td, accion, trxVenta.EstadoActual, "consulta a la sunat"))
        //                    if (trxVenta.Voidstts == 0)  //documento no anulado
        //                    {
        //                        await ConsultaCDR(ticket, trxVenta.Sopnumbe, trxVenta.Rfc);
        //                        if (_codigoRespuesta.Equals("0"))   //aceptada
        //                        {
        //                            eBinario = maquina.eBinarioNuevo;
        //                            _mensajeSunat = string.IsNullOrEmpty(_mensajeSunat) ? "Consulta CDR OK" : _mensajeSunat;
        //                        }
        //                        else //if (!_consultaCDRExito)
        //                        {
        //                            eBinario = maquina.eBinActualConError;
        //                            errores++;
        //                        }

        //                        DocVenta.AlmacenaEnRepositorio(trxVenta, _mensajeSunat, maquina, string.Empty, tramaZipCdr, ticket, string.Concat(trxVenta.Sopnumbe, nombreArchivoCdr), 
        //                                                    td, accion, _codigoRespuesta.Equals("0"));

        //                        DocVenta.ActualizaFacturaEmitida(trxVenta.Soptype, trxVenta.Sopnumbe, _Conex.Usuario, "emitido", "emitido", eBinario, maquina.EnLetras(eBinario, td)+ _mensajeSunat, ticket);
        //                    }
        //            }
        //            catch (ApplicationException ae)
        //            {
        //                msj = ae.Message + Environment.NewLine + ae.StackTrace;
        //                errores++;
        //            }
        //            catch (IOException io)
        //            {
        //                msj = "Excepción al revisar la carpeta/archivo: " + trxVenta.Ruta_clave + " Verifique su existencia y privilegios." + Environment.NewLine + io.Message + Environment.NewLine;
        //                errores++;
        //            }
        //            catch (Exception lo)
        //            {
        //                string imsj = lo.InnerException == null ? "" : lo.InnerException.ToString();
        //                msj = lo.Message + " " + imsj + Environment.NewLine + lo.StackTrace;
        //                errores++;
        //            }
        //            finally
        //            {
        //                OnProgreso(i * 100 / trxVenta.RowCount, "Doc:" + trxVenta.Sopnumbe + " " + msj.Trim() + " " + maquina.ultimoMensaje + Environment.NewLine);              //Notifica al suscriptor
        //                i++;
        //            }
        //        } while (trxVenta.MoveNext() && errores < 10);
        //    }
        //    catch (Exception xw)
        //    {
        //        string imsj = xw.InnerException == null ? "" : xw.InnerException.ToString();
        //        this.ultimoMensaje = xw.Message + " " + imsj + Environment.NewLine + xw.StackTrace;
        //    }
        //    finally
        //    {
        //        OnProgreso(100, ultimoMensaje);
        //    }
        //    OnProgreso(100, "PROCESO FINALIZADO!");
        //}

        //public async Task ProcesaBajaComprobante(String motivoBaja)
        //{
        //    try
        //    {
        //        String msj = String.Empty;
        //        String eBinario = String.Empty;
        //        trxVenta.Rewind();                                                          //move to first record

        //        int errores = 0; int i = 1;
        //        cfdReglasFacturaXml DocVenta = new cfdReglasFacturaXml(_Conex, _Param);     //log de facturas xml emitidas y anuladas
        //        ReglasME maquina = new ReglasME(_Param);

        //        OnProgreso(1, "INICIANDO BAJA DE DOCUMENTO...");              //Notifica al suscriptor
        //        do
        //        {
        //            msj = String.Empty;
        //            try
        //            {
        //                String accion = "DAR DE BAJA";
        //                if (maquina.ValidaTransicion(_Param.tipoDoc, accion, trxVenta.EstadoActual, "baja solicitada"))
        //                {
        //                    eBinario = maquina.eBinarioNuevo;

        //                    trxVenta.ArmarBaja(motivoBaja);

        //                    var proxy = new HttpClient { BaseAddress = new Uri(ConfigurationManager.AppSettings["UrlOpenInvoicePeruApi"]) };

        //                    var response = await proxy.PostAsJsonAsync("api/GenerarComunicacionBaja", trxVenta.DocumentoBaja);
        //                    response.EnsureSuccessStatusCode();

        //                    var respuesta = await response.Content.ReadAsAsync<DocumentoResponse>();
        //                    if (!respuesta.Exito)
        //                        throw new ApplicationException(respuesta.MensajeError);

        //                    if (!_Param.seguridadIntegrada)
        //                    {
        //                        String RutaArchivo = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"{trxVenta.DocumentoBaja.IdDocumento}sf.xml");
        //                        byte[] bTramaXmlSinFirma = Convert.FromBase64String(respuesta.TramaXmlSinFirma);
        //                        File.WriteAllBytes(RutaArchivo, bTramaXmlSinFirma);
        //                    }

        //                    await FirmaYEnviaASunat(respuesta.TramaXmlSinFirma, string.Empty, trxVenta.DocumentoBaja.IdDocumento, trxVenta.DocumentoBaja.Emisor.NroDocumento, false, false, true);

        //                    DocVenta.AlmacenaEnRepositorio(trxVenta, Encoding.UTF8.GetString(Convert.FromBase64String(respuesta.TramaXmlSinFirma)), maquina, tramaXmlFirmado, String.Empty, nroTicket, String.Empty,
        //                                                _Param.tipoDoc, accion, true);
        //                    DocVenta.ActualizaFacturaEmitida(trxVenta.Soptype, trxVenta.Sopnumbe, _Conex.Usuario, "emitido", "emitido", eBinario, maquina.EnLetras(eBinario, _Param.tipoDoc), nroTicket);

        //                }
        //            }
        //            catch (HttpRequestException he)
        //            {
        //                msj = string.Concat(he.Message, Environment.NewLine, he.StackTrace);
        //                errores++;
        //            }
        //            catch (ApplicationException ae)
        //            {
        //                msj = ae.Message + Environment.NewLine + ae.StackTrace;
        //                errores++;
        //            }
        //            catch (IOException io)
        //            {
        //                msj = "Excepción al revisar la carpeta/archivo: " + trxVenta.Ruta_clave + " Verifique su existencia y privilegios." + Environment.NewLine + io.Message + Environment.NewLine;
        //                errores++;
        //            }
        //            catch (Exception lo)
        //            {
        //                string imsj = lo.InnerException == null ? "" : lo.InnerException.ToString();
        //                msj = lo.Message + " " + imsj + Environment.NewLine + lo.StackTrace;
        //                errores++;
        //            }
        //            finally
        //            {
        //                OnProgreso(i * 100 / trxVenta.RowCount, "Doc:" + trxVenta.Sopnumbe + " " + msj.Trim() + " " + maquina.ultimoMensaje + Environment.NewLine);              //Notifica al suscriptor
        //                i++;
        //            }
        //        } while (trxVenta.MoveNext() && errores < 10 && i<2); //Dar de baja uno por uno
        //    }
        //    catch (Exception xw)
        //    {
        //        string imsj = xw.InnerException == null ? "" : xw.InnerException.ToString();
        //        this.ultimoMensaje = xw.Message + " " + imsj + Environment.NewLine + xw.StackTrace;
        //    }
        //    finally
        //    {
        //        OnProgreso(100, ultimoMensaje);
        //    }
        //    OnProgreso(100, "Proceso finalizado!");
        //}

        /// <summary>
        /// Firma y envia el documento xml a la SUNAT
        /// </summary>
        /// <param name="xmlSinFirma"></param>
        /// <param name="tipoDoc"></param>
        /// <param name="IdDocumento"></param>
        /// <param name="emisorNroDocumento"></param>
        /// <param name="rbResumen">Indicar si el documento a enviar es un resumen de boletas</param>
        /// <param name="RetencPercepGRem">Indicar si el documento a enviar es de retenciones</param>
        /// <returns></returns>
        //async Task FirmaYEnviaASunat(String tramaXmlSinFirma, String codigoTipoDoc, String IdDocumento, String emisorNroDocumento, bool rbResumen, bool RetencPercepGRem, bool baja)
        //{
        //    if (string.IsNullOrEmpty(IdDocumento))
        //        throw new InvalidOperationException("La Serie y el Correlativo no pueden estar vacíos");

        //    var firmadoRequest = new FirmadoRequest
        //    {
        //        TramaXmlSinFirma = tramaXmlSinFirma,
        //        //CertificadoDigital = Convert.ToBase64String(File.ReadAllBytes(_txtRutaCertificado)),
        //        CertificadoDigital = Convert.ToBase64String(File.ReadAllBytes(trxVenta.Ruta_clave)),
        //        PasswordCertificado = trxVenta.Contrasenia_clave,   // txtPassCertificado.Text,
        //        UnSoloNodoExtension = RetencPercepGRem || rbResumen || baja
        //    };

        //    var jsonFirmado = await _client.PostAsJsonAsync("api/Firmar", firmadoRequest);
        //    jsonFirmado.EnsureSuccessStatusCode();

        //    var respuestaFirmado = await jsonFirmado.Content.ReadAsAsync<FirmadoResponse>();
        //    if (!respuestaFirmado.Exito)
        //        throw new ApplicationException(string.Concat("Excepción al firmar antes de enviar a la Sunat. ", respuestaFirmado.MensajeError));

        //    var enviarDocumentoRequest = new EnviarDocumentoRequest
        //    {
        //        Ruc = emisorNroDocumento,  // txtNroRuc.Text,
        //        UsuarioSol = trxVenta.Ruta_certificadoPac,    //txtUsuarioSol.Text,
        //        ClaveSol = trxVenta.Contrasenia_clavePac,
        //        EndPointUrl = _Param.URLwebServPAC,
        //        IdDocumento = IdDocumento,
        //        TipoDocumento = codigoTipoDoc,
        //        TramaXmlFirmado = respuestaFirmado.TramaXmlFirmado
        //    };

        //    var apiMetodo = (rbResumen || baja) && codigoTipoDoc != "09" ? "api/EnviarResumen" : "api/EnviarDocumento";

        //    var jsonEnvioDocumento = await _client.PostAsJsonAsync(apiMetodo, enviarDocumentoRequest);
        //    jsonEnvioDocumento.EnsureSuccessStatusCode();

        //    RespuestaComunConArchivo respuestaEnvio;
        //    if (rbResumen||baja)
        //    {
        //        tramaXmlFirmado = respuestaFirmado.TramaXmlFirmado;
        //        respuestaEnvio = await jsonEnvioDocumento.Content.ReadAsAsync<EnviarResumenResponse>();
        //        var rpta = (EnviarResumenResponse)respuestaEnvio;
        //        nroTicket = string.IsNullOrEmpty(rpta.NroTicket) ? String.Empty : rpta.NroTicket;
        //        //txtResult.Text = $@"{Resources.procesoCorrecto}{Environment.NewLine}{rpta.NroTicket}";
        //        if (!respuestaEnvio.Exito)
        //            throw new ApplicationException(string.Concat("La respuesta de la Sunat es negativa para el resumen o baja. ", respuestaEnvio.MensajeError));
        //    }
        //    else
        //    {
        //        respuestaEnvio = await jsonEnvioDocumento.Content.ReadAsAsync<EnviarDocumentoResponse>();
        //        var rpta = (EnviarDocumentoResponse)respuestaEnvio;
                    
        //        //txtResult.Text = $@"{Resources.procesoCorrecto}{Environment.NewLine}{rpta.MensajeRespuesta} siendo las {DateTime.Now}";
        //        if (rpta.Exito)
        //        {
        //            if (!string.IsNullOrEmpty(rpta.TramaZipCdr))
        //            {
        //                tramaXmlFirmado = respuestaFirmado.TramaXmlFirmado;
        //                tramaZipCdr = rpta.TramaZipCdr;
        //                nombreArchivoCdr = $"R-{respuestaEnvio.NombreArchivo}.zip";

        //                _codigoRespuesta = rpta.CodigoRespuesta;

        //                int iCodRespuesta = 0;
        //                if (Int32.TryParse(rpta.CodigoRespuesta, out iCodRespuesta))
        //                {
        //                    if (iCodRespuesta >= 100 && iCodRespuesta < 2000)
        //                        throw new ApplicationException(string.Concat("La SUNAT no procesó el comprobante. Codigo: ", rpta.CodigoRespuesta, " ", rpta.MensajeError, Environment.NewLine, rpta.MensajeRespuesta, " Corrija el problema e intente nuevamente."));
        //                }
        //                else
        //                    throw new ApplicationException(string.Concat("Código de respuesta desconocido. ", rpta.CodigoRespuesta, " ", rpta.MensajeError, Environment.NewLine, rpta.MensajeRespuesta));

        //            }
        //            else
        //            {
        //                throw new ApplicationException("La SUNAT no devolvió correctamente el comprobante de recepción. Verifique en el sitio web de la SUNAT.");
        //            }
        //        }
        //        else
        //        {
        //            throw new ApplicationException(string.Concat("La SUNAT no pudo procesar el comprobante. Intente más tarde. ", rpta.CodigoRespuesta, " ", rpta.MensajeError, Environment.NewLine, rpta.MensajeRespuesta));
        //        }
        //    }

        //}
        /// <summary>
        /// Consulta comprobante de recepción en la SUNAT
        /// </summary>
        /// <param name="txtNroTicket"></param>
        /// <param name="IdDocumento"></param>
        /// <param name="emisorNroDocumento"></param>
        /// <returns></returns>
        //public async Task ConsultaCDR(String txtNroTicket, String IdDocumento, String emisorNroDocumento)
        //{
        //    _consultaCDRExito = false;
        //    var consultaTicketRequest = new ConsultaTicketRequest
        //            {
        //                Ruc = emisorNroDocumento,
        //                UsuarioSol = trxVenta.Ruta_certificadoPac,
        //                ClaveSol = trxVenta.Contrasenia_clavePac,
        //                EndPointUrl = _Param.URLwebServPAC,
        //                IdDocumento = IdDocumento,
        //                NroTicket = txtNroTicket
        //            };

        //    var jsonConsultaTicket = await _client.PostAsJsonAsync("api/ConsultarTicket", consultaTicketRequest);
        //    jsonConsultaTicket.EnsureSuccessStatusCode();

        //    var respuestaEnvio = await jsonConsultaTicket.Content.ReadAsAsync<EnviarDocumentoResponse>();
            
        //    if (!respuestaEnvio.Exito || !string.IsNullOrEmpty(respuestaEnvio.MensajeError))
        //        throw new InvalidOperationException(string.Concat(respuestaEnvio.MensajeError, " [ConsultaCDR]"));

        //    if (string.IsNullOrEmpty(respuestaEnvio.TramaZipCdr) || respuestaEnvio.TramaZipCdr.Equals("Aun en proceso"))
        //        throw new InvalidOperationException("El CDR todavía está en proceso en la SUNAT. Intente consultar más tarde. ");

        //    _codigoRespuesta = respuestaEnvio.CodigoRespuesta;

        //    int iCodRespuesta = 0;
        //    if (Int32.TryParse(respuestaEnvio.CodigoRespuesta, out iCodRespuesta))
        //    {
        //        if (iCodRespuesta >= 100 && iCodRespuesta < 2000)
        //            throw new ApplicationException(string.Concat("La SUNAT no procesó el CDR. Codigo: ", respuestaEnvio.CodigoRespuesta, " ", respuestaEnvio.MensajeError, Environment.NewLine, respuestaEnvio.MensajeRespuesta, " Corrija el problema e intente nuevamente."));
        //    }
        //    else
        //        throw new ApplicationException(string.Concat("Código de respuesta desconocido. ", respuestaEnvio.CodigoRespuesta, " ", respuestaEnvio.MensajeError, Environment.NewLine, respuestaEnvio.MensajeRespuesta));

        //    tramaZipCdr = respuestaEnvio.TramaZipCdr;
        //    nombreArchivoCdr = $"R{respuestaEnvio.NombreArchivo}.zip";
        //    //File.WriteAllBytes($"{Program.CarpetaCdr}\\R-{respuestaEnvio.NombreArchivo}.zip", Convert.FromBase64String(respuestaEnvio.TramaZipCdr));

        //    //txtResult.Text = $@"{respuestaEnvio.MensajeRespuesta}";
        //    _consultaCDRExito = respuestaEnvio.Exito;
        //    _mensajeSunat = respuestaEnvio.MensajeRespuesta;

        //}
    }
}

