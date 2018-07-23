using System;
using System.Collections.Generic;
using System.Text;
using Comun;

namespace Reporteador
{
    public class Documento
    {
        public string mensajeErr = "";
        public int numErr = 0;
        private ConexionAFuenteDatos _Conexion;
        private Parametros _Param;
        ReporteSSRS rSSRS;
        ReporteCrystal rcrystal;

        public Documento(ConexionAFuenteDatos conex, Parametros param)
        {
            _Conexion = conex;
            _Param = param;
            rSSRS = new ReporteSSRS(_Conexion, _Param);
            rcrystal = new ReporteCrystal(_Conexion);

            mensajeErr = rSSRS.ultimoMensaje;
            numErr = rSSRS.numError;
        }

        /// <summary>
        /// Genera el reporte en SSRS o Crystal
        /// 15/11/16 jcf Usa el mismo reporte SSRS sea que esté configurado para emitir o no. 
        /// </summary>
        /// <param name="strRutaYNomArchivo"></param>
        /// <param name="shSoptype"></param>
        /// <param name="strSopnumbe"></param>
        /// <param name="strEstadoContab"></param>
        public void generaEnFormatoPDF(String strRutaYNomArchivo, short shSoptype, string strSopnumbe, string strEstadoContab)
        {
            mensajeErr = "";
            numErr = 0;
            List<string> ValoresParametros = new List<string>();
            string prmTabla = "SOP30200";
            try
            {
                if (_Param.emite && _Param.reporteador.Equals("SSRS"))
                {
                    ValoresParametros.Add(shSoptype.ToString());
                    ValoresParametros.Add(strSopnumbe);

                    rSSRS.renderPDF(ValoresParametros, strRutaYNomArchivo + ".pdf");
                    mensajeErr = rSSRS.ultimoMensaje;
                    numErr = rSSRS.numError;
                }

                if (!_Param.emite && _Param.reporteador.Equals("SSRS"))
                {
                    ValoresParametros.Add(shSoptype.ToString());
                    ValoresParametros.Add(strSopnumbe);
                    ValoresParametros.Add(_Conexion.Intercompany);

                    rSSRS.renderPDF(ValoresParametros, strRutaYNomArchivo + ".pdf");
                    mensajeErr = rSSRS.ultimoMensaje;
                    numErr = rSSRS.numError;
                }

                //En el caso de una compañía que debe emitir xml, usar reporte con 4 parámetros
                if (_Param.emite && _Param.reporteador.Equals("CRYSTAL"))
                {
                    if (!rcrystal.GuardaDocumentoEnPDF(_Param, strSopnumbe, strSopnumbe, prmTabla, shSoptype, strRutaYNomArchivo + ".pdf"))
                        mensajeErr = rcrystal.ultimoMensaje;
                    numErr = rcrystal.numError;
                }

                if (!_Param.emite && _Param.reporteador.Equals("CRYSTAL"))
                {
                    if (strEstadoContab.ToLower().Equals("en lote"))
                        prmTabla = "SOP10100";
                    ValoresParametros.Add(strSopnumbe);
                    ValoresParametros.Add(strSopnumbe);
                    ValoresParametros.Add(prmTabla);
                    if (!rcrystal.GuardaDocumentoEnPDF(_Param, ValoresParametros, strRutaYNomArchivo + ".pdf"))
                        mensajeErr = rcrystal.ultimoMensaje;
                    numErr = rcrystal.numError;
                }
            }
            catch (Exception eFormato)
            {
                mensajeErr = "Contacte a su administrador. No se puede guardar el archivo PDF. [usaFormatoPDF] " + eFormato.Message;
                numErr++;
            }
        }
    }
}
