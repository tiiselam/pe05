using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;
using System.IO;

using Comun;
using cfd.FacturaElectronica;
using MyGeneration.dOOdads;
using MaquinaDeEstados;
using System.Linq;
using cfdiPeruInterfaces;
using cfdiPeruOperadorServiciosElectronicos;

namespace cfdiPeru
{
    public partial class winformGeneraFE : Form
    {
        //static winVisorDeReportes FrmVisorDeReporte;
        cfdReglasFacturaXml regla;
        DataGridView dGridActivo;
        String vistaActiva = String.Empty;

        DateTime fechaIni = DateTime.Now;
        DateTime fechaFin = DateTime.Now;
        string ultimoMensaje = "";
        int estadoCompletadoCia = 0;
        int estadoCompletadoResumen = 0;
        short idxChkBox = 0;                    //columna check box del grid
        short idxIdDoc = 1;                     //columna id de documento del grid
        short idxSoptype = 2;                   //columna soptype del grid
        short idxSopnumbe = 3;                  //columna sopnumbe del grid
        short idxEstado = 8;                    //columna estado (en letras) del grid
        short idxMensaje = 9;                   //columna mensaje del grid
        short idxEstadoContab = 10;             //columna estado contabilizado del grid
        short idxAnulado = 11;                  //columna anulado del grid
        short idxEstadoDoc = 13;                //columna estado del documento (en números) del grid
        List<CfdiDocumentoId> LDocsNoSeleccionados = new List<CfdiDocumentoId>();   //Docs no marcados del grid
        private ConexionDB DatosConexionDB = new ConexionDB();  //Lee la configuración del archivo xml y obtiene los datos de conexión.

        ICfdiMetodosWebService ServiciosOse; 

        public winformGeneraFE()
        {
            InitializeComponent();
        }

        void reportaProgreso(int i, string s)
        {
            progressBar1.Increment(i);
            progressBar1.Refresh();

            if (progressBar1.Value == progressBar1.Maximum)
                progressBar1.Value = 0;

            txtbxMensajes.AppendText(s + Environment.NewLine);
            txtbxMensajes.Refresh();
        }

        private void ObtieneGrid(string nombreTab)
        {
            switch (nombreTab)
            {
                case "tabFacturas":
                    dGridActivo = dgridTrxFacturas;
                    vistaActiva = "vwCfdiTransaccionesDeVenta";
                    break;
                case "tabResumen":
                    dGridActivo = dgridTrxResumen;
                    vistaActiva = "vwCfdiListaResumenDiario";
                    break;
                default:
                    dGridActivo = dgridTrxFacturas;
                    vistaActiva = "vwCfdiTransaccionesDeVenta";
                    break;
            }
        }

        /// <summary>
        /// Aplica los criterios de filtro, actualiza la pantalla e inicializa los checkboxes del grid
        /// </summary>
        /// <param name=""></param>
        /// <returns>bool</returns>
        private bool AplicaFiltroYActualizaPantalla(string nombreTab)
        {
            txtbxMensajes.AppendText("Explorando...\r\n");
            txtbxMensajes.Refresh();

            ObtieneGrid(nombreTab);
            Parametros Compannia = new Parametros(DatosConexionDB.Elemento.Intercompany);
            txtbxMensajes.AppendText (Compannia.ultimoMensaje);
            if (!Compannia.ultimoMensaje.Equals(string.Empty))
                return false;

            regla = new cfdReglasFacturaXml(DatosConexionDB.Elemento, Compannia);
            regla.AplicaFiltroADocumentos(checkBoxFecha.Checked, dtPickerDesde.Value, dtPickerHasta.Value, fechaIni, fechaFin,
                         checkBoxNDoc.Checked, txtBNumDocDesde.Text, txtBNumDocHasta.Text,
                         checkBoxIdDoc.Checked, cmbBIdDoc.Text, 
                         checkBoxEstado.Checked, cmbBEstado.Text,
                         checkBoxCliente.Checked, textBCliente.Text,
                         vistaActiva);

            if (regla.numMensajeError == 0)
            {
                vwCfdTransaccionesDeVentaBindingSource.DataSource = regla.CfdiTransacciones.DefaultView;
                txtbxMensajes.AppendText("Completado: " + regla.CfdiTransacciones.RowCount.ToString() + " documento(s) consultado(s).\r\n");
            }
            else
            {
                vwCfdTransaccionesDeVentaBindingSource.DataSource = null;
                txtbxMensajes.AppendText(regla.ultimoMensaje);
            }
            txtbxMensajes.Refresh();
            dGridActivo.Refresh();

            //Restituir las filas marcadas usando la lista de docs no seleccionados
            InicializaCheckBoxDelGrid(dGridActivo, idxChkBox, LDocsNoSeleccionados);

            return regla.numMensajeError == 0;
        }

        void InicializaCheckBoxDelGrid(DataGridView dataGrid, short idxChkBox, bool marca)
        {
            for (int r = 0; r < dataGrid.RowCount; r++)
            {
                dataGrid[idxChkBox, r].Value = marca; 
            }
            dataGrid.EndEdit();
        }

        void InicializaCheckBoxDelGrid(DataGridView dataGrid, short idxChkBox, List<CfdiDocumentoId> LNoSeleccionados)
        {
            for (int r = 0; r < dataGrid.RowCount; r++)
            {
                dataGrid[idxChkBox, r].Value = !LNoSeleccionados.Exists(delegate(CfdiDocumentoId match)
                                            {
                                                return (match.idDoc == dataGrid[idxIdDoc, r].Value.ToString()
                                                    && match.sopnumbe == dataGrid[idxSopnumbe, r].Value.ToString());
                                            });
            }
            dataGrid.EndEdit();
            dataGrid.Refresh();
        }

