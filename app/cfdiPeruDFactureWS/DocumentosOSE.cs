using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        string _DocElectronicoLinea04 = string.Empty;

        public String FormatearDocElectronico(String tipoDocumento, DocumentoElectronico docElectronico)
        {
            //LINEA 0A
            Debug.WriteLine("Método Formatear Documento");
            _DocElectronicoLinea0A = "0A|MANUAL|";
            _DocElectronicoLinea0A = _DocElectronicoLinea0A + Environment.NewLine;

            string em = string.Empty;
            if (!string.IsNullOrEmpty(docElectronico.Receptor.EMailTo))
                em = docElectronico.Receptor.EMailTo.Substring(1, docElectronico.Receptor.EMailTo.Length-1);

            //LINEA 1 RECEPTOR
            _DocElectronicoLinea01 = "01|"; // 1 - Identificador de LInea
            _DocElectronicoLinea01 = _DocElectronicoLinea01 + docElectronico.Receptor.NombreLegal + "|";  // 2 - 
            _DocElectronicoLinea01 = _DocElectronicoLinea01 + docElectronico.Receptor.NroDocumento + "/";  
            _DocElectronicoLinea01 = _DocElectronicoLinea01 + docElectronico.Receptor.TipoDocumento + "|"; //3
            _DocElectronicoLinea01 += string.Concat(em, "|"); 
            _DocElectronicoLinea01 = _DocElectronicoLinea01 + "|"; //docElectronico.Receptor.Telefono + "|";
            _DocElectronicoLinea01 = _DocElectronicoLinea01 + docElectronico.Receptor.Direccion + "|";
            _DocElectronicoLinea01 +=  "|"; //distrito
            _DocElectronicoLinea01 = _DocElectronicoLinea01 + docElectronico.Receptor.Provincia + "|";
            _DocElectronicoLinea01 = _DocElectronicoLinea01 + "|"; //docElectronico.Receptor.CodigoPostal + "|";
            _DocElectronicoLinea01 = _DocElectronicoLinea01 + docElectronico.Receptor.Departamento + "|";
            _DocElectronicoLinea01 = _DocElectronicoLinea01 + "|"; //país
            _DocElectronicoLinea01 += string.Concat(String.IsNullOrEmpty(docElectronico.Receptor.EMailTo) ? "NO" : "SI", "|");
            _DocElectronicoLinea01 = _DocElectronicoLinea01 + Environment.NewLine;

            //LINEA 2  DATOS GENERALES
            _DocElectronicoLinea02 = string.Empty;
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
            _DocElectronicoLinea02 = _DocElectronicoLinea02 + docElectronico.DescuentoNoGlobal.ToString("0.00").Replace(",", ".") + "|";
            _DocElectronicoLinea02 = _DocElectronicoLinea02 + docElectronico.TotalVenta.ToString("0.00").Replace(",", ".") + "|";
            _DocElectronicoLinea02 = _DocElectronicoLinea02 + docElectronico.Moneda + "|";

            foreach (OpenInvoicePeru.Comun.Dto.Modelos.DocumentoRelacionado relacionados in docElectronico.Relacionados)
            {
                _DocElectronicoLinea02 = _DocElectronicoLinea02 + relacionados.NroDocumento; 
            }
            _DocElectronicoLinea02 += "|"; 

            _DocElectronicoLinea02 = _DocElectronicoLinea02 + "|";
            _DocElectronicoLinea02 = _DocElectronicoLinea02 + docElectronico.Gratuitas.ToString("0.00").Replace(",", ".") + "|";
            _DocElectronicoLinea02 = _DocElectronicoLinea02 + string.Concat(docElectronico.Emisor.Direccion, " ", docElectronico.Emisor.Urbanizacion, " ", docElectronico.Emisor.Departamento, " ", docElectronico.Emisor.Distrito, " cp", docElectronico.Emisor.Ubigeo, "|");
            _DocElectronicoLinea02 = _DocElectronicoLinea02 + "|"; // TIPO_CAMBIO
            _DocElectronicoLinea02 = _DocElectronicoLinea02 + "|"; //docElectronico.MetodoPago // 23 METODO_PAGO
            _DocElectronicoLinea02 = _DocElectronicoLinea02 + "|"; // 24 Observaciones
            _DocElectronicoLinea02 = _DocElectronicoLinea02 + docElectronico.DescuentoGlobal.ToString("0.00").Replace(",", ".") + "|"; 
            _DocElectronicoLinea02 += (tipoDocumento.Equals("07")? docElectronico.Discrepancias.First().Tipo: string.Empty) +"|"; // CODIGO_NOTA
            _DocElectronicoLinea02 = _DocElectronicoLinea02 + Environment.NewLine;

            //LINEA 3 CONCEPTOS 
            _DocElectronicoLinea03 = string.Empty;
            foreach (OpenInvoicePeru.Comun.Dto.Modelos.DetalleDocumento detalle in docElectronico.Items)
            {
                _DocElectronicoLinea03 = _DocElectronicoLinea03 + "03|"; // 1 - Identificador de LInea
                _DocElectronicoLinea03 = _DocElectronicoLinea03 + detalle.UnidadMedida + "|";
                _DocElectronicoLinea03 = _DocElectronicoLinea03 + detalle.Cantidad.ToString("0.00").Replace(",", ".") + "|";
                _DocElectronicoLinea03 = _DocElectronicoLinea03 + detalle.Descripcion + "|";
                _DocElectronicoLinea03 = _DocElectronicoLinea03 + detalle.PrecioUnitario.ToString("0.00").Replace(",", ".") + "|";
                _DocElectronicoLinea03 = _DocElectronicoLinea03 + detalle.PrecioReferencial.ToString("0.00").Replace(",", ".") + "|";
                _DocElectronicoLinea03 = _DocElectronicoLinea03 + detalle.Impuesto.ToString("0.00").Replace(",", ".") + "|";
                _DocElectronicoLinea03 = _DocElectronicoLinea03 + detalle.ImpuestoSelectivo.ToString("0.00").Replace(",", ".") + "|";
                _DocElectronicoLinea03 = _DocElectronicoLinea03 + detalle.TotalVenta.ToString("0.00").Replace(",", ".") + "|";
                _DocElectronicoLinea03 += "||||";
                _DocElectronicoLinea03 += detalle.TipoImpuesto +"|";
                _DocElectronicoLinea03 += "|" + Environment.NewLine;

            }

            //LINEA 4  DATOS adicionales
            if (!tipoDocumento.Equals("07"))
            {
                _DocElectronicoLinea04 = "04|";
                _DocElectronicoLinea04 += "1|";
                _DocElectronicoLinea04 += "Observaciones|";
                _DocElectronicoLinea04 += docElectronico.Observaciones.Trim() + "|";
            }

            _DocElectronico = _DocElectronicoLinea0A + _DocElectronicoLinea01 + _DocElectronicoLinea02 + _DocElectronicoLinea03 + _DocElectronicoLinea04;

            return _DocElectronico;
        }

        public String FormatearResumenElectronico(String tipoDocumento, ResumenDiarioNuevo docResumen)
        {
            Debug.WriteLine("Método Formatear Resumen");
            //LINEA 1
            _DocElectronicoLinea01 = "01|RESUMEN||||||||||NO|";
            _DocElectronicoLinea01 = _DocElectronicoLinea01 + Environment.NewLine;

            // _DocElectronicoLinea02 = _DocElectronicoLinea02 + docResumen.  + "|";

            DateTime hoy = DateTime.Today;

            foreach (OpenInvoicePeru.Comun.Dto.Modelos.GrupoResumenNuevo detalle in docResumen.Resumenes)
            {
                String[] serieCorrelativo = detalle.IdDocumento.Split(new char[] { '-' });

                //Datos Genericos
                _DocElectronicoLinea02 = _DocElectronicoLinea02 + "02|"; //Identificador de LInea
                _DocElectronicoLinea02 = _DocElectronicoLinea02 + hoy.ToString("yyyy-MM-dd") + "|";
                _DocElectronicoLinea02 = _DocElectronicoLinea02 + docResumen.FechaEmision.Substring(0, 10) + "|";
                _DocElectronicoLinea02 = _DocElectronicoLinea02 + detalle.TipoDocumento + "|";
                _DocElectronicoLinea02 = _DocElectronicoLinea02 + serieCorrelativo[0] + "|"; //detalle.IdDocumento.Substring(0,4) 
                _DocElectronicoLinea02 += serieCorrelativo[1] + "|";
                _DocElectronicoLinea02 += serieCorrelativo[1] + "|";
                //Importes 
                _DocElectronicoLinea02 = _DocElectronicoLinea02 + detalle.Gravadas.ToString("0.00").Replace(",", ".") + "|";
                _DocElectronicoLinea02 = _DocElectronicoLinea02 + detalle.Exoneradas.ToString("0.00").Replace(",", ".") + "|";
                _DocElectronicoLinea02 = _DocElectronicoLinea02 + detalle.Inafectas.ToString("0.00").Replace(",", ".") + "|";
                _DocElectronicoLinea02 = _DocElectronicoLinea02 + "|"; // OTROS_CONCEPTOS
                _DocElectronicoLinea02 += string.Concat( detalle.TotalIsc.ToString("0.00")?.Replace(",", "."),  "|");
                _DocElectronicoLinea02 = _DocElectronicoLinea02 + detalle.TotalIgv.ToString("0.00").Replace(",", ".") + "|";
                _DocElectronicoLinea02 = _DocElectronicoLinea02 + detalle.TotalOtrosImpuestos.ToString("0.00").Replace(",", ".") + "|";
                _DocElectronicoLinea02 = _DocElectronicoLinea02 + detalle.TotalDescuentos.ToString("0.00").Replace(",", ".") + "|";
                _DocElectronicoLinea02 = _DocElectronicoLinea02 + detalle.TotalVenta.ToString("0.00").Replace(",", ".") + "|";
                //Oros Datos
                _DocElectronicoLinea02 = _DocElectronicoLinea02 + detalle.Moneda + "|";
                _DocElectronicoLinea02 += string.Concat(detalle.DocumentoRelacionado, "|");
                _DocElectronicoLinea02 = _DocElectronicoLinea02 + "|";
                _DocElectronicoLinea02 = _DocElectronicoLinea02 + detalle.Gratuitas.ToString("0.00").Replace(",", ".") + "|";
                _DocElectronicoLinea02 += string.Concat(docResumen.Emisor.Direccion, " ", docResumen.Emisor.Urbanizacion, " ", docResumen.Emisor.Departamento, " ", docResumen.Emisor.Distrito,  "|");
                _DocElectronicoLinea02 = _DocElectronicoLinea02 + "|"; // TIPO_CAMBIO
                _DocElectronicoLinea02 = _DocElectronicoLinea02 + "|"; // detalle.MetodoPago + // METODO_PAGO no obligatorio
                _DocElectronicoLinea02 = _DocElectronicoLinea02 + "|"; // Observaciones
                _DocElectronicoLinea02 = _DocElectronicoLinea02 + detalle.DescuentosGlobales.ToString("0.00").Replace(",", ".") + "|";
                _DocElectronicoLinea02 = _DocElectronicoLinea02 + "|"; // CODIGO_NOTA\n
                _DocElectronicoLinea02 = _DocElectronicoLinea02 + Environment.NewLine;
            }

            _DocElectronico = _DocElectronicoLinea01 + _DocElectronicoLinea02;

            return _DocElectronico;
        }

    }
}
