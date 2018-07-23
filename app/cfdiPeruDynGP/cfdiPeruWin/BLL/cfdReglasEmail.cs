using System;
using System.Collections.Generic;
using System.Text;
using MyGeneration.dOOdads;
using cfdiPeru;
using Comun;
using MaquinaDeEstados;
using EMailManejador;

namespace cfd.FacturaElectronica
{

    class cfdReglasEmail
    {
        public string ultimoMensaje = "";
        public int numMensajeError = 0;
        private ConexionAFuenteDatos _Conexion = null;
        private Parametros _Param = null;

        public cfdReglasEmail (ConexionAFuenteDatos conex, Parametros Param)
        {
            _Conexion = conex;
            _Param = Param;
        }

        public bool SeccionesEmail(ref string asunto, ref string cuerpo)
        {
            vwCfdCartasReclamacionDeuda carta = new vwCfdCartasReclamacionDeuda(_Conexion.ConnStr);
            carta.Where.Letter_type.Value = 3;
            carta.Where.Letter_type.Operator = WhereParameter.Operand.Equal;
            carta.Where.Ltrrptnm.Conjuction = WhereParameter.Conj.And;
            carta.Where.Ltrrptnm.Value = _Param.emailCarta;
            carta.Where.Ltrrptnm.Operator = WhereParameter.Operand.Equal;
            ultimoMensaje = "";
            try
            {
                if (!carta.Query.Load())
                {
                    ultimoMensaje = "No está configurada la carta predeterminada " + _Param.emailCarta + ". Registre la carta en Tarjetas > ventas > Collection Letters.";
                    numMensajeError++;
                    return false;
                }
                else
                {
                    asunto = carta.CN_Email_Subject;
                    cuerpo = carta.CN_Letter_Text;
                }
                return true;
            }
            catch (Exception eSe)
            {
                ultimoMensaje = "Contacte al administrador. No se pudo consultar la base de datos. [SeccionesEmail] " + eSe.Message;
                numMensajeError++;
                return false;
            }

        }

        public DireccionesEmail ObtieneDirecciones(string custnmbr)
        {
            vwCfdClienteDireccionesCorreo dirCorreo = new vwCfdClienteDireccionesCorreo(_Conexion.ConnStr);     //direcciones de correo de los clientes
            dirCorreo.Where.CUSTNMBR.Value = custnmbr;
            dirCorreo.Where.CUSTNMBR.Operator = WhereParameter.Operand.Equal;
            DireccionesEmail dir = new DireccionesEmail("", "", "");
            try
            {
                if (!dirCorreo.Query.Load())
                {
                    ultimoMensaje = "El cliente no tiene direcciones de correo registradas.";
                    numMensajeError++;
                    return dir;
                }
                else
                {
                    dir.mailTo = dirCorreo.EmailTo;
                    dir.mailCC = dirCorreo.EmailCC;
                    dir.mailCCO = dirCorreo.EmailCCO;
                    ultimoMensaje = "";
                }
                return dir;
            }
            catch (Exception edc)
            {
                ultimoMensaje = "Contacte al administrador. No se pudo consultar la base de datos. [ObtieneDireccionesDeCorreo] " + edc.Message;
                numMensajeError++;
                return dir;
            }

        }

        public bool ProcesaEnvioMail(EmailSmtp mail, string custnmbr, string docId, short Soptype, string Sopnumbe, string nombreCliente,
                                    string eBinarioActual, string eBinarioNuevo, string Mensaje, 
                                    string asunto, string cuerpo)
        {
            cfdReglasFacturaXml doc = new cfdReglasFacturaXml(_Conexion, _Param);    //log de facturas xml emitidas y anuladas
            List<string> Adjunto = new List<string>();
            DireccionesEmail dir = this.ObtieneDirecciones(custnmbr);
            string nombreArchivo = Utiles.FormatoNombreArchivo(docId + Sopnumbe + "_" + custnmbr, nombreCliente, 20);

            //Si hay error al obtener direcciones, no continuar
            if (ultimoMensaje.Equals(string.Empty))
            {
                if (_Param.emite)
                    Adjunto.Add(Mensaje.Replace("Almacenado en ", "") + "." + _Param.emailAdjEmite);    //xml o zip

                if (_Param.imprime)
                    Adjunto.Add(Mensaje.Replace("Almacenado en ", "") + "." + _Param.emailAdjImprm);    //pdf
                
                if (mail.SendMessage(Utiles.Derecha(dir.mailTo, dir.mailTo.Length - 1), _Param.emailAccount,
                                    asunto.Trim() + " (" + nombreArchivo + ")", cuerpo,
                                    Utiles.Derecha(dir.mailCC, dir.mailCC.Length - 1),
                                    Utiles.Derecha(dir.mailCCO, dir.mailCCO.Length - 1), 
                                    _Param.replyto, Adjunto))
                    doc.RegistraLogDeArchivoXML(Soptype, Sopnumbe, "E-mail enviado.", "0", _Conexion.Usuario, "", "enviado", eBinarioNuevo, "E-mail enviado el " + DateTime.Today.ToString());
                else
                    numMensajeError++;
            }
            else
                numMensajeError++;
            ultimoMensaje = ultimoMensaje + mail.ultimoMensaje + doc.ultimoMensaje;

            return ultimoMensaje.Equals(string.Empty);
        }
    }
}