        private bool cargaIdDocumento()
        {
            vwCfdIdDocumentos iddoc = new vwCfdIdDocumentos(DatosConexionDB.Elemento.ConnStr);
            try
            {
                if (iddoc.LoadAll())
                {
                    cmbBIdDoc.DisplayMember = vwCfdIdDocumentos.ColumnNames.Docid;
                    cmbBIdDoc.ValueMember = vwCfdIdDocumentos.ColumnNames.Docid;
                    cmbBIdDoc.DataSource = iddoc.DefaultView;
                    return true;
                }
                else
                    ultimoMensaje = "Los Id. de documentos de venta no están configurados. Revise la configuración de Procesamiento de Ventas de GP.";
            }
            catch (Exception eIddoc)
            {
                ultimoMensaje = "Contacte al administrador. No se puede acceder a la base de datos." + eIddoc.Message;
            }
            return false;
        }

        private bool cargaCompannias(bool Filtro, string Unica)
        {
            vwCfdCompannias Compannias = new vwCfdCompannias(DatosConexionDB.Elemento.ConnStrDyn);
            //if (Filtro)
            //{
            //    Compannias.Where.INTERID.Value = Unica;
            //    Compannias.Where.INTERID.Operator = WhereParameter.Operand.Equal;
            //}
            try
            {
                if (Compannias.Query.Load())
                {
                    //Ocasiona que se dispare el trigger textChanged del combo box
                    cmbBxCompannia.DisplayMember = vwCfdCompannias.ColumnNames.CMPNYNAM;
                    cmbBxCompannia.ValueMember = vwCfdCompannias.ColumnNames.INTERID;
                    cmbBxCompannia.DataSource = Compannias.DefaultView;
                    return true;
                }
                else
                    ultimoMensaje = "No tiene acceso a ninguna compañía. Revise los privilegios otorgados a su usuario. [cargaCompannias]";
            }
            catch (Exception eCia)
            {
                ultimoMensaje = "Contacte al administrador. No se puede acceder a la base de datos. [CargaCompannias] " + DatosConexionDB.ultimoMensaje + " - " + eCia.Message;
            }
            return false;
        }

        /// <summary>
        /// Filtra las facturas marcadas en el grid y memoriza las filas no marcadas.
        /// </summary>
        /// <param name=""></param>
        /// <returns>bool: True indica que la lista ha sido filtrada exitosamente</returns>
        public bool filtraListaSeleccionada()
        {
            int i = 1;
            object[] llaveDocumento = new object[2];
            LDocsNoSeleccionados = new List<CfdiDocumentoId>();
            try
            {
                dGridActivo.EndEdit();
                progressBar1.Value = 0;
                //cargar lista de no seleccionados
                foreach (DataGridViewRow dgvr in dGridActivo.Rows)
                {
                    if (!(dgvr.Cells[idxChkBox].Value != null && (dgvr.Cells[idxChkBox].Value.Equals(true) || dgvr.Cells[idxChkBox].Value.ToString().Equals("1"))))  
                        LDocsNoSeleccionados.Add(new CfdiDocumentoId(dgvr.Cells[idxIdDoc].Value.ToString(), 
                                                                Convert.ToInt16(dgvr.Cells[idxSoptype].Value.ToString()),
                                                                dgvr.Cells[idxSopnumbe].Value.ToString()));
                    progressBar1.Value = Convert.ToInt32( i * 100 / dGridActivo.RowCount);
                    i++;
                }
                progressBar1.Value = 0;
                bool vacio = dGridActivo.RowCount == LDocsNoSeleccionados.Count;
                if (vacio)
                    ultimoMensaje = "[filtraListaSeleccionada] No ha marcado ningún documento. Marque al menos una casilla en la primera columna para continuar con el proceso.\r\n";
                else
                {
                    //eliminar del datasource los registros no seleccionados
                    regla.CfdiTransacciones.DefaultView.Sort = vwCfdTransaccionesDeVenta.ColumnNames.Docid + ", " + vwCfdTransaccionesDeVenta.ColumnNames.Sopnumbe;
                    foreach (CfdiDocumentoId registro in LDocsNoSeleccionados)
                    {
                        llaveDocumento[0] = registro.idDoc;     //idDoc
                        llaveDocumento[1] = registro.sopnumbe;  //sopnumbe
                        regla.CfdiTransacciones.DefaultView.Delete(regla.CfdiTransacciones.DefaultView.Find(llaveDocumento));
                    }
                }
                return (!vacio);
            }
            catch (Exception eFiltro)
            {
                ultimoMensaje = "[filtraListaSeleccionada] No se pudo filtrar los documentos seleccionados. " + eFiltro.Message;
                return (false);
            }
        }

        private void winformGeneraFE_Load(object sender, EventArgs e)
        {
            if (!cargaCompannias(!DatosConexionDB.Elemento.IntegratedSecurity, DatosConexionDB.Elemento.Intercompany))
            {
                txtbxMensajes.Text = ultimoMensaje;
                HabilitarVentana(false,false,false,false,false, true);
            }
            dtPickerDesde.Value = DateTime.Now;
            dtPickerHasta.Value = DateTime.Now;
            lblFecha.Text = DateTime.Now.ToString();
        }

        private void HabilitarVentana(bool emite, bool anula, bool imprime, bool publica, bool envia, bool cambiaCia)
        {
            cmbBxCompannia.Enabled = cambiaCia;
            tsButtonGenerar.Enabled = emite;      //Emite xml
            tsBtnAbrirXML.Enabled = false;                          //Emite xml
            tsBtnArchivoMensual.Enabled = emite;  //Emite xml
            tsBtnAnulaElimina.Enabled = anula;    //Elimina xml
            toolStripPDF.Enabled = imprime;       //Imprime
            toolStripImpresion.Enabled = imprime; //Imprime
            toolStripEmail.Enabled = envia;       //Envía emails
            toolStripEmailMas.Enabled = envia;

            toolStripConsulta.Enabled = emite || anula || imprime || publica || envia;
            btnBuscar.Enabled = emite || anula || imprime || publica || envia;
        }

