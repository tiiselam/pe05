using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Net;
using Comun;


namespace Reporteador
{
    public partial class winVisorDeReportes : Form
    {
        public string mensajeErr = "";
        public int numErr = 0;
        private ConexionAFuenteDatos _Conexion = null;
        Parametros _Configuracion = null;
        List<string> _ValoresParametros = new List<string>();
        string _Param1 = "";
        string _Param2 = "";
        string _Param3 = "";
        int _Param4 = 0;

        public winVisorDeReportes()
        {
            InitializeComponent();
        }

        public winVisorDeReportes(ConexionAFuenteDatos Conexion, Parametros Configuracion, string Param1, string Param2, string Param3, int Param4)
        {
            InitializeComponent();
            _Conexion = Conexion;
            _Configuracion = Configuracion;
            _Param1 = Param1;
            _Param2 = Param2;
            _Param3 = Param3;
            _Param4 = Param4;
            if (_Configuracion.reporteador.Equals("CRYSTAL"))
            {
                _ValoresParametros.Add(_Param1);
                _ValoresParametros.Add(_Param2);
                _ValoresParametros.Add(_Param3);
            }
            if (Configuracion.emite && _Configuracion.reporteador.Equals("SSRS"))
            {
                _ValoresParametros.Add(_Param4.ToString());
                _ValoresParametros.Add(_Param1);
            }
            if (!Configuracion.emite && _Configuracion.reporteador.Equals("SSRS"))
            {
                _ValoresParametros.Add(_Param4.ToString());
                _ValoresParametros.Add(_Param1);
                _ValoresParametros.Add(Conexion.Intercompany);
            }
        }

        private void winVisorDeReportes_Load(object sender, EventArgs e)
        {
            ReporteCrystal reporteCrystal = new ReporteCrystal(_Conexion);
            ReporteSSRS reporteSsrs = new ReporteSSRS(_Conexion, _Configuracion);
            crViewerCfd.Visible = false;
            ssrsRepView.Visible = false;
            if (_Configuracion.reporteador.Equals("CRYSTAL"))
            {
                crViewerCfd.Visible = true;
                //En el caso de una compañía que debe emitir xml, usar reporte de 4 parámetros
                //if (Utiles.Derecha(Binario, 1).Equals("1"))
                if (_Configuracion.emite)
                    crViewerCfd.ReportSource = reporteCrystal.MuestraEnVisor(_Param1, _Param2, _Param3, _Param4);
                else
                    crViewerCfd.ReportSource = reporteCrystal.MuestraEnVisor(_Configuracion, _ValoresParametros);

                mensajeErr = reporteCrystal.ultimoMensaje;
                numErr = reporteCrystal.numError;
                crViewerCfd.Refresh();

            }
            if (_Configuracion.reporteador.Equals("SSRS"))
            {
                reporteSsrs.muestraEnVisor(ssrsRepView, _ValoresParametros);

                mensajeErr = reporteSsrs.ultimoMensaje;
                numErr = reporteSsrs.numError;
            }
        }
    }
}