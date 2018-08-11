using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using cfdiPeruInterfaces;
using OpenInvoicePeru.Comun.Dto.Modelos;

namespace cfdiPeruOperadorServiciosElectronicos
{
    public class DocumentosOSE : ICfdiPeruDocumento
    {

        private string _DocElectronico;

        private string _DocElectronicoLinea0A;
        private string _DocElectronicoLinea01;
        private string _DocElectronicoLinea02;
        private string _DocElectronicoLinea03;

        public String FormatearDocElectronico(String tipoDocumento, DocumentoElectronico docElectronico)
        {
            //LINEA 0A
            Console.WriteLine("Metodo Formatear Documento");
            _DocElectronicoLinea0A = "0A|MANUAL|";
            _DocElectronicoLinea0A = _DocElectronicoLinea0A + Environment.NewLine;
            // VER EL TEMA DE sALto DE CARRo

            //LINEA 1 RECEPTOR
            _DocElectronicoLinea01 = "01|"; // 1 - Identificador de LInea
            _DocElectronicoLinea01 = _DocElectronicoLinea01 + docElectronico.Receptor.NombreLegal + "|";  // 2 - 
            _DocElectronicoLinea01 = _DocElectronicoLinea01 + docElectronico.Receptor.NroDocumento + "/"; // 3 
            _DocElectronicoLinea01 = _DocElectronicoLinea01 + docElectronico.Receptor.TipoDocumento + "|";
            _DocElectronicoLinea01 = _DocElectronicoLinea01 + "|"; //docElectronico.Receptor.Mail + "|";
            _DocElectronicoLinea01 = _DocElectronicoLinea01 + "|"; //docElectronico.Receptor.Telefono + "|";
            _DocElectronicoLinea01 = _DocElectronicoLinea01 + docElectronico.Receptor.Direccion + "|";
            _DocElectronicoLinea01 = _DocElectronicoLinea01 + docElectronico.Receptor.Provincia + "|";
            _DocElectronicoLinea01 = _DocElectronicoLinea01 + "|"; //docElectronico.Receptor.CodigoPostal + "|";
            _DocElectronicoLinea01 = _DocElectronicoLinea01 + docElectronico.Receptor.Departamento + "|";
            _DocElectronicoLinea01 = _DocElectronicoLinea01 + "|"; //país
            _DocElectronicoLinea01 = _DocElectronicoLinea01 + "NO|";
            //_DocElectronicoLinea01 = _DocElectronicoLinea01 + String.IsNullOrEmpty(docElectronico.Receptor?.Mail) ? "NO" : "SI" + "|";
            _DocElectronicoLinea01 = _DocElectronicoLinea01 + Environment.NewLine;

            //LINEA 2  DATOS GENERALES
            _DocElectronicoLinea02 = _DocElectronicoLinea02 + "02|"; //Identificador de LInea
            _DocElectronicoLinea02 = _DocElectronicoLinea02 + docElectronico.FechaEmision.Substring(0, 10) + "|";
            _DocElectronicoLinea02 = _DocElectronicoLinea02 + "|"; // FECHA EMISION RESUMEN
            _DocElectronicoLinea02 = _DocElectronicoLinea02 + tipoDocumento + "|";
            _DocElectronicoLinea02 = _DocElectronicoLinea02 + docElectronico.IdDocumento + "|";
            _DocElectronicoLinea02 = _DocElectronicoLinea02 + "|"; // NRO INICIO SERIE -- Esto es para resumen
            _DocElectronicoLinea02 = _DocElectronicoLinea02 + "|"; // NRO FIN SERIE -- Esto es para resumen
            _DocElectronicoLinea02 = _DocElectronicoLinea02 + docElectronico.Gravadas.ToString("0.00").Replace(",", ".") + "|";
            _DocElectronicoLinea02 = _DocElectronicoLinea02 + docElectronico.Exoneradas.ToString("0.00").Replace(",", ".") + "|";
            _DocElectronicoLinea02 = _DocElectronicoLinea02 + docElectronico.Inafectas.ToString("0.00").Replace(",", ".") + "|";
            _DocElectronicoLinea02 = _DocElectronicoLinea02 + "|"; // OTROS_CONCEPTOS
            _DocElectronicoLinea02 = _DocElectronicoLinea02 + docElectronico.TotalIsc.ToString("0.00").Replace(",", ".") + "|";
            _DocElectronicoLinea02 = _DocElectronicoLinea02 + docElectronico.TotalIgv.ToString("0.00").Replace(",", ".") + "|";
            _DocElectronicoLinea02 = _DocElectronicoLinea02 + docElectronico.TotalOtrosTributos.ToString("0.00").Replace(",", ".") + "|";
            _DocElectronicoLinea02 = _DocElectronicoLinea02 + "|"; // DESCUENTOS NO GLOBALES
            _DocElectronicoLinea02 = _DocElectronicoLinea02 + docElectronico.TotalVenta.ToString("0.00").Replace(",", ".") + "|";
            _DocElectronicoLinea02 = _DocElectronicoLinea02 + docElectronico.Moneda + "|";

            foreach (OpenInvoicePeru.Comun.Dto.Modelos.DocumentoRelacionado relacionados in docElectronico.Relacionados)
            {
                _DocElectronicoLinea02 = _DocElectronicoLinea02 + relacionados.NroDocumento; //VERIFICAR
            }
            _DocElectronicoLinea02 += "|"; //VERIFICAR

            _DocElectronicoLinea02 = _DocElectronicoLinea02 + "|";
            _DocElectronicoLinea02 = _DocElectronicoLinea02 + docElectronico.Gratuitas.ToString("0.00").Replace(",", ".") + "|";
            _DocElectronicoLinea02 = _DocElectronicoLinea02 + string.Concat(docElectronico.Emisor.Direccion, " ", docElectronico.Emisor.Urbanizacion, " ", docElectronico.Emisor.Departamento, " ", docElectronico.Emisor.Distrito, " cp", docElectronico.Emisor.Ubigeo, "|");
            _DocElectronicoLinea02 = _DocElectronicoLinea02 + "|"; // TIPO_CAMBIO
            _DocElectronicoLinea02 = _DocElectronicoLinea02 + "|"; // METODO_PAGO
            _DocElectronicoLinea02 = _DocElectronicoLinea02 + "|"; // Observaciones
            _DocElectronicoLinea02 = _DocElectronicoLinea02 + docElectronico.DescuentoGlobal.ToString("0.00").Replace(",", ".") + "|"; 
            _DocElectronicoLinea02 = _DocElectronicoLinea02 + "|"; // CODIGO_NOTA
            _DocElectronicoLinea02 = _DocElectronicoLinea02 + Environment.NewLine;

            //LINEA 3 CONCEPTOS 

            foreach (OpenInvoicePeru.Comun.Dto.Modelos.DetalleDocumento detalle in docElectronico.Items)
            {
                _DocElectronicoLinea03 = _DocElectronicoLinea03 + "03|"; // 1 - Identificador de LInea
                //Console.WriteLine(detalle);
                _DocElectronicoLinea03 = _DocElectronicoLinea03 + detalle.UnidadMedida + "|";
                _DocElectronicoLinea03 = _DocElectronicoLinea03 + detalle.Cantidad.ToString("0.00").Replace(",", ".") + "|";
                _DocElectronicoLinea03 = _DocElectronicoLinea03 + detalle.Descripcion + "|";
                //_DocElectronicoLinea03 = _DocElectronicoLinea03 + Regex.Replace(detalle.Descripcion, @"[^0-9A-Za-z]", "", RegexOptions.None);  + "|";
                _DocElectronicoLinea03 = _DocElectronicoLinea03 + detalle.PrecioUnitario.ToString("0.00").Replace(",", ".") + "|";
                _DocElectronicoLinea03 = _DocElectronicoLinea03 + detalle.PrecioReferencial.ToString("0.00").Replace(",", ".") + "|";
                _DocElectronicoLinea03 = _DocElectronicoLinea03 + detalle.Impuesto.ToString("0.00").Replace(",", ".") + "|";
                _DocElectronicoLinea03 = _DocElectronicoLinea03 + detalle.ImpuestoSelectivo.ToString("0.00").Replace(",", ".") + "|";
                _DocElectronicoLinea03 = _DocElectronicoLinea03 + detalle.Suma.ToString("0.00").Replace(",", ".") + "|";
                _DocElectronicoLinea03 = _DocElectronicoLinea03 + Environment.NewLine;

            }


            _DocElectronico = _DocElectronicoLinea0A + _DocElectronicoLinea01 + _DocElectronicoLinea02 + _DocElectronicoLinea03;

            return _DocElectronico;
        }