        private void ReActualizaDatosDeVentana()
        {
            DatosConexionDB.Elemento.Compannia = cmbBxCompannia.Text.ToString().Trim();
            DatosConexionDB.Elemento.Intercompany = cmbBxCompannia.SelectedValue.ToString().Trim();
            lblUsuario.Text = DatosConexionDB.Elemento.Usuario;
            ToolTip tTipCompannia = new ToolTip();
            tTipCompannia.AutoPopDelay = 5000;
            tTipCompannia.InitialDelay = 1000;
            tTipCompannia.UseFading = true;
            tTipCompannia.Show("Está conectado a: " + DatosConexionDB.Elemento.Compannia, this.cmbBxCompannia, 5000);

            txtbxMensajes.Text = "";
            if (!cargaIdDocumento())
            {
                txtbxMensajes.AppendText(ultimoMensaje);
                HabilitarVentana(false,false,false,false,false, true);
            }

            Parametros configCfd = new Parametros(DatosConexionDB.Elemento.Intercompany);   //Carga configuración desde xml
            estadoCompletadoCia = configCfd.intEstadoCompletado;
            estadoCompletadoResumen = configCfd.intEstadoCompletadoResumen;

            if (!configCfd.ultimoMensaje.Equals(string.Empty))
            {
                txtbxMensajes.AppendText(configCfd.ultimoMensaje);
                HabilitarVentana(false,false,false,false,false, true);
                return;
            }

            tsComboDestinoRep.Text = "Pantalla";
            if (configCfd.ImprimeEnImpresora)
                tsComboDestinoRep.Text = "Impresora";

            HabilitarVentana(configCfd.emite, configCfd.anula, configCfd.imprime, configCfd.publica, configCfd.envia, true);
            AplicaFiltroYActualizaPantalla(this.tabCfdi.SelectedTab.Name);
        }

        private void btnBuscar_Click(object sender, EventArgs e)
        {
            txtbxMensajes.Text = "";
            LDocsNoSeleccionados = new List<CfdiDocumentoId>();
            AplicaFiltroYActualizaPantalla(this.tabCfdi.SelectedTab.Name);
        }

        private bool ExistenTransaccionesAMedioContabilizar(vwCfdTransaccionesDeVenta tdv)
        {
            tdv.Rewind();
            List<string> t = new List<string>();
            if (tdv.RowCount > 0)
                do
                {
                    t.Add(tdv.s_Soptype + "-" + tdv.s_Sopnumbe);
                } while (tdv.MoveNext());

            var ragrupado = t.GroupBy(f => f)
                            .Where(repetido => repetido.Count() > 1)
                            .ToList();
            if (ragrupado.Count() > 0)
            {
                txtbxMensajes.AppendText("Las siguientes facturas todavía no terminaron de contabilizar:");
                ragrupado.ForEach(i => txtbxMensajes.AppendText(i.First()));
                txtbxMensajes.AppendText(Environment.NewLine + "Espere a que finalice la contabilización y vuelva a intentar.");
            }
            return (ragrupado.Count() > 0);
        }

        /// <summary>
        /// Genera XMLs masivamente
        /// </summary>
        /// <param name="e"></param>
        private async void toolStripButton2_Click(object sender, EventArgs e)
        {
            int errores = 0;
            txtbxMensajes.Text = "";

            Parametros Param = new Parametros(DatosConexionDB.Elemento.Intercompany);
            Param.ExtDefault = this.tabCfdi.SelectedTab.Name;
            ServiciosOse = new WebServicesOSE(Param.URLwebServPAC);

            //ServiciosOse.TimbraYEnviaASunat()
                       

            if (!Param.ultimoMensaje.Equals(string.Empty)) 
            {
                txtbxMensajes.Text = Param.ultimoMensaje;
                errores++;
            }
            if (regla.CfdiTransacciones.RowCount == 0)
            {
                txtbxMensajes.Text = "No hay documentos para generar. Verifique los criterios de búsqueda.";
                errores++;
            }
            if (!filtraListaSeleccionada()) //Filtra cfdiTransacciones sólo con docs marcados
            {
                txtbxMensajes.Text = ultimoMensaje;
                errores++;
            }
            if (errores == 0 && !ExistenTransaccionesAMedioContabilizar(regla.CfdiTransacciones))
            {
                HabilitarVentana(false, false, false, false, false, false);
                ProcesaCfdi proc = new ProcesaCfdi(DatosConexionDB.Elemento, Param);
                proc.TrxVenta = regla.CfdiTransacciones;
                proc.Progreso += new ProcesaCfdi.LogHandler(reportaProgreso);
                pBarProcesoActivo.Visible = true;

                //    await proc.GeneraResumenXmlAsync(ServiciosOse, EstructuraDocsOse);
                //else
                if (this.tabCfdi.SelectedTab.Name.Equals("tabFacturas"))
                    await proc.GeneraDocumentoXmlAsync(ServiciosOse);

            }
            //Actualiza la pantalla
            HabilitarVentana(Param.emite, Param.anula, Param.imprime, Param.publica, Param.envia, true);
            AplicaFiltroYActualizaPantalla(this.tabCfdi.SelectedTab.Name);
            progressBar1.Value = 0;
            pBarProcesoActivo.Visible = false;
        }

        void bw_Progress(object sender, ProgressChangedEventArgs e)
        {
            try
            {
                progressBar1.Value = e.ProgressPercentage;
                txtbxMensajes.AppendText(e.UserState.ToString());
            }
            catch (Exception ePr)
            {
                txtbxMensajes.AppendText("bw Progress: " + ePr.Message);
            }
        }

