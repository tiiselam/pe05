using cfdiEntidadesGP;
using cfdiPeruInterfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using cfdiPeruDFactureUbl21.DFactureUbl21;
using System.Xml.Linq;
//using System.Xml;

namespace cfdiPeruOperadorServiciosElectronicos
{
    public class WebServicesOSE : ICfdiMetodosWebService
    {
        private DocumentoElectronico DocEnviarWS;
        private ServiceClient ServicioWS = new ServiceClient();
        string debug_xml;
        string MensajeError;

        public WebServicesOSE(string URLwebServPAC)
        {
            ServicioWS.Endpoint.Address = new System.ServiceModel.EndpointAddress(URLwebServPAC) ;
        }

        private DocumentoElectronico ArmaDocumentoEnviarWS(DocumentoVentaGP documentoGP)
        {
            DocEnviarWS = new DocumentoElectronico();
            int i = 0; // Variable para loopear
            int correlativo = 1; // Variable para corre de productos;
            Boolean DescItemIGV = true;
            debug_xml = "";

            //Seccion emisor
            DocEnviarWS.emisor = new Emisor();

            DocEnviarWS.emisor.ruc = documentoGP.DocVenta.emisorNroDoc;
            //DocEnviarWS.emisor.nombreComercial = documentoGP.DocVenta.emisorNombre;
            //DocEnviarWS.emisor.lugarExpedicion = documentoGP.DocVenta.emmisor**
            /*DocEnviarWS.emisor.domicilioFiscal = documentoGP.DocVenta.emisorDireccion;
            DocEnviarWS.emisor.urbanizacion = documentoGP.DocVenta.emisorUrbanizacion;
            DocEnviarWS.emisor.distrito = documentoGP.DocVenta.emisorDistrito;
            DocEnviarWS.emisor.provincia = documentoGP.DocVenta.emisorProvincia;
            DocEnviarWS.emisor.departamento = documentoGP.DocVenta.emisorDepartamento;
            DocEnviarWS.emisor.codigoPais = documentoGP.DocVenta.emisorCodPais;
            DocEnviarWS.emisor.ubigeo = documentoGP.DocVenta.emisorUbigeo;*/

            debug_xml = "<EMISOR>" + DocEnviarWS.emisor.ruc + "\r\n";

            // SECCION RECEPTOR
            DocEnviarWS.receptor = new Receptor();
            DocEnviarWS.receptor.email = documentoGP.DocVenta.emailTo;
            DocEnviarWS.receptor.notificar = documentoGP.DocVenta.emailTo.Trim() == string.Empty ? "NO" : "SI";
            DocEnviarWS.receptor.numDocumento = documentoGP.DocVenta.receptorNroDoc;
            DocEnviarWS.receptor.direccion = documentoGP.DocVenta.receptorDireccion;
            DocEnviarWS.receptor.departamento = documentoGP.DocVenta.receptorCiudad;
            //DocEnviarWS.receptor.distrito = documentoGP.DocVenta.recep
            DocEnviarWS.receptor.pais = documentoGP.DocVenta.receptorPais;
            DocEnviarWS.receptor.provincia = documentoGP.DocVenta.receptorProvincia;
            DocEnviarWS.receptor.razonSocial = documentoGP.DocVenta.receptorNombre;
            //     DocEnviarWS.receptor.telefono = documentoGP.DocVenta.
            DocEnviarWS.receptor.tipoDocumento = documentoGP.DocVenta.receptorTipoDoc;
            //    DocEnviarWS.receptor.ubigeo = documentoGP.DocVenta.rece

            debug_xml = debug_xml + "<RECEPTOR>" + DocEnviarWS.receptor.tipoDocumento + ":" + DocEnviarWS.receptor.numDocumento + "\r\n";
            debug_xml = debug_xml + "   <RazonSocial>" + DocEnviarWS.receptor.razonSocial + "\r\n";

            // SECCION COMROBANTE
            DocEnviarWS.codigoTipoOperacion = documentoGP.DocVenta.tipoOperacion;
            DocEnviarWS.correlativo = documentoGP.DocVenta.numero;
            //DocEnviarWS.correlativo = "10000106"; // se usa para reenviar comprobante.
            DocEnviarWS.fechaEmision = documentoGP.DocVenta.fechaEmision.ToString("yyyy-MM-dd");
            DocEnviarWS.fechaVencimiento = documentoGP.DocVenta.fechaVencimiento.ToString("yyyy-MM-dd");
            DocEnviarWS.horaEmision = documentoGP.DocVenta.horaEmision;
            DocEnviarWS.idTransaccion = documentoGP.DocVenta.idDocumento;
            DocEnviarWS.serie = documentoGP.DocVenta.serie;
            DocEnviarWS.tipoDocumento = documentoGP.DocVenta.tipoDocumento;
            {
                debug_xml = debug_xml + "<COMPROBANTE>" + DocEnviarWS.serie + "-" + DocEnviarWS.correlativo + "\r\n";
                debug_xml = debug_xml + "   <tipoDocumento>" + DocEnviarWS.tipoDocumento + "\r\n";
                debug_xml = debug_xml + "   <codigoTipoOperacion>" + DocEnviarWS.codigoTipoOperacion + "\r\n";
                debug_xml = debug_xml + "   <FechaEmisio>" + DocEnviarWS.fechaEmision + "\r\n";
                debug_xml = debug_xml + "   <fechaVencimiento>" + DocEnviarWS.fechaVencimiento + "\r\n";
                debug_xml = debug_xml + "   <horaEmision>" + DocEnviarWS.horaEmision + "\r\n";
                debug_xml = debug_xml + "   <idTransaccion>" + DocEnviarWS.idTransaccion + "\r\n";
                debug_xml = debug_xml + "<FIN COMPROBANTE>" + "\r\n";
            }
            // FIN SECCION COMPROBANTE

            // SECCION Relacionado. VER mas adelante
            debug_xml = debug_xml + "<RELACIONADO NOTAS>" + documentoGP.LDocVentaRelacionados.Count() + "\r\n";
            if (string.IsNullOrEmpty(documentoGP.DocVenta.cRelacionadoTipoDocAfectado))
            {
                debug_xml = debug_xml + "<SIN DOC RELACIONADO>" + "\r\n";
            }
            else
            {
                if (DocEnviarWS.tipoDocumento == "07" || DocEnviarWS.tipoDocumento == "08")
                {
                    var relacionadoN = new RelacionadoNotas();

                    relacionadoN.codigoTipoNota = documentoGP.DocVenta.infoRelNotasCodigoTipoNota;
                    relacionadoN.observaciones = documentoGP.DocVenta.infoRelNotasObservaciones;
                    relacionadoN.numeroDocAfectado = documentoGP.DocVenta.cRelacionadoNumDocAfectado.Trim();
                    relacionadoN.tipoDocAfectado = documentoGP.DocVenta.cRelacionadoTipoDocAfectado;

                    DocEnviarWS.relacionadoNotas = new RelacionadoNotas();
                    DocEnviarWS.relacionadoNotas = relacionadoN;
                    debug_xml = debug_xml + "  <TIPOAFECTADO>" + DocEnviarWS.relacionadoNotas.tipoDocAfectado + "\r\n";
                    debug_xml = debug_xml + "  <DOCAFECTADO>" + DocEnviarWS.relacionadoNotas.numeroDocAfectado + "\r\n";
                    debug_xml = debug_xml + "  <NOTA>" + DocEnviarWS.relacionadoNotas.codigoTipoNota + "\r\n";
                    debug_xml = debug_xml + "  <observaciones>" + DocEnviarWS.relacionadoNotas.observaciones + "\r\n";
                    debug_xml = debug_xml + "<FIN RELACIONADO NOTAS>\r\n";
                }
                else
                {
                    if (DocEnviarWS.tipoDocumento == "01")
                    {
                        var relacionado = new Relacionado();
                        relacionado.numeroDocRelacionado = documentoGP.DocVenta.cRelacionadoNumDocAfectado.Trim();
                        relacionado.tipoDocRelacionado = documentoGP.DocVenta.cRelacionadoTipoDocAfectado;

                        DocEnviarWS.relacionado = new Relacionado[1];
                        DocEnviarWS.relacionado[0] = relacionado;

                        debug_xml = debug_xml + "  <TIPOAFECTADO>" + DocEnviarWS.relacionado[0].numeroDocRelacionado + "\r\n";
                        debug_xml = debug_xml + "  <DOCAFECTADO>" + DocEnviarWS.relacionado[0].tipoDocRelacionado + "\r\n";
                        debug_xml = debug_xml + "<FIN RELACIONADO NOTAS>\r\n";
                    }
                }

            }


            // debug_xml = debug_xml + "<RELACIONADO NOTAS>" + documentoGP.LDocVentaRelacionados.Count() + "\r\n";
            foreach (vwCfdiRelacionados relacionado_gp in documentoGP.LDocVentaRelacionados)
            {
                if (DocEnviarWS.tipoDocumento == "07" || DocEnviarWS.tipoDocumento == "08")
                {
                    var relacionadoN = new RelacionadoNotas();

                    relacionadoN.codigoTipoNota = documentoGP.DocVenta.infoRelNotasCodigoTipoNota;
                    relacionadoN.observaciones = documentoGP.DocVenta.infoRelNotasObservaciones;
                    relacionadoN.numeroDocAfectado = relacionado_gp.sopnumbeTo.Trim();
                    relacionadoN.tipoDocAfectado = relacionado_gp.tipoDocumento;

                    DocEnviarWS.relacionadoNotas = new RelacionadoNotas();
                    DocEnviarWS.relacionadoNotas = relacionadoN;

                    debug_xml = debug_xml + "  <TIPOAFECTADO>" + DocEnviarWS.relacionadoNotas.tipoDocAfectado + "\r\n";
                    debug_xml = debug_xml + "  <DOCAFECTADO>" + DocEnviarWS.relacionadoNotas.numeroDocAfectado + "\r\n";
                    debug_xml = debug_xml + "  <NOTA>" + DocEnviarWS.relacionadoNotas.codigoTipoNota + "\r\n";
                    debug_xml = debug_xml + "  <observaciones>" + DocEnviarWS.relacionadoNotas.observaciones + "\r\n";
                    debug_xml = debug_xml + "<FIN RELACIONADO NOTAS>\r\n";
                }
                else
                {
                    if (DocEnviarWS.tipoDocumento == "01")
                    {
                        var relacionado = new Relacionado();
                        relacionado.numeroDocRelacionado = relacionado_gp.sopnumbeTo.Trim(); ;
                        relacionado.tipoDocRelacionado = relacionado_gp.tipoDocumento;

                        DocEnviarWS.relacionado = new Relacionado[1];
                        DocEnviarWS.relacionado[0] = relacionado;

                        debug_xml = debug_xml + "  <TIPOAFECTADO>" + DocEnviarWS.relacionado[0].numeroDocRelacionado + "\r\n";
                        debug_xml = debug_xml + "  <DOCAFECTADO>" + DocEnviarWS.relacionado[0].tipoDocRelacionado + "\r\n";
                        debug_xml = debug_xml + "<FIN RELACIONADO NOTAS>\r\n";
                    }

                }

                //Aumenta contadoresDocEnviarWS.producto[i].
                i++;
                correlativo++;
            }


            // SECCION Producto.
            DocEnviarWS.producto = new Producto[documentoGP.LDocVentaConceptos.Count()];
            debug_xml = debug_xml + "<CANT PROD>" + DocEnviarWS.producto.Count() + "\r\n";
            i = 0; correlativo = 1;
            foreach (vwCfdiConceptos producto_gp in documentoGP.LDocVentaConceptos)
            {
                var producto = new Producto();

                producto.cantidad = producto_gp.cantidad.ToString();
                producto.codigoPLU = producto_gp.ITEMNMBR;
                producto.codigoPLUSunat = producto_gp.claveProdSunat.Trim();
                producto.descripcion = producto_gp.ITEMDESC;
                producto.montoTotalImpuestoItem = producto_gp.montoIva.ToString("0.00");
                producto.precioVentaUnitarioItem = producto_gp.precioUniConIva.ToString();
                producto.unidadMedida = producto_gp.udemSunat;
                producto.valorReferencialUnitario = producto_gp.precioUniConIva.ToString();
                producto.valorUnitarioBI = producto_gp.valorUni.ToString();
                producto.valorVentaItemQxBI = string.Format("{0,14:0.00}", producto_gp.importe).Trim();
                producto.numeroOrden = correlativo.ToString();
                {
                    debug_xml = debug_xml + "<PRODUCTO>" + correlativo + "\r\n";

                    debug_xml = debug_xml + "   <cantidad>" + producto.cantidad + "\r\n";
                    debug_xml = debug_xml + "   <codigoPLU>" + producto.codigoPLU + "\r\n";
                    debug_xml = debug_xml + "   <codigoPLUSunat>" + producto.codigoPLUSunat + "\r\n";
                    debug_xml = debug_xml + "   <descripcion>" + producto.descripcion + "\r\n";
                    debug_xml = debug_xml + "   <montoTotalImpuestoItem>" + producto.montoTotalImpuestoItem + "\r\n";
                    debug_xml = debug_xml + "   <precioVentaUnitarioItem>" + producto.precioVentaUnitarioItem + "\r\n";
                    debug_xml = debug_xml + "   <unidadMedida>" + producto.unidadMedida + "\r\n";
                    debug_xml = debug_xml + "   <valorReferencialUnitario>" + producto.valorReferencialUnitario + "\r\n";
                    debug_xml = debug_xml + "   <valorUnitarioBI>" + producto.valorUnitarioBI + "\r\n";
                    debug_xml = debug_xml + "   <valorVentaItemQxBI>" + producto.valorVentaItemQxBI + "\r\n";
                    debug_xml = debug_xml + "   <numeroOrden>" + producto.numeroOrden + "\r\n";
                }

                // SECCION PRODUCTO IGV
                producto.IGV = new ProductoIGV();
                switch (producto_gp.tipoAfectacion.ToString().Trim())
                {
                    case "20":
                        producto.IGV.baseImponible = producto_gp.montoImponibleExonera.ToString("0.00");
                        break;
                    case "21":
                        producto.IGV.baseImponible = producto_gp.montoImponibleGratuito.ToString("0.00");
                        break;
                    case "30":
                        producto.IGV.baseImponible = producto_gp.montoImponibleInafecto.ToString("0.00");
                        break;
                    case "35":
                        producto.IGV.baseImponible = producto_gp.montoImponibleInafecto.ToString("0.00");
                        break;
                    case "40":
                        producto.IGV.baseImponible = producto_gp.montoImponibleExporta.ToString("0.00");
                        break;
                    default:
                        producto.IGV.baseImponible = producto_gp.montoImponibleIva.ToString("0.00");
                        break;
                }

                producto.IGV.monto = producto_gp.montoIva.ToString("0.00");
                producto.IGV.tipo = producto_gp.tipoAfectacion.ToString().Trim();

                if (!string.IsNullOrEmpty(documentoGP.DocVenta.infoRelNotasCodigoTipoNota))
                {
                    producto.IGV.porcentaje = string.Format("{0,8:0.00}", producto_gp.porcentajeIva * 100).Trim();
                }
                else
                {
                    producto.IGV.porcentaje = string.Format("{0,8:0.00}", producto_gp.porcentajeIva * 100).Trim();
                }
                {

                    debug_xml = debug_xml + "   <IGV>\r\n";
                    debug_xml = debug_xml + "       <baseImponible>" + producto.IGV.baseImponible + "\r\n";
                    debug_xml = debug_xml + "           <baseImponibleIVA>" + producto_gp.montoImponibleIva.ToString("0.00") + "\r\n";
                    debug_xml = debug_xml + "           <baseImponibleExo>" + producto_gp.montoImponibleExonera.ToString("0.00") + "\r\n";
                    debug_xml = debug_xml + "           <baseImponibleExp>" + producto_gp.montoImponibleExporta.ToString("0.00") + "\r\n";
                    debug_xml = debug_xml + "           <baseImponibleGra>" + producto_gp.montoImponibleGratuito.ToString("0.00") + "\r\n";
                    debug_xml = debug_xml + "           <baseImponibleIna>" + producto_gp.montoImponibleInafecto.ToString("0.00") + "\r\n";
                    debug_xml = debug_xml + "       <porcentaje>" + producto.IGV.porcentaje + "\r\n";
                    debug_xml = debug_xml + "       <monto>" + producto.IGV.monto + "\r\n";
                    debug_xml = debug_xml + "       <tipo>" + producto.IGV.tipo + "\r\n";
                }

                //SECCION PRODUCTO DESCUENTO
                if (producto_gp.descuento != 0)
                {
                    producto.descuento = new ProductoDescuento();
                    producto.descuento.baseImponible = string.Format("{0,14:0.00}", producto_gp.descuentoBaseImponible).Trim();
                    producto.descuento.monto = string.Format("{0,14:0.00}", producto_gp.descuento).Trim();
                    producto.descuento.porcentaje = string.Format("{0,8:0.00000}", producto_gp.descuentoPorcentaje * 100).Trim();
                    producto.descuento.codigo = producto_gp.descuentoCodigo;

                    if (producto_gp.descuentoCodigo == "01")
                    {
                        DescItemIGV = false;
                    }

                    {
                        debug_xml = debug_xml + "   <DESC ITEM>" + "\r\n";
                        debug_xml = debug_xml + "       <baseImponible>" + producto.descuento.baseImponible + "\r\n";
                        debug_xml = debug_xml + "       <monto>" + producto.descuento.monto + "\r\n";
                        debug_xml = debug_xml + "       <porcentaje>" + producto.descuento.porcentaje + "\r\n";
                        debug_xml = debug_xml + "       <codigo>" + producto.descuento.codigo + "\r\n";
                    }

                }

                DocEnviarWS.producto[i] = producto;
                debug_xml = debug_xml + "  <PRODUCTO>" + DocEnviarWS.producto[i].codigoPLU + " Imp:" + DocEnviarWS.producto[i].valorVentaItemQxBI + "\r\n";
                debug_xml = debug_xml + "       IGVporc: " + DocEnviarWS.producto[i].IGV.porcentaje + "\r\n";

                //Aumenta contadoresDocEnviarWS.producto[i].
                i++;
                correlativo++;
            }
            debug_xml = debug_xml + "<FIN PRODUCTOS>\r\n";

            // SECCION Descuentos Globales
            if (documentoGP.DocVenta.descuentoGlobalMonto != 0)
            {
                DocEnviarWS.descuentosGlobales = new DescuentosGlobales();

                DocEnviarWS.descuentosGlobales.baseImponible = documentoGP.DocVenta.descuentoGlobalImponible.ToString("0.00");
                DocEnviarWS.descuentosGlobales.monto = documentoGP.DocVenta.descuentoGlobalMonto.ToString("0.00");
                if (DescItemIGV)
                {
                    DocEnviarWS.descuentosGlobales.motivo = "02";
                }
                else
                {
                    DocEnviarWS.descuentosGlobales.motivo = "03";
                }
                DocEnviarWS.descuentosGlobales.porcentaje = string.Format("{0,8:0.00000}", documentoGP.DocVenta.descuentoGlobalPorcentaje).Trim();

                {
                    debug_xml = debug_xml + "<DESCUENTOS GLOBALES>" + "\r\n";
                    debug_xml = debug_xml + "    <baseImponible" + DocEnviarWS.descuentosGlobales.baseImponible + "\r\n";
                    debug_xml = debug_xml + "    <porcentaje>" + DocEnviarWS.descuentosGlobales.porcentaje + "\r\n";
                    debug_xml = debug_xml + "    <monto>" + DocEnviarWS.descuentosGlobales.monto + "\r\n";
                    debug_xml = debug_xml + "    <motivo>" + DocEnviarWS.descuentosGlobales.motivo + "\r\n";
                    debug_xml = debug_xml + "<FIN DESCUENTOS GLOBALES>" + "\r\n";
                }
            }
            else
            {
                debug_xml = debug_xml + "<SIN DESCUENTOS GLOBALES>" + "\r\n";
            }

            //SECCION DETRACCIONES
            if (string.IsNullOrEmpty(documentoGP.DocVenta.codigoDetraccion) || documentoGP.DocVenta.codigoDetraccion.Trim() == "00")
            {
                debug_xml = debug_xml + "<SIN DETRACCIONES>" + "\r\n";
            }
            else
            {
                debug_xml = debug_xml + "<DETRACCIONES>" + "\r\n";

                var detracciones = new Detraccion();
                detracciones.codigo = documentoGP.DocVenta.codigoDetraccion.Trim();
                detracciones.medioPago = documentoGP.DocVenta.medioPagoDetraccion;
                detracciones.monto = string.Format("{0,14:0.00}", documentoGP.DocVenta.montoDetraccion).Trim();
                detracciones.numCuentaBancodelaNacion = documentoGP.DocVenta.numCuentaBancoNacion.Trim();
                detracciones.porcentaje = string.Format("{0,8:0.00}", documentoGP.DocVenta.porcentajeDetraccion).Trim();

                DocEnviarWS.detraccion = new Detraccion[1];
                DocEnviarWS.detraccion[0] = detracciones;

                {
                    debug_xml = debug_xml + "    <codigo>" + DocEnviarWS.detraccion[0].codigo + "\r\n";
                    debug_xml = debug_xml + "    <medioPago>" + DocEnviarWS.detraccion[0].medioPago + "\r\n";
                    debug_xml = debug_xml + "    <monto>" + DocEnviarWS.detraccion[0].monto + "\r\n";
                    debug_xml = debug_xml + "    <numCuentaBancodelaNacion>" + DocEnviarWS.detraccion[0].numCuentaBancodelaNacion + "\r\n";
                    debug_xml = debug_xml + "    <porcentaje>" + DocEnviarWS.detraccion[0].porcentaje + "\r\n";
                }

                debug_xml = debug_xml + "<FIN DETRACCIONES>" + DocEnviarWS.detraccion[0].monto + "\r\n";
            }


            //SECCION TOTALES
            debug_xml = debug_xml + "<TOTALES>" + "\r\n";
            DocEnviarWS.totales = new Totales();
            DocEnviarWS.totales.importeTotalPagar = documentoGP.DocVenta.montoTotalVenta.ToString("0.00");
            DocEnviarWS.totales.importeTotalVenta = documentoGP.DocVenta.montoTotalVenta.ToString("0.00");
            DocEnviarWS.totales.montoTotalImpuestos = documentoGP.DocVenta.montoTotalImpuestos.ToString("0.00");
            DocEnviarWS.totales.subtotalValorVenta = string.Format("{0,14:0.00}", documentoGP.DocVenta.montoSubtotalValorVenta).Trim();
            DocEnviarWS.totales.sumaTotalDescuentosporItem = string.Format("{0,14:0.00}", documentoGP.DocVenta.montoTotalDescuentosPorItem).Trim();
            DocEnviarWS.totales.sumatoriaImpuestosOG = documentoGP.DocVenta.montoTotalImpuOperGratuitas.ToString("0.00");
            DocEnviarWS.totales.totalIGV = documentoGP.DocVenta.montoTotalIgv.ToString("0.00");

            {
                debug_xml = debug_xml + "   <importeTotalPagar>" + DocEnviarWS.totales.importeTotalPagar + "\r\n";
                debug_xml = debug_xml + "   <importeTotalVenta>" + DocEnviarWS.totales.importeTotalVenta + "\r\n";
                debug_xml = debug_xml + "   <montoTotalImpuestos>" + DocEnviarWS.totales.montoTotalImpuestos + "\r\n";
                debug_xml = debug_xml + "   <subtotalValorVenta>" + DocEnviarWS.totales.subtotalValorVenta + "\r\n";
                debug_xml = debug_xml + "   <sumaTotalDescuentosporItem>" + DocEnviarWS.totales.sumaTotalDescuentosporItem + "\r\n";
                debug_xml = debug_xml + "   <sumatoriaImpuestosOG>" + DocEnviarWS.totales.sumatoriaImpuestosOG + "\r\n";
                debug_xml = debug_xml + "   <totalIGV>" + DocEnviarWS.totales.totalIGV + "\r\n";
                debug_xml = debug_xml + "<FIN TOTALES>" + "\r\n";
            }

            //SECCION SUBTOTALES

            DocEnviarWS.totales.subtotal = new Subtotal();
            DocEnviarWS.totales.subtotal.IGV = documentoGP.DocVenta.montoSubtotalIvaImponible.ToString("0.00");
            DocEnviarWS.totales.subtotal.exoneradas = documentoGP.DocVenta.montoSubtotalExonerado.ToString("0.00");
            DocEnviarWS.totales.subtotal.exportacion = documentoGP.DocVenta.montoSubtotalExportacion.ToString("0.00");
            DocEnviarWS.totales.subtotal.gratuitas = documentoGP.DocVenta.montoSubtotalGratuito.ToString("0.00");
            DocEnviarWS.totales.subtotal.inafectas = documentoGP.DocVenta.montoSubtotalInafecto.ToString("0.00");
            {
                debug_xml = debug_xml + "   <SUBTOTALES>" + "\r\n";
                debug_xml = debug_xml + "       <IGV>" + DocEnviarWS.totales.subtotal.IGV + "\r\n";
                debug_xml = debug_xml + "       <exoneradas>" + DocEnviarWS.totales.subtotal.exoneradas + "\r\n";
                debug_xml = debug_xml + "       <exportacion>" + DocEnviarWS.totales.subtotal.exportacion + "\r\n";
                debug_xml = debug_xml + "       <gratuitas>" + DocEnviarWS.totales.subtotal.gratuitas + "\r\n";
                debug_xml = debug_xml + "       <inafectas>" + DocEnviarWS.totales.subtotal.inafectas + "\r\n";
                debug_xml = debug_xml + "   <FIN SUBTOTALES>" + "\r\n";
            }

            //SECCION PAGO
            DocEnviarWS.pago = new Pago();
            DocEnviarWS.pago.moneda = documentoGP.DocVenta.moneda;
            DocEnviarWS.pago.tipoCambio = documentoGP.DocVenta.xchgrate.ToString("0.00000");
            DocEnviarWS.pago.fechaInicio = DateTime.Now.ToString("yyyy-MM-dd");
            DocEnviarWS.pago.fechaFin = DateTime.Now.ToString("yyyy-MM-dd");

            //SECCION PERSONALIZACION PDF
            if (!string.IsNullOrEmpty(documentoGP.LeyendasXml))
            {
                XElement leyendasX = XElement.Parse(documentoGP.LeyendasXml);
                int numLeyendas = leyendasX.Elements().Count();
                if (!string.IsNullOrEmpty(leyendasX.Elements().FirstOrDefault().Attribute("S").Value) && 
                    !string.IsNullOrEmpty(leyendasX.Elements().FirstOrDefault().Attribute("T").Value) &&
                    !string.IsNullOrEmpty(leyendasX.Elements().FirstOrDefault().Attribute("V").Value)
                    )
                {
                    DocEnviarWS.personalizacionPDF = new PersonalizacionPDF[numLeyendas];
                    int idx = 0;
                    foreach (XElement child in leyendasX.Elements())
                    {
                        DocEnviarWS.personalizacionPDF[idx] = new PersonalizacionPDF();
                        DocEnviarWS.personalizacionPDF[idx].seccion = child.Attribute("S").Value;
                        DocEnviarWS.personalizacionPDF[idx].titulo = child.Attribute("T").Value;
                        DocEnviarWS.personalizacionPDF[idx].valor = child.Attribute("V").Value;
                        idx++;
                    }
                }
            }
            {
                debug_xml = debug_xml + "<PAGO>" + "\r\n";
                debug_xml = debug_xml + "   <moneda>" + DocEnviarWS.pago.moneda + "\r\n";
                debug_xml = debug_xml + "    <tipoCambio>" + DocEnviarWS.pago.tipoCambio + "\r\n";
                debug_xml = debug_xml + "<FIN PAGO>" + "\r\n";
            }

            debug_xml = debug_xml + "<LLAMADA>" ;

            return DocEnviarWS;
        }

