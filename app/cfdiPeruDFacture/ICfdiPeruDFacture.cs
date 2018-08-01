using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using cfdiPeruInterfaces;

namespace cfdiPeruDFacture
{
    public class ICfdiPeruDFacture : ICfdiPeru
    {

        private string _DocElectronico;

        private string _DocElectronicoLinea0A;
        private string _DocElectronicoLinea01;
        private string _DocElectronicoLinea02;
        private string _DocElectronicoLinea03;

        public String FormatearDocElectronico(String tipoDocumento, OpenInvoicePeru.Comun.Dto.Modelos.DocumentoElectronico docElectronico)
        {
            //LINEA 0A
            Console.WriteLine("Metodo Formatear Documento");
            _DocElectronicoLinea0A = "0A|MANUAL|";
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
            _DocElectronicoLinea01 = _DocElectronicoLinea01 + "PE" + "|";
            _DocElectronicoLinea01 = _DocElectronicoLinea01 + "NO" + "|";

            //LINEA 2  DATOS GENERALES
            _DocElectronicoLinea02 = _DocElectronicoLinea02 + "02|"; //Identificador de LInea
            _DocElectronicoLinea02 = _DocElectronicoLinea02 + docElectronico.FechaEmision.Substring(0,10) + "|";
            _DocElectronicoLinea02 = _DocElectronicoLinea02 +  "|"; // FECHA EMISION RESUMEN
            _DocElectronicoLinea02 = _DocElectronicoLinea02 + tipoDocumento + "|";
            _DocElectronicoLinea02 = _DocElectronicoLinea02 + docElectronico.IdDocumento + "|";
            _DocElectronicoLinea02 = _DocElectronicoLinea02 + "|"; // NRO INICIO SERIE -- Esto es para resumen
            _DocElectronicoLinea02 = _DocElectronicoLinea02 + "|"; // NRO FIN SERIE -- Esto es para resumen
            _DocElectronicoLinea02 = _DocElectronicoLinea02 + docElectronico.Gravadas + "|";
            _DocElectronicoLinea02 = _DocElectronicoLinea02 + docElectronico.Exoneradas + "|";
            _DocElectronicoLinea02 = _DocElectronicoLinea02 + docElectronico.Inafectas + "|";
            _DocElectronicoLinea02 = _DocElectronicoLinea02 + "|"; // OTROS_CONCEPTOS
            _DocElectronicoLinea02 = _DocElectronicoLinea02 + docElectronico.TotalIsc + "|";
            _DocElectronicoLinea02 = _DocElectronicoLinea02 + docElectronico.TotalIgv + "|";
            _DocElectronicoLinea02 = _DocElectronicoLinea02 + docElectronico.TotalOtrosTributos + "|";
            _DocElectronicoLinea02 = _DocElectronicoLinea02 + "|"; // DESCUENTOS NO GLOBALES
            _DocElectronicoLinea02 = _DocElectronicoLinea02 + docElectronico.TotalVenta + "|";
            _DocElectronicoLinea02 = _DocElectronicoLinea02 + docElectronico.Moneda + "|";
            _DocElectronicoLinea02 = _DocElectronicoLinea02 + docElectronico.Relacionados[1].NroDocumento + "|"; //VERIFICAR
            _DocElectronicoLinea02 = _DocElectronicoLinea02 + docElectronico.MontoPercepcion + "|";
            _DocElectronicoLinea02 = _DocElectronicoLinea02 + docElectronico.Gratuitas + "|";
            _DocElectronicoLinea02 = _DocElectronicoLinea02 + docElectronico.Emisor.Direccion + "|";
            _DocElectronicoLinea02 = _DocElectronicoLinea02 + "|"; // TIPO_CAMBIO
            _DocElectronicoLinea02 = _DocElectronicoLinea02 + "|"; // METODO_PAGO
            _DocElectronicoLinea02 = _DocElectronicoLinea02 + "|"; // Observaciones
            _DocElectronicoLinea02 = _DocElectronicoLinea02 + docElectronico.DescuentoGlobal + "|"; // VERIFICAR ESTE CAMPO
            _DocElectronicoLinea02 = _DocElectronicoLinea02 + "|"; // CODIGO_NOTA
            
            //LINEA 3 CONCEPTOS 
            
            foreach (OpenInvoicePeru.Comun.Dto.Modelos.DetalleDocumento detalle in docElectronico.Items)
            {
                _DocElectronicoLinea03 = "03|"; // 1 - Identificador de LInea
                //Console.WriteLine(detalle);
                _DocElectronicoLinea03 = _DocElectronicoLinea03 + detalle.UnidadMedida + "|";
                _DocElectronicoLinea03 = _DocElectronicoLinea03 + detalle.Cantidad + "|";
                _DocElectronicoLinea03 = _DocElectronicoLinea03 + detalle.Descripcion + "|";
                _DocElectronicoLinea03 = _DocElectronicoLinea03 + detalle.PrecioUnitario + "|";
                _DocElectronicoLinea03 = _DocElectronicoLinea03 + detalle.PrecioUnitario + "|";
                _DocElectronicoLinea03 = _DocElectronicoLinea03 + detalle.Impuesto + "|";
                _DocElectronicoLinea03 = _DocElectronicoLinea03 + detalle.ImpuestoSelectivo + "|";
                _DocElectronicoLinea03 = _DocElectronicoLinea03 + detalle.Suma + "|";

            }


            _DocElectronico = _DocElectronicoLinea0A + _DocElectronicoLinea01 + _DocElectronicoLinea02 + _DocElectronicoLinea03;








            return _DocElectronico;
        }

        public String FormatearResumenElectronico(String tipoDocumento, OpenInvoicePeru.Comun.Dto.Modelos.ResumenDiarioNuevo docResumen)
        {
            Console.WriteLine("Metodo Formatear Resumen");
            return "VUELTA RESUMEN";
        }
    }
}
