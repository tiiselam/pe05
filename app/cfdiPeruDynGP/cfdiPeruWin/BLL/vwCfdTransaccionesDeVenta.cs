using System;
using System.Collections.Generic;
using System.Text;
using OpenInvoicePeru.Comun.Dto.Modelos;
using cfdiEntidadesGP;
using Comun;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Linq;

namespace cfdiPeru
{
    class vwCfdTransaccionesDeVenta : vwCfdiTransaccionesDeVenta
    {
        ComunicacionBaja _documentoBaja;
        DocumentoVentaGP docGP;
        private const string FormatoFecha = "yyyy-MM-dd";
        private const string FormatoFechaHora = "yyyy-MM-dd";

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

        public DocumentoVentaGP DocGP { get => docGP; set => docGP = value; }

        public vwCfdTransaccionesDeVenta(string connstr, string nombreVista)
        {
            this.ConnectionString = connstr;
            this.QuerySource = nombreVista;
            this.MappingName = nombreVista;

            //this.QuerySource = "vwCfdiTransaccionesDeVenta";
            //this.MappingName = "vwCfdiTransaccionesDeVenta";
        }

        public static async Task<string> ObtieneLeyendasAsync()
        {
            return await DocumentoVentaGP.GetParametrosTipoLeyendaAsync();

        }

        public void ArmarDocElectronico(string leyendas)
        {
            try
            {
                docGP = new DocumentoVentaGP();
                docGP.GetDatosDocumentoVenta(this.Sopnumbe, this.Soptype);

                docGP.LeyendasXml = ObtieneLeyendaConjunta(leyendas, docGP.DocVenta.leyendaPorFactura, docGP.DocVenta.leyendaPorFactura2);

            }
            catch (Exception)
            {

                throw;
            }
        }

        private string ObtieneLeyendaConjunta(string leyendas, string leyendaPorFactura, string leyendaPorFactura2)
        {
            string leyendaConjunta = leyendas;
            if (!string.IsNullOrEmpty(leyendas))
            {
                XElement leyendasX = XElement.Parse(leyendas);
                XElement nuevaSeccion;
                if (!string.IsNullOrEmpty(leyendaPorFactura))
                {
                    nuevaSeccion = new XElement("SECCION", new XAttribute("S", 1), new XAttribute("T", "Adicional"), new XAttribute("V", leyendaPorFactura));
                    leyendasX.Add(nuevaSeccion);
                }
                if (!string.IsNullOrEmpty(leyendaPorFactura2))
                {
                    nuevaSeccion = new XElement("SECCION", new XAttribute("S", 2), new XAttribute("T", "Adicionales"), new XAttribute("V", leyendaPorFactura2));
                    leyendasX.Add(nuevaSeccion);
                }

                leyendaConjunta = leyendasX.ToString();
            }

            return leyendaConjunta;
        }

        public void ArmarResumenElectronico()
        {
            try
            {
                DocumentoVentaGP docGP = new DocumentoVentaGP();
                docGP.GetDatosResumenBoletas(this.Sopnumbe, this.Soptype);

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