        public string TimbraYEnviaASunat(string ruc, string usuario, string usuarioPassword, DocumentoVentaGP documentoGP)
        {
            var docWs = ArmaDocumentoEnviarWS(documentoGP);

            var response = ServicioWS.Enviar(ruc, usuario, usuarioPassword, docWs);

                if (response.codigo == 0)
                {
                        
                    byte[] converbyte = Convert.FromBase64String(response.xml.ToString());
                    return System.Text.Encoding.UTF8.GetString(converbyte);
                    //return response.xml.ToString();

                    /*return "Mensaje XML: " + response.mensaje + Environment.NewLine +
                                                        "Código error: " + response.codigo + Environment.NewLine +
                                                        "Estatus: " + response.estatus + Environment.NewLine +
                                                        "Hora: " + response.hora + Environment.NewLine +
                                                        "Id Transacción: " + response.idtransaccion + Environment.NewLine +
                                                        "Numeración: " + response.numeracion + Environment.NewLine +
                                                        "CRC: " + response.crc + Environment.NewLine +
                                                        "DebugXML: " + debug_xml + Environment.NewLine  + 
                                                        "XML: " + converbyte.ToString();*/


                }
                else
                {
                    //using (StreamWriter sw = File.CreateText(ruc + documentoGP.DocVenta.serie + documentoGP.DocVenta.numero + DateTime.Today.ToLongDateString()))
                    //{
                    //    sw.WriteLine(debug_xml);
                    //}
                    if (response.codigo == 202 || response.codigo == 207)
                        throw new ArgumentException(response.codigo.ToString() + " - " + response.mensaje );
                    else
                        throw new TimeoutException("Excepción al conectarse con el Web Service de Facturación. " + response.codigo.ToString() + " - " + response.mensaje + " " + debug_xml);

                }
            
        }

