using System;
using System.Collections.Generic;
using System.Text;
using OpenInvoicePeru.Comun.Dto.Modelos;
using cfdiEntidadesGP;
using Comun;

namespace cfdiPeru
{
    class vwCfdTransaccionesDeVenta : vwCfdiTransaccionesDeVenta
    {
        DocumentoElectronico _docElectronico;
        ComunicacionBaja _documentoBaja;
        ResumenDiarioNuevo _resumenElectronico;
        List<DetalleDocumento> lDetalleDocumento;
        private const string FormatoFecha = "yyyy-MM-dd";

        public DocumentoElectronico DocElectronico
        {
            get
            {
                return _docElectronico;
            }

            set
            {
                _docElectronico = value;
            }
        }

        public ResumenDiarioNuevo ResumenElectronico
        {
            get
            {
                return _resumenElectronico;
            }

            set
            {
                _resumenElectronico = value;
            }
        }

        public ComunicacionBaja DocumentoBaja
        {
            get
            {
                return _documentoBaja;
            }

            set
            {
                _documentoBaja = value;
            }
        }

        public vwCfdTransaccionesDeVenta(string connstr, string nombreVista)
        {
            this.ConnectionString = connstr;
            this.QuerySource = nombreVista;
            this.MappingName = nombreVista;

            //this.QuerySource = "vwCfdiTransaccionesDeVenta";
            //this.MappingName = "vwCfdiTransaccionesDeVenta";
        }

        public void ArmarDocElectronico()
        {
            try
            {
                DocumentoVentaGP docGP = new DocumentoVentaGP();

                docGP.GetDatosDocumentoVenta(this.Sopnumbe, this.Soptype);

                _docElectronico = new DocumentoElectronico();
                _docElectronico.TipoDocumento = docGP.DocVenta.tipoDocumento;
                _docElectronico.IdDocumento = docGP.DocVenta.idDocumento;
                _docElectronico.FechaEmision = this.Fechahora.ToString();
                _docElectronico.Moneda = docGP.DocVenta.moneda;
                _docElectronico.Emisor.NroDocumento = docGP.DocVenta.emisorNroDoc;
                _docElectronico.Emisor.NombreComercial = docGP.DocVenta.emisorNombre;
                _docElectronico.Emisor.NombreLegal = docGP.DocVenta.emisorNombre;
                _docElectronico.Emisor.Ubigeo = docGP.DocVenta.emisorUbigeo;
                _docElectronico.Emisor.Direccion = docGP.DocVenta.emisorDireccion;
                _docElectronico.Emisor.Urbanizacion = docGP.DocVenta.emisorUrbanizacion;
                _docElectronico.Emisor.Departamento = docGP.DocVenta.emisorDepartamento;
                _docElectronico.Emisor.Provincia = docGP.DocVenta.emisorProvincia;
                _docElectronico.Emisor.Distrito = docGP.DocVenta.emisorDistrito;
                _docElectronico.Receptor.TipoDocumento = docGP.DocVenta.receptorTipoDoc;
                _docElectronico.Receptor.NroDocumento = docGP.DocVenta.receptorNroDoc;
                _docElectronico.Receptor.NombreComercial = docGP.DocVenta.receptorNombre;
                _docElectronico.Receptor.NombreLegal = docGP.DocVenta.receptorNombre;
                _docElectronico.TipoOperacion = docGP.DocVenta.tipoOperacion;
                _docElectronico.DescuentoGlobal = docGP.DocVenta.ORTDISAM;
                _docElectronico.TotalIgv = docGP.DocVenta.iva;
                _docElectronico.Gravadas = docGP.DocVenta.ivaImponible;
                _docElectronico.Inafectas = docGP.DocVenta.inafecta;
                _docElectronico.Exoneradas = docGP.DocVenta.exonerado;
                _docElectronico.Gratuitas = docGP.DocVenta.gratuito;
                _docElectronico.TotalVenta = docGP.DocVenta.total;
                _docElectronico.MontoEnLetras = docGP.DocVenta.montoEnLetras;

                lDetalleDocumento = new List<DetalleDocumento>();
                int i = 1;
                foreach (vwCfdiConceptos d in docGP.LDocVentaConceptos)
                {
                    lDetalleDocumento.Add(new DetalleDocumento()
                    {
                        CodigoItem = d.ITEMNMBR,
                        Id = i, // Convert.ToInt16(d.id),
                        Descripcion = d.Descripcion,
                        Cantidad = d.cantidad,
                        UnidadMedida = d.udemSunat,
                        PrecioUnitario = d.valorUni,
                        PrecioReferencial = Convert.ToDecimal(d.precioUniConIva),
                        TotalVenta = Convert.ToDecimal(d.importe),
                        TipoPrecio = d.tipoPrecio,
                        Impuesto = d.orslstax,
                        TipoImpuesto = d.tipoImpuesto,
                        
                    });
                    i++;
                }
                _docElectronico.Items = new List<DetalleDocumento>();
                _docElectronico.Items = lDetalleDocumento;

                if (docGP.LDocVentaRelacionados.Count > 0)
                {
                    _docElectronico.Relacionados = new List<DocumentoRelacionado>();
                    _docElectronico.Discrepancias = new List<Discrepancia>();
                    foreach (vwCfdiRelacionados d in docGP.LDocVentaRelacionados)
                    {
                        _docElectronico.Relacionados.Add(new DocumentoRelacionado()
                        {
                            NroDocumento = d.sopnumbeTo,
                            TipoDocumento = d.tipoDocumento
                        });

                        _docElectronico.Discrepancias.Add(new Discrepancia()
                        {
                            Tipo = docGP.DocVenta.discrepanciaTipo,
                            Descripcion = docGP.DocVenta.discrepanciaDesc,
                            NroReferencia = d.sopnumbeTo
                        });
                    }

                }


            }
            catch (Exception)
            {

                throw;
            }
        }