        public String FormatearResumenElectronico(String tipoDocumento, ResumenDiarioNuevo docResumen)
        {
            Console.WriteLine("Metodo Formatear Resumen");
            //LINEA 1
            _DocElectronicoLinea01 = "01|RESUMEN||||||||||NO|";
            _DocElectronicoLinea01 = _DocElectronicoLinea01 + Environment.NewLine;

            // _DocElectronicoLinea02 = _DocElectronicoLinea02 + docResumen.  + "|";

            foreach (OpenInvoicePeru.Comun.Dto.Modelos.GrupoResumenNuevo detalle in docResumen.Resumenes)
            {
                //Datos Genericos
                _DocElectronicoLinea02 = _DocElectronicoLinea02 + "02|"; //Identificador de LInea
                _DocElectronicoLinea02 = _DocElectronicoLinea02 + docResumen.FechaEmision.Substring(0, 10) + "|";
                _DocElectronicoLinea02 = _DocElectronicoLinea02 + docResumen.FechaReferencia.Substring(0, 10) + "|";
                _DocElectronicoLinea02 = _DocElectronicoLinea02 + detalle.TipoDocumento + "|";
                _DocElectronicoLinea02 = _DocElectronicoLinea02 + detalle.IdDocumento.Substring(0, 4) + "|"; //SERIE Hay que ahcer el substr
                _DocElectronicoLinea02 = _DocElectronicoLinea02 + detalle.IdDocumento.Substring(5, 8) + "|";
                _DocElectronicoLinea02 = _DocElectronicoLinea02 + detalle.IdDocumento.Substring(5, 8) + "|";
                //Importes 
                _DocElectronicoLinea02 = _DocElectronicoLinea02 + detalle.Gravadas.ToString("0.00").Replace(",", ".") + "|";
                _DocElectronicoLinea02 = _DocElectronicoLinea02 + detalle.Exoneradas.ToString("0.00").Replace(",", ".") + "|";
                _DocElectronicoLinea02 = _DocElectronicoLinea02 + detalle.Inafectas.ToString("0.00").Replace(",", ".") + "|";
                _DocElectronicoLinea02 = _DocElectronicoLinea02 + "|"; // OTROS_CONCEPTOS
                _DocElectronicoLinea02 = _DocElectronicoLinea02 + detalle.TotalIsc.ToString("0.00").Replace(",", ".") + "|";
                _DocElectronicoLinea02 = _DocElectronicoLinea02 + detalle.TotalIgv.ToString("0.00").Replace(",", ".") + "|";
                _DocElectronicoLinea02 = _DocElectronicoLinea02 + detalle.TotalOtrosImpuestos.ToString("0.00").Replace(",", ".") + "|";
                _DocElectronicoLinea02 = _DocElectronicoLinea02 + detalle.TotalDescuentos.ToString("0.00").Replace(",", ".") + "|";
                _DocElectronicoLinea02 = _DocElectronicoLinea02 + detalle.TotalVenta.ToString("0.00").Replace(",", ".") + "|";
                //Oros Datos
                _DocElectronicoLinea02 = _DocElectronicoLinea02 + detalle.Moneda + "|";
                _DocElectronicoLinea02 = _DocElectronicoLinea02 + detalle.DocumentoRelacionado + "|"; //VERIFICAR
                _DocElectronicoLinea02 = _DocElectronicoLinea02 + "|";
                _DocElectronicoLinea02 = _DocElectronicoLinea02 + detalle.Gratuitas.ToString("0.00").Replace(",", ".") + "|";
                _DocElectronicoLinea02 = _DocElectronicoLinea02 + docResumen.Emisor.Direccion + "|";
                _DocElectronicoLinea02 = _DocElectronicoLinea02 + "|"; // TIPO_CAMBIO
                _DocElectronicoLinea02 = _DocElectronicoLinea02 + "|"; // METODO_PAGO
                _DocElectronicoLinea02 = _DocElectronicoLinea02 + "|"; // Observaciones
                _DocElectronicoLinea02 = _DocElectronicoLinea02 + "|"; // Observaciones
                _DocElectronicoLinea02 = _DocElectronicoLinea02 + "|"; // CODIGO_NOTA\n
                _DocElectronicoLinea02 = _DocElectronicoLinea02 + Environment.NewLine;
            }

            _DocElectronico = _DocElectronicoLinea01 + _DocElectronicoLinea02;

            return _DocElectronico;
        }

    }
}
