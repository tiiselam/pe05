using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;

using Comun;
using cfdiPeru;
using EMailManejador;
using MaquinaDeEstados;

namespace cfd.FacturaElectronica
{
    class cfdEmailWorker : BackgroundWorker
    {
        private Parametros _Param;
        private ConexionAFuenteDatos _Conex;

        public cfdEmailWorker(ConexionAFuenteDatos Conex, Parametros Param)
        {
            WorkerReportsProgress = true;
            WorkerSupportsCancellation = true;
            _Param = Param;
            _Conex = Conex;
        }

        /// <summary>
        /// Ejecuta el envío de correos en un thread independiente
        /// </summary>
        /// <param name="e">trxVentas</param>
        protected override void OnDoWork(DoWorkEventArgs e)
        {
            int errores = 0; int i = 1; 
            string asunto = ""; string cuerpo = "";

            ReportProgress(0, "Iniciando proceso...\r\n");
            object[] args = e.Argument as object[];
            vwCfdTransaccionesDeVenta trxVenta = (vwCfdTransaccionesDeVenta)args[0];

            cfdReglasEmail cliente = new cfdReglasEmail(_Conex, _Param);
            if (!cliente.SeccionesEmail(ref asunto, ref cuerpo))
            {
                e.Result = "No puede enviar e-mails \r\n";
                ReportProgress(100, cliente.ultimoMensaje + "\r\n");
                return;
            }

            //log de facturas xml emitidas y anuladas
            cfdReglasFacturaXml doc = new cfdReglasFacturaXml(_Conex, _Param);
            EmailSmtp mail = new EmailSmtp(_Param.emailSmtp, _Param.emailPort, _Param.emailUser, _Param.emailPwd, _Param.emailSsl);
            ReglasME maquina = new ReglasME(_Param);
            string eBinario = "";
            trxVenta.Rewind();                                          //move to first record
            do
            {
                try
                {
                    if (CancellationPending)
                    {
                        e.Cancel = true;
                        return;
                    }
                    if (trxVenta.Voidstts == 0)                             //documento no anulado
                    {
                        if (maquina.ValidaTransicion(_Param.tipoDoc, "ENVIA EMAIL", trxVenta.EstadoActual, "enviado"))
                        {
                            if (!cliente.ProcesaEnvioMail(mail, trxVenta.CUSTNMBR, trxVenta.Docid, trxVenta.Soptype, trxVenta.Sopnumbe, trxVenta.NombreCliente,
                                                        trxVenta.EstadoActual, maquina.eBinarioNuevo, trxVenta.Mensaje, asunto, cuerpo))
                            {
                                eBinario = maquina.eBinActualConError;
                                errores++;
                            }
                            else
                                eBinario = maquina.eBinarioNuevo;

                            doc.ActualizaFacturaEmitida(trxVenta.Soptype, trxVenta.Sopnumbe, _Conex.Usuario,
                                                        "emitido", "emitido", eBinario, maquina.EnLetras(eBinario, _Param.tipoDoc) + cliente.ultimoMensaje, String.Empty);
                        }
                    }

                }
                catch (Exception x)
                {
                    cliente.ultimoMensaje = x.Message;
                }
                ReportProgress(i * 100 / trxVenta.RowCount, "Doc:" + trxVenta.Sopnumbe + " " + cliente.ultimoMensaje + maquina.ultimoMensaje + "\r\n");
                i++;
                cliente.ultimoMensaje = "";
            } while (trxVenta.MoveNext() && errores < 10);
            e.Result = "Envío de Emails finalizado! \r\n";
            ReportProgress(100, "");

        }
    }
}