        public void ArmarResumenElectronico()
        {
            try
            {
                DocumentoVentaGP docGP = new DocumentoVentaGP();
                docGP.GetDatosResumenBoletas(this.Sopnumbe, this.Soptype);

                _resumenElectronico = new ResumenDiarioNuevo()
                {
                    IdDocumento = docGP.ResumenCab.numResumenDiario,
                    FechaEmision = docGP.ResumenCab.docdate.ToString(FormatoFecha), //DateTime.Today.ToString(FormatoFecha),
                    FechaReferencia = docGP.ResumenCab.docdate.ToString(FormatoFecha),
                    Emisor = new Contribuyente()
                    {
                        NroDocumento = docGP.ResumenCab.emisorNroDoc,
                        TipoDocumento = docGP.ResumenCab.emisorTipoDoc,
                        Direccion = docGP.ResumenCab.emisorDireccion,
                        Urbanizacion = docGP.ResumenCab.emisorUrbanizacion,
                        Departamento = docGP.ResumenCab.emisorDepartamento,
                        Provincia = docGP.ResumenCab.emisorProvincia,
                        Distrito = docGP.ResumenCab.emisorDistrito,
                        NombreComercial = docGP.ResumenCab.emisorNombre,
                        NombreLegal = docGP.ResumenCab.emisorNombre,
                        Ubigeo = docGP.ResumenCab.emisorUbigeo
                    },
                    Resumenes=new List<GrupoResumenNuevo>()
                };

                int i = 1;
                foreach (vwCfdiGeneraResumenDiario re in docGP.LDocResumenLineas)
                {
                    var grn = new GrupoResumenNuevo()
                    {
                        Id = i,
                        TipoDocumento = re.tipoDocumento,
                        IdDocumento = re.sopnumbe,
                        NroDocumentoReceptor = re.receptorNroDoc,
                        TipoDocumentoReceptor = re.receptorTipoDoc,
                        CodigoEstadoItem = 1,
                        Moneda = re.moneda,
                        TotalVenta = Convert.ToDecimal(re.total),
                        TotalDescuentos = Convert.ToDecimal(re.totalDescuento),
                        Gratuitas = Convert.ToDecimal(re.totalGratuito),
                        Gravadas = Convert.ToDecimal(re.totalIvaImponible),
                        Exoneradas = Convert.ToDecimal(re.totalExonerado),
                        Inafectas = Convert.ToDecimal(re.totalInafecta),
                        TotalIgv = Convert.ToDecimal(re.totalIva),

                        DocumentoRelacionado = (re.tipoDocumento=="07" || re.tipoDocumento == "08") ? re.sopnumbeTo : null,
                        TipoDocumentoRelacionado = (re.tipoDocumento == "07" || re.tipoDocumento == "08") ? re.tipoDocumentoTo : null
                        //IdDocumento = re.serie,
                        //Serie = re.serie,
                        //CorrelativoInicio = Convert.ToInt32(re.iniRango),
                        //CorrelativoFin = Convert.ToInt32(re.finRango),
                    };

                    _resumenElectronico.Resumenes.Add(grn);

                    i++;
                }
            }
            catch (Exception)
            { throw;
            }
        }

        public void ArmarBaja(String motivoBaja)
        {
            DocumentoVentaGP docGP = new DocumentoVentaGP();

            docGP.GetDatosDocumentoVenta(this.Sopnumbe, this.Soptype);

            _documentoBaja = new ComunicacionBaja
            {
                
                IdDocumento = string.Format("RA-{0:yyyyMMdd}-"+ Utiles.Derecha( docGP.DocVenta.correlativo, 5), DateTime.Today),
                FechaEmision = DateTime.Today.ToString(FormatoFecha),
                FechaReferencia = DateTime.Today.ToString(FormatoFecha),
                Emisor = new Contribuyente()
                {
                    NroDocumento = docGP.DocVenta.emisorNroDoc,
                    TipoDocumento = docGP.DocVenta.emisorTipoDoc,
                    Direccion = docGP.DocVenta.emisorDireccion,
                    Urbanizacion = docGP.DocVenta.emisorUrbanizacion,
                    Departamento = docGP.DocVenta.emisorDepartamento,
                    Provincia = docGP.DocVenta.emisorProvincia,
                    Distrito = docGP.DocVenta.emisorDistrito,
                    NombreComercial = docGP.DocVenta.emisorNombre,
                    NombreLegal = docGP.DocVenta.emisorNombre,
                    Ubigeo = docGP.DocVenta.emisorUbigeo
                },

                Bajas = new List<DocumentoBaja>()
            };

            _documentoBaja.Bajas.Add(new DocumentoBaja
            {
                Id = 1,
                Correlativo = docGP.DocVenta.numero,
                TipoDocumento = docGP.DocVenta.tipoDocumento,
                Serie = docGP.DocVenta.serie,
                MotivoBaja = motivoBaja
            });

        }
    }
}