        void bw_Completed(object sender, RunWorkerCompletedEventArgs e)
        {
            try
            {
            if (e.Cancelled)
                progressBar1.Value = 0;
            else if (e.Error != null)
                txtbxMensajes.AppendText("[cfdFacturaXmlWorker]: " + e.Error.ToString());
            else
                txtbxMensajes.AppendText(e.Result.ToString());

            //Actualiza la pantalla
            Parametros Cia = new Parametros(DatosConexionDB.Elemento.Intercompany);   //Carga configuración desde xml
            HabilitarVentana(Cia.emite, Cia.anula, Cia.imprime, Cia.publica, Cia.envia, true);
            AplicaFiltroYActualizaPantalla(this.tabCfdi.SelectedTab.Name);
            progressBar1.Value = 0;
            pBarProcesoActivo.Visible = false;

            }
            catch (Exception eCm)
            {
                txtbxMensajes.AppendText("bw Completed: " + eCm.Message);
            }

        }

        private void salirToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void hoyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            fechaIni = DateTime.Now;
            fechaFin = DateTime.Now;
            checkBoxFecha.Checked = false;
            tsDDButtonFiltroF.Text = hoyToolStripMenuItem.Text;
        }

        private void ayerToolStripMenuItem_Click(object sender, EventArgs e)
        {
            fechaIni = DateTime.Now.AddDays(-1);
            fechaFin = DateTime.Now.AddDays(-1);
            checkBoxFecha.Checked = false;
            tsDDButtonFiltroF.Text = ayerToolStripMenuItem.Text;
        }

        private void ultimos7DíasToolStripMenuItem_Click(object sender, EventArgs e)
        {
            fechaIni = DateTime.Now.AddDays(-6);
            fechaFin = DateTime.Now;
            checkBoxFecha.Checked = false;
            tsDDButtonFiltroF.Text = ultimos7DíasToolStripMenuItem.Text;
        }

        private void dToolStripMenuItem_Click(object sender, EventArgs e)
        {
            fechaIni = DateTime.Now.AddDays(-29);
            fechaFin = DateTime.Now;
            checkBoxFecha.Checked = false;
            tsDDButtonFiltroF.Text = ultimos30diasToolStripMenuItem.Text;
        }

        private void ultimos60DíasToolStripMenuItem_Click(object sender, EventArgs e)
        {
            fechaIni = DateTime.Now.AddDays(-59);
            fechaFin = DateTime.Now;
            checkBoxFecha.Checked = false;
            tsDDButtonFiltroF.Text = ultimos60DíasToolStripMenuItem.Text;
        }

        private void mesActualToolStripMenuItem_Click(object sender, EventArgs e)
        {
            fechaIni = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            fechaFin = fechaIni.AddMonths(1);
            int ultimoDia = fechaFin.Day;
            fechaFin = fechaFin.AddDays(-ultimoDia);
            checkBoxFecha.Checked = false;
            tsDDButtonFiltroF.Text = mesActualToolStripMenuItem.Text;
        }

        private void tsDDButtonFiltroF_TextChanged(object sender, EventArgs e)
        {
            txtbxMensajes.Text = "";
            AplicaFiltroYActualizaPantalla(this.tabCfdi.SelectedTab.Name);
        }

        public bool ExistenFacturasNoEmitidas()
        {
            //int i = 0; 
            //progressBar1.Value = 0;
            //foreach (DataGridViewRow dgvr in dgridTrxFacturas.Rows)
            //{
            //    if (!dgvr.Cells[idxEstado].Value.Equals("emitido"))
            //    {
            //        dgvr.DefaultCellStyle.ForeColor = Color.Red;
            //        dgridTrxFacturas.CurrentCell = dgvr.Cells[idxEstado];
            //        progressBar1.Value = 0;
            //        return true;
            //    }
            //    progressBar1.Value = i * 100 / dgridTrxFacturas.RowCount;
            //    i++;
            //}
            //progressBar1.Value = 0;
            return false;
        }

        public void GuardaArchivoMensual()
        {

        }

        private void tsConfirmaAnulaXml_MouseLeave(object sender, EventArgs e)
        {
            tsConfirmaAnulaXml.Visible = false;

        }

        private void tsButtonConfirmaAnulaXml_Click(object sender, EventArgs e)
        {
        }