        public async Task<string> TimbraYEnviaASunatAsync(string ruc, string usuario, string usuarioPassword, DocumentoVentaGP documentoGP)
        {

            var docWs = ArmaDocumentoEnviarWS(documentoGP);

            var response = await ServicioWS.EnviarAsync(ruc, usuario, usuarioPassword, docWs);

            if (response.codigo == 0)
            {
                byte[] converbyte = Convert.FromBase64String(response.xml.ToString());
                return System.Text.Encoding.UTF8.GetString(converbyte);

            }
            else
            {
                if (response.codigo == 202 || response.codigo == 207)
                    throw new ArgumentException(response.codigo.ToString() + " - " + response.mensaje);
                else
                    throw new TimeoutException("Excepción al conectarse con el Web Service de Facturación. [TimbraYEnviaASunatAsync] " + response.codigo.ToString() + " - " + response.mensaje + " " + debug_xml + " ruc: " + ruc + " USu: " + usuario + "/" + usuarioPassword );

            }

        }

        public async Task<string> ObtienePDFdelOSEAsync(string ruc, string usuario, string usuarioPassword, string tipoDoc, string serie, string correlativo, string ruta, string nombreArchivo, string extension)
        {
            string rutaYNomArchivoPDF = Path.Combine(ruta, nombreArchivo + extension);

                var response_descarga = await ServicioWS.DescargaArchivoAsync(usuario, usuarioPassword, ruc + "-" + tipoDoc + "-" + serie + "-" + correlativo, "PDF");
           
                if (response_descarga.codigo == 0)
                {

                    byte[] converbyte = Convert.FromBase64String(response_descarga.archivo.ToString());

                    using (FileStream SourceStream = File.Open(rutaYNomArchivoPDF, FileMode.OpenOrCreate))
                    {
                        SourceStream.Seek(0, SeekOrigin.End);
                        await SourceStream.WriteAsync(converbyte, 0, converbyte.Length);
                    }

                    return rutaYNomArchivoPDF;
                }
                else
                {
                    throw new Exception("Excepción al descargar el PDF del servicio web. " + response_descarga.codigo.ToString() + " - " + response_descarga.mensaje + " " + response_descarga.numeracion);
                }
        }