        private void GenerarReportePdf()
        {
            //string prmFolioDesde = "";
            //string prmFolioHasta = "";
            //string prmTabla = "SOP30200";
            //int prmSopType = 0;
            //Parametros configCfd = new Parametros(DatosConexionDB.Elemento.Intercompany);   //Carga configuración desde xml
            //configCfd.ExtDefault = this.tabCfdi.SelectedTab.Name;

            //txtbxMensajes.Text = "";
            //txtbxMensajes.Refresh();
            //configCfd.ImprimeEnImpresora = false;
            //if (tsComboDestinoRep.Text.Equals("Impresora"))
            //    configCfd.ImprimeEnImpresora = true;

            //if (dgridTrxFacturas.CurrentRow != null)
            //{
            //    if (dgridTrxFacturas.CurrentCell.Selected)
            //    {
            //        prmFolioDesde = dgridTrxFacturas.CurrentRow.Cells[idxSopnumbe].Value.ToString();
            //        prmFolioHasta = dgridTrxFacturas.CurrentRow.Cells[idxSopnumbe].Value.ToString();
            //        prmSopType = Convert.ToInt16(dgridTrxFacturas.CurrentRow.Cells[idxSoptype].Value.ToString());

            //        //En el caso de una compañía que debe emitir xml, controlar que la factura ha sido emitida antes de imprimir.
            //        if(configCfd.emite)
            //        {
            //            if (!dgridTrxFacturas.CurrentRow.Cells[idxEstado].Value.Equals("emitido"))      //estado FE
            //            {
            //                txtbxMensajes.Text = "La factura " + prmFolioDesde + " no fue emitida. Emita la factura y vuelva a intentar.\r\n";
            //                return;
            //            }
            //        }
            //        else
            //        {
            //            if (dgridTrxFacturas.CurrentRow.Cells[idxAnulado].Value.ToString().Equals("1")) //factura anulada en GP
            //            {
            //                txtbxMensajes.Text = "La factura " + prmFolioDesde + " no se puede imprimir porque está anulada. \r\n";
            //                return;
            //            }
            //            if (dgridTrxFacturas.CurrentRow.Cells[idxEstadoContab].Value.Equals("en lote")) //estado contabilizado en GP
            //                prmTabla = "SOP10100";
            //        }

            //        if (FrmVisorDeReporte == null)
            //        {
            //            try
            //            {
            //                FrmVisorDeReporte = new winVisorDeReportes(DatosConexionDB.Elemento, configCfd, prmFolioDesde, prmFolioHasta, prmTabla, prmSopType);
            //            }
            //            catch (Exception ex)
            //            {
            //                MessageBox.Show(ex.Message);
            //            }
            //        }
            //        else
            //        {
            //            if (FrmVisorDeReporte.Created == false)
            //            {
            //                FrmVisorDeReporte = new winVisorDeReportes(DatosConexionDB.Elemento, configCfd, prmFolioDesde, prmFolioHasta, prmTabla, prmSopType);
            //            }
            //        }

            //        // Always show and activate the WinForm
            //        FrmVisorDeReporte.Show();
            //        FrmVisorDeReporte.Activate();
            //        txtbxMensajes.Text = FrmVisorDeReporte.mensajeErr;
            //    }
            //    else
            //        txtbxMensajes.Text = "No seleccionó ninguna factura. Debe marcar la factura que desea imprimir y luego presionar el botón de impresión.";
            //}
        }

        /// <summary>
        /// Imprimir en pantalla
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void tsButtonImprimir_Click(object sender, EventArgs e)
        {
            string prmFolioDesde = "";

            txtbxMensajes.Text = "";
            txtbxMensajes.Refresh();

            if (dgridTrxFacturas.CurrentRow != null)
            {
                if (dgridTrxFacturas.CurrentCell.Selected)
                {
                    string nombreYRutaPdf = dgridTrxFacturas.CurrentRow.Cells[idxMensaje].Value.ToString();
                    prmFolioDesde = dgridTrxFacturas.CurrentRow.Cells[idxSopnumbe].Value.ToString();

                        if (!dgridTrxFacturas.CurrentRow.Cells[idxEstado].Value.Equals("emitido"))      //estado FE
                        {
                            txtbxMensajes.Text = "La factura " + prmFolioDesde + " no fue emitida. Emita la factura y vuelva a intentar.\r\n";
                            return;
                        }
                        if (dgridTrxFacturas.CurrentRow.Cells[idxAnulado].Value.ToString().Equals("1")) //factura anulada en GP
                        {
                            txtbxMensajes.Text = "La factura " + prmFolioDesde + " no se puede imprimir porque está anulada. \r\n";
                            return;
                        }
                        string archivo = nombreYRutaPdf.ToLower().Replace(".xml", ".pdf");
                        if (File.Exists(archivo))
                            System.Diagnostics.Process.Start(archivo);
                        else
                            txtbxMensajes.Text = $"No existe el archivo {archivo}";

                }
                else
                    txtbxMensajes.Text = "No seleccionó ninguna factura. Debe marcar la factura que desea imprimir y luego presionar el botón de impresión.";
            }
        }

        private void cmbBxCompannia_TextChanged(object sender, EventArgs e)
        {
            ReActualizaDatosDeVentana();
        }

        private void tsBtnAnulaElimina_Click(object sender, EventArgs e)
        {
            if (this.tabCfdi.SelectedTab.Name.Equals("tabFacturas"))
                toolStripAuxRechazar.Visible = true;
            else
                txtbxMensajes.Text = "Presione el tab FACTURAS y vuelva a intentar." + Environment.NewLine;

        }

        private void tsBtnAbrirXML_Click(object sender, EventArgs e)
        {
            try
            {
                txtbxMensajes.Text = "";
                
                string ruta = dGridActivo.CurrentRow.Cells[idxMensaje].Value.ToString().Replace("Almacenado en ", "").Trim();
                string archivo = ".XML";

                if (dGridActivo.CurrentRow.Cells[idxEstado].Value.ToString().Equals("emitido"))
                    Help.ShowHelp(this, ruta + archivo);
                else
                    txtbxMensajes.Text = "No se ha emitido el archivo XML para este documento.";
            }
            catch (Exception eAbrexml)
            {
                txtbxMensajes.Text = "Error al abrir el archivo XML. Es probable que el archivo no exista o haya sido trasladado a otra carpeta. " + eAbrexml.Message;
            }

        }

        private void tsBtnArchivoMensual_Click(object sender, EventArgs e)
        {

        }