        public string ObtienePDFdelOSE(string ruc, string usuario, string usuarioPassword, string tipoDoc, string serie, string correlativo, string ruta, string nombreArchivo, string extension)
        {
            string  rutaYNomArchivoPDF = ruta + nombreArchivo + extension;

            try
            {
                var response_descarga = ServicioWS.DescargaArchivo(usuario, usuarioPassword, ruc + "-" + tipoDoc + "-" + serie + "-" + correlativo, "PDF");

                if (response_descarga.codigo == 0)
                {

                    byte[] converbyte = Convert.FromBase64String(response_descarga.archivo.ToString());

                    if (!Directory.Exists(ruta))
                    {
                        DirectoryInfo di = Directory.CreateDirectory(ruta);


                    }

                    using (FileStream Writer = new FileStream(rutaYNomArchivoPDF, FileMode.Create, FileAccess.Write))
                        Writer.Write(converbyte, 0, converbyte.Length);

                    return rutaYNomArchivoPDF;

                }
                else
                {
                    //throw new Exception(rutaYNomArchivoPDF+ " || " + response_descarga.mensaje + 2"||" + ruc + "-" + tipoDoc + "-" + serie + "-" + correlativo);
                    return rutaYNomArchivoPDF + "||" + response_descarga.codigo +"-"+ response_descarga.mensaje + "/" + response_descarga.numeracion + "||" + ruc + "-" + tipoDoc + "-" + serie + "-" + correlativo;
                }
            }
            catch (DirectoryNotFoundException)
            {
                string smsj = "Verifique la existencia de la carpeta indicada en la configuración de Ruta de archivos Xml de GP. La ruta de la carpeta no existe: " + rutaYNomArchivoPDF;
                throw new DirectoryNotFoundException(smsj);
            }
            catch (IOException)
            {
                string smsj = "Verifique permisos de escritura en la carpeta: " + rutaYNomArchivoPDF + ". No se pudo guardar el archivo xml.";
                throw new IOException(smsj);
            }
            catch (Exception eAFE)
            {
                string smsj;
                if (eAFE.Message.Contains("denied"))
                    smsj = "Elimine el archivo pdf antes de volver a generar uno nuevo. Luego vuelva a intentar. " + eAFE.Message;
                else
                    smsj = "Contacte a su administrador. No se pudo guardar el archivo XML. " + eAFE.Message + Environment.NewLine + eAFE.StackTrace;
                throw new IOException(smsj);
            }

            throw new NotImplementedException();
        }