        /// <summary>
        /// Generar PDF
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void tsBtnGeneraPDF_Click(object sender, EventArgs e)
        {
            int errores = 0;
            txtbxMensajes.Text = "";

            Parametros Param = new Parametros(DatosConexionDB.Elemento.Intercompany);
            Param.ExtDefault = this.tabCfdi.SelectedTab.Name;
            ServiciosOse = new WebServicesOSE(Param.URLwebServPAC);

            if (!Param.ultimoMensaje.Equals(string.Empty))
            {
                txtbxMensajes.Text = Param.ultimoMensaje;
                errores++;
            }
            if (regla.CfdiTransacciones.RowCount == 0)
            {
                txtbxMensajes.Text = "No hay documentos para generar. Verifique los criterios de búsqueda.";
                errores++;
            }
            if (!filtraListaSeleccionada()) //Filtra cfdiTransacciones sólo con docs marcados
            {
                txtbxMensajes.Text = ultimoMensaje;
                errores++;
            }
            if (errores == 0)
            {
                pBarProcesoActivo.Visible = true;
                HabilitarVentana(false, false, false, false, false, false);
                ProcesaCfdi proc = new ProcesaCfdi(DatosConexionDB.Elemento, Param);
                proc.TrxVenta = regla.CfdiTransacciones;
                proc.Progreso += new ProcesaCfdi.LogHandler(reportaProgreso);
                pBarProcesoActivo.Visible = true;

                if (!this.tabCfdi.SelectedTab.Name.Equals("tabResumen"))
                    await proc.ProcesaObtienePDFAsync(ServiciosOse);

                //cfdFacturaPdfWorker _bw = new cfdFacturaPdfWorker(DatosConexionDB.Elemento, Param);
                //_bw.RunWorkerCompleted += new RunWorkerCompletedEventHandler(bw_Completed);
                //_bw.ProgressChanged += new ProgressChangedEventHandler(bw_Progress);
                //object[] arguments = { regla.CfdiTransacciones };
                //_bw.RunWorkerAsync(arguments);
                //Actualiza la pantalla
                HabilitarVentana(Param.emite, Param.anula, Param.imprime, Param.publica, Param.envia, true);
                AplicaFiltroYActualizaPantalla(this.tabCfdi.SelectedTab.Name);
                progressBar1.Value = 0;
                pBarProcesoActivo.Visible = false;
            }
        }

        private void cmbBxCompannia_SelectedIndexChanged(object sender, EventArgs e)
        {
            ReActualizaDatosDeVentana();
        }

        private void tsBtnEnviaEmail_Click(object sender, EventArgs e)
        {
            int errores = 0;
            txtbxMensajes.Text = "";

            Parametros Param = new Parametros(DatosConexionDB.Elemento.Intercompany);
            if (!Param.ultimoMensaje.Equals(string.Empty))
            {
                txtbxMensajes.Text = Param.ultimoMensaje;
                errores++;
            }
            if (regla.CfdiTransacciones.RowCount == 0)
            {
                txtbxMensajes.Text = "No hay documentos para generar. Verifique los criterios de búsqueda.";
                errores++;
            }
            if (!filtraListaSeleccionada()) //Filtra cfdiTransacciones sólo con docs marcados
            {
                txtbxMensajes.Text = ultimoMensaje;
                errores++;
            }
            if (errores == 0)
            {
                cfdEmailWorker _bw = new cfdEmailWorker(DatosConexionDB.Elemento, Param);
                pBarProcesoActivo.Visible = true;
                HabilitarVentana(false, false, false, false, false, false);
                _bw.RunWorkerCompleted += new RunWorkerCompletedEventHandler(bw_Completed);
                _bw.ProgressChanged += new ProgressChangedEventHandler(bw_Progress);
                object[] arguments = { regla.CfdiTransacciones };
                _bw.RunWorkerAsync(arguments);
            }
        }

        private void button1_Click_1(object sender, EventArgs e)
        {//pruebas
            if (filtraListaSeleccionada())
            {
                txtbxMensajes.AppendText("Documentos seleccionados listos para procesar...proceso...\r\n");
                AplicaFiltroYActualizaPantalla(this.tabCfdi.SelectedTab.Name);
            }
            else
                txtbxMensajes.Text = ultimoMensaje;
        }

        private void checkBoxMark_CheckedChanged(object sender, EventArgs e)
        {
            InicializaCheckBoxDelGrid(dGridActivo, idxChkBox, checkBoxMark.Checked);
        }

        private async void tsButtonConsultaTimbre_Click(object sender, EventArgs e)
        {
            int errores = 0;
            txtbxMensajes.Text = "";

            Parametros Param = new Parametros(DatosConexionDB.Elemento.Intercompany);
            Param.ExtDefault = this.tabCfdi.SelectedTab.Name;
            ServiciosOse = new WebServicesOSE(Param.URLwebServPAC);

            if (!Param.ultimoMensaje.Equals(string.Empty))
            {
                txtbxMensajes.Text = Param.ultimoMensaje;
                errores++;
            }
            if (regla.CfdiTransacciones.RowCount == 0)
            {
                txtbxMensajes.Text = "No hay documentos para procesar. Verifique los criterios de búsqueda.";
                errores++;
            }
            if (!filtraListaSeleccionada()) //Filtra cfdiTransacciones sólo con docs marcados
            {
                txtbxMensajes.Text = ultimoMensaje;
                errores++;
            }
            if (errores == 0)
            {
                HabilitarVentana(false, false, false, false, false, false);
                ProcesaCfdi proc = new ProcesaCfdi(DatosConexionDB.Elemento, Param);
                proc.TrxVenta = regla.CfdiTransacciones;

                proc.Progreso += new ProcesaCfdi.LogHandler(reportaProgreso);

                pBarProcesoActivo.Visible = true;

                if (this.tabCfdi.SelectedTab.Name.Equals("tabFacturas"))
                    await proc.ProcesaConsultaStatusAsync(ServiciosOse);
                else
                    txtbxMensajes.Text = "Presione el tab FACTURAS y luego el botón Consulta." + Environment.NewLine;

                //Actualiza la pantalla
                Parametros Cia = new Parametros(DatosConexionDB.Elemento.Intercompany);   //Carga configuración desde xml
                HabilitarVentana(Cia.emite, Cia.anula, Cia.imprime, Cia.publica, Cia.envia, true);
                AplicaFiltroYActualizaPantalla(this.tabCfdi.SelectedTab.Name);
                progressBar1.Value = 0;
                pBarProcesoActivo.Visible = false;
            }

        }

        private void tsMenuImprimir_Click(object sender, EventArgs e)
        {
            //string prmFolioDesde = "";
            //string prmFolioHasta = "";
            //string prmTabla = "SOP30200";
            //int prmSopType = 0;
            //Parametros configCfd = new Parametros(DatosConexionDB.Elemento.Intercompany);   //Carga configuración desde xml
            //configCfd.ExtDefault = this.tabCfdi.SelectedTab.Name;

            //txtbxMensajes.Text = "";
            //txtbxMensajes.Refresh();
            //configCfd.ImprimeEnImpresora = false;
            //if (tsComboDestinoRep.Text.Equals("Impresora"))
            //    configCfd.ImprimeEnImpresora = true;

            //if (dgridTrxFacturas.CurrentRow != null && dgridTrxFacturas.CurrentCell.Selected)
            //{
            //        prmFolioDesde = tsTextDesde.Text;
            //        prmFolioHasta = tsTextHasta.Text;
            //        prmSopType = Convert.ToInt16(dgridTrxFacturas.CurrentRow.Cells[idxSoptype].Value.ToString());

            //        //En el caso de una compañía que debe emitir xml, controlar que la factura ha sido emitida antes de imprimir.
            //        if (configCfd.emite)
            //        {
            //            if (!dgridTrxFacturas.CurrentRow.Cells[idxEstado].Value.Equals("emitido"))      //estado FE
            //            {
            //                txtbxMensajes.Text = "La factura " + prmFolioDesde + " no fue emitida. Emita la factura y vuelva a intentar.\r\n";
            //                return;
            //            }
            //        }
            //        else
            //        {
            //            if (dgridTrxFacturas.CurrentRow.Cells[idxAnulado].Value.ToString().Equals("1")) //factura anulada en GP
            //            {
            //                txtbxMensajes.Text = "La factura " + prmFolioDesde + " no se puede imprimir porque está anulada. \r\n";
            //                return;
            //            }
            //            if (dgridTrxFacturas.CurrentRow.Cells[idxEstadoContab].Value.Equals("en lote")) //estado contabilizado en GP
            //                prmTabla = "SOP10100";
            //        }

            //        if (FrmVisorDeReporte == null)
            //        {
            //            try
            //            {
            //                FrmVisorDeReporte = new winVisorDeReportes(DatosConexionDB.Elemento, configCfd, prmFolioDesde, prmFolioHasta, prmTabla, prmSopType);
            //            }
            //            catch (Exception ex)
            //            {
            //                MessageBox.Show(ex.Message);
            //            }
            //        }
            //        else
            //        {
            //            if (FrmVisorDeReporte.Created == false)
            //            {
            //                FrmVisorDeReporte = new winVisorDeReportes(DatosConexionDB.Elemento, configCfd, prmFolioDesde, prmFolioHasta, prmTabla, prmSopType);
            //            }
            //        }

            //        // Always show and activate the WinForm
            //        FrmVisorDeReporte.Show();
            //        FrmVisorDeReporte.Activate();
            //        txtbxMensajes.Text = FrmVisorDeReporte.mensajeErr;
            //}
            //else
            //    txtbxMensajes.Text = "No seleccionó ninguna factura. Debe marcar la factura que desea imprimir y luego presionar el botón de impresión.";
        }

        private void tsddButtonImprimir_Click(object sender, EventArgs e)
        {
            Parametros configCfd = new Parametros(DatosConexionDB.Elemento.Intercompany);   //Carga configuración desde xml

            var pdialago = new System.Windows.Forms.PrintDialog();
            configCfd.NombreImpresora = pdialago.PrinterSettings.PrinterName;
            tsComboDestinoRep.ToolTipText = configCfd.NombreImpresora;

            txtbxMensajes.Text = "";
            txtbxMensajes.Refresh();

            tsComboDestinoRep.Enabled = false;
            if (!configCfd.emite && configCfd.reporteador.Equals("CRYSTAL"))    //Se habilitó esta opción porque el visor de crystal no puede imprimir. 
                tsComboDestinoRep.Enabled = true;
            
            if (dGridActivo.CurrentRow != null)
            {
                if (dGridActivo.CurrentCell.Selected)
                {
                    tsTextDesde.Text = dGridActivo.CurrentRow.Cells[idxSopnumbe].Value.ToString();
                    tsTextHasta.Text = dGridActivo.CurrentRow.Cells[idxSopnumbe].Value.ToString();

                }
                else
                    txtbxMensajes.Text = "No seleccionó ninguna factura. Debe marcar la factura que desea imprimir y luego presionar el botón de impresión.";
            }

        }

        private void tabCfdi_SelectedIndexChanged(object sender, EventArgs e)
        {
            AplicaFiltroYActualizaPantalla(this.tabCfdi.SelectedTab.Name);
        }

        private void cBoxMarcCobros_CheckedChanged(object sender, EventArgs e)
        {
            InicializaCheckBoxDelGrid(dGridActivo, idxChkBox, cBoxMarcCobros.Checked);

        }