        public async Task<string> ObtieneXMLdelOSEAsync(string ruc, string usuario, string usuarioPassword, string tipoDoc, string serie, string correlativo)
        {
                var response_descarga = await ServicioWS.DescargaArchivoAsync(usuario, usuarioPassword, ruc + "-" + tipoDoc + "-" + serie + "-" + correlativo, "XML");

                if (response_descarga.codigo == 0)
                {
                    return response_descarga.archivo.ToString();
                }
                else
                {
                    throw new Exception("Excepción al descargar el XML del servicio web. " + response_descarga.codigo.ToString() + " - " + response_descarga.mensaje + " " + response_descarga.numeracion);
                }

        }

        public string ObtieneXMLdelOSE(string ruc, string usuario, string usuarioPassword, string tipoDoc, string serie, string correlativo)
        {

            string MsjError;
            try
            {
                var response_descarga = ServicioWS.DescargaArchivo(usuario, usuarioPassword, ruc + "-" + tipoDoc + "-" + serie + "-" + correlativo, "XML");

                if (response_descarga.codigo == 0)
                {

                    return response_descarga.archivo.ToString();

                }
                else
                {
                    MsjError = "Mensaje: " + response_descarga.mensaje + Environment.NewLine +
                               "Código error: " + response_descarga.codigo + Environment.NewLine;

                    throw new NotImplementedException(MsjError);
                }
            }
            catch(Exception)
            {
                throw new NotImplementedException("Mensaje: Error en el servicio");
            }
           
            
        }

        public string TimbraYEnviaASunat(string ruc, string usuario, string usuarioPassword, string texto)
        {
            throw new NotImplementedException();
        }

        public async Task<string> SolicitarBajaAsync(string ruc, string usuario, string usuarioPassword, string nroDocumento, string motivo)
        {
            var baja = await ServicioWS.ComunicacionBajaAsync(ruc, usuario, usuarioPassword, nroDocumento, motivo);

            if (baja.codigo == 0)
            {
                return baja.mensaje;
            }
            else
            {
                throw new Exception("Excepción al solicitar la baja al servicio web. " + baja.codigo.ToString() + " - " + baja.mensaje + " " + baja.numeracion);
            }

        }

        //public Task<string> ObtieneCDRdelOSEAsync(string ruc, string tipoDoc, string serie, string correlativo, string rutaYNomArchivoCfdi)
        //{
        //    throw new NotImplementedException();
        //}

        public async Task<string> ConsultaStatusAlOSEAsync(string ruc, string usuario, string usuarioPassword, string tipoDoc, string serie, string correlativo)
        {
            //string rutaYNomArchivoPDF = Path.Combine(ruta, nombreArchivo + extension);

            var response_descarga = await ServicioWS.EstatusDocumentoAsync(usuario, usuarioPassword, ruc + "-" + tipoDoc + "-" + serie + "-" + correlativo);
            return string.Concat(response_descarga.codigo.ToString(), "-", response_descarga.mensaje);
            //if (response_descarga.codigo == 0)
            //{
            //    return response_descarga.codigo.ToString();
            //}
            //else
            //{
            //    return string.Concat(response_descarga.codigo.ToString(), " - ", response_descarga.mensaje);
            //}

        }

        //public string ObtieneCDRdelOSE(string ruc, string tipoDoc, string serie, string correlativo)
        //{
        //    throw new NotImplementedException();
        //}
        //public Tuple<string, string> Baja(string ruc, string usuario, string usuarioPassword, string nroDocumento, string motivo)
        //{
        //        throw new NotImplementedException();
        //}
        //public Tuple<string, string> ResumenDiario(string ruc, string usuario, string usuarioPassword, string texto)
        //{
        //    throw new NotImplementedException();
        //}

    }
}