        private async void tsBtnMotivoRechazo_Click(object sender, EventArgs e)
        {
            int errores = 0;
            txtbxMensajes.Text = "";
            txtbxMensajes.Refresh();

            Parametros _param = new Parametros(DatosConexionDB.Elemento.Intercompany);
            ServiciosOse = new WebServicesOSE(_param.URLwebServPAC);
            if (!_param.ultimoMensaje.Equals(string.Empty))
            {
                txtbxMensajes.Text = _param.ultimoMensaje;
                errores++;
            }
            if (regla.CfdiTransacciones.RowCount == 0)
            {
                txtbxMensajes.Text = "No hay documentos para procesar. Verifique los criterios de búsqueda.";
                errores++;
            }
            if (!filtraListaSeleccionada()) //Filtra trxVenta sólo con docs marcados
            {
                txtbxMensajes.Text = ultimoMensaje;
                errores++;
            }
            if (tsTextBoxMotivoRechazo.Text.Equals(String.Empty))
            {
                txtbxMensajes.Text = "Ingrese el motivo de la baja.";
                errores++;
            }
            if (tsTextBoxMotivoRechazo.Text.Length > 100)
            {
                txtbxMensajes.Text = "El texto del motivo de rechazo es demasiado largo.";
                errores++;
            }

            if (errores == 0)
            {
                HabilitarVentana(false, false, false, false, false, false);
                ProcesaCfdi proc = new ProcesaCfdi(DatosConexionDB.Elemento, _param);
                proc.TrxVenta = regla.CfdiTransacciones;

                proc.Progreso += new ProcesaCfdi.LogHandler(reportaProgreso);

                pBarProcesoActivo.Visible = true;

                if (this.tabCfdi.SelectedTab.Name.Equals("tabFacturas"))
                    await proc.ProcesaBajaComprobanteAsync(tsTextBoxMotivoRechazo.Text, ServiciosOse);
                    //await proc.ProcesaBajaComprobante(tsTextBoxMotivoRechazo.Text);
                else
                    txtbxMensajes.Text = "Presione el tab FACTURAS y vuelva a intentar." + Environment.NewLine;

                //Actualiza la pantalla
                //Parametros Cia = new Parametros(DatosConexionDB.Elemento.Intercompany);   //Carga configuración desde xml
                HabilitarVentana(_param.emite, _param.anula, _param.imprime, _param.publica, _param.envia, true);
                AplicaFiltroYActualizaPantalla(this.tabCfdi.SelectedTab.Name);
                progressBar1.Value = 0;
                pBarProcesoActivo.Visible = false;
                toolStripAuxRechazar.Visible = false;
            }
        }

        /// <summary>
        /// Barre el grid para indicar los colores que corresponden a cada fila.
        /// </summary>
        private void dgridTrxResumen_RowPostPaint(object sender, DataGridViewRowPostPaintEventArgs e)
        {
            if (e.RowIndex >= 0)
            {
                //Está completo
                int estadoDoc = Convert.ToInt32(dGridActivo.Rows[e.RowIndex].Cells[idxEstadoDoc].Value.ToString(), 2);
                if (estadoDoc == estadoCompletadoResumen)
                {
                    dGridActivo.Rows[e.RowIndex].Cells[idxIdDoc].Style.BackColor = Color.YellowGreen;
                }

                //Está en proceso
                if (estadoDoc > 0 && estadoDoc != estadoCompletadoResumen)
                {
                    dGridActivo.Rows[e.RowIndex].Cells[idxIdDoc].Style.BackColor = Color.Orange;
                }
            }

        }
        /// <summary>
        /// Barre el grid para indicar los colores que corresponden a cada fila.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void dgridTrxFacturas_RowPostPaint_1(object sender, DataGridViewRowPostPaintEventArgs e)
        {
            if (e.RowIndex >= 0)
            {
                //Está completo
                int estadoDoc = Convert.ToInt32(dGridActivo.Rows[e.RowIndex].Cells[idxEstadoDoc].Value.ToString(), 2);
                if (estadoDoc == estadoCompletadoCia)
                {
                    dGridActivo.Rows[e.RowIndex].Cells[idxIdDoc].Style.BackColor = Color.YellowGreen;
                }

                //Está en proceso
                if (estadoDoc > 0 && estadoDoc != estadoCompletadoCia)
                {
                    dGridActivo.Rows[e.RowIndex].Cells[idxIdDoc].Style.BackColor = Color.Orange;
                }
            }
        }

        //private async void toolStripButton2_Click_1(object sender, EventArgs e)
        //{
        //    int errores = 0;
        //    txtbxMensajes.Text = "";

        //    Parametros Param = new Parametros(DatosConexionDB.Elemento.Intercompany);
        //    Param.ExtDefault = this.tabCfdi.SelectedTab.Name;

        //    if (!Param.ultimoMensaje.Equals(string.Empty))
        //    {
        //        txtbxMensajes.Text = Param.ultimoMensaje;
        //        errores++;
        //    }
        //    if (regla.CfdiTransacciones.RowCount == 0)
        //    {
        //        txtbxMensajes.Text = "No hay documentos para generar. Verifique los criterios de búsqueda.";
        //        errores++;
        //    }
        //    if (!filtraListaSeleccionada()) //Filtra cfdiTransacciones sólo con docs marcados
        //    {
        //        txtbxMensajes.Text = ultimoMensaje;
        //        errores++;
        //    }
        //    if (errores == 0 && !ExistenTransaccionesAMedioContabilizar(regla.CfdiTransacciones))
        //    {
        //        HabilitarVentana(false, false, false, false, false, false);
        //        ProcesaCfdi proc = new ProcesaCfdi(DatosConexionDB.Elemento, Param);
        //        proc.TrxVenta = regla.CfdiTransacciones;
        //        proc.Progreso += new ProcesaCfdi.LogHandler(reportaProgreso);
        //        pBarProcesoActivo.Visible = true;
        //        ICfdiMetodosWebService servicioTimbre = null;
        //        await proc.GeneraResumenXmlAsync(servicioTimbre);

        //    }
        //    //Actualiza la pantalla
        //    HabilitarVentana(Param.emite, Param.anula, Param.imprime, Param.publica, Param.envia, true);
        //    AplicaFiltroYActualizaPantalla(this.tabCfdi.SelectedTab.Name);
        //    progressBar1.Value = 0;
        //    pBarProcesoActivo.Visible = false;
        //}
    }
}
