namespace Reporteador
{
    partial class winVisorDeReportes
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.crViewerCfd = new CrystalDecisions.Windows.Forms.CrystalReportViewer();
            this.ssrsRepView = new Microsoft.Reporting.WinForms.ReportViewer();
            this.SuspendLayout();
            // 
            // crViewerCfd
            // 
            this.crViewerCfd.ActiveViewIndex = -1;
            this.crViewerCfd.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.crViewerCfd.DisplayGroupTree = false;
            this.crViewerCfd.Dock = System.Windows.Forms.DockStyle.Fill;
            this.crViewerCfd.Location = new System.Drawing.Point(0, 0);
            this.crViewerCfd.Name = "crViewerCfd";
            this.crViewerCfd.SelectionFormula = "";
            this.crViewerCfd.Size = new System.Drawing.Size(966, 566);
            this.crViewerCfd.TabIndex = 0;
            this.crViewerCfd.ViewTimeSelectionFormula = "";
            // 
            // ssrsRepView
            // 
            this.ssrsRepView.Dock = System.Windows.Forms.DockStyle.Fill;
            this.ssrsRepView.Location = new System.Drawing.Point(0, 0);
            this.ssrsRepView.Name = "ssrsRepView";
            this.ssrsRepView.ProcessingMode = Microsoft.Reporting.WinForms.ProcessingMode.Remote;
            this.ssrsRepView.ServerReport.ReportServerUrl = new System.Uri("", System.UriKind.Relative);
            this.ssrsRepView.Size = new System.Drawing.Size(966, 566);
            this.ssrsRepView.TabIndex = 1;
            // 
            // winVisorDeReportes
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(966, 566);
            this.Controls.Add(this.ssrsRepView);
            this.Controls.Add(this.crViewerCfd);
            this.Name = "winVisorDeReportes";
            this.Text = "Visor de Reporte";
            this.Load += new System.EventHandler(this.winVisorDeReportes_Load);
            this.ResumeLayout(false);

        }

        #endregion

        private CrystalDecisions.Windows.Forms.CrystalReportViewer crViewerCfd;
        private Microsoft.Reporting.WinForms.ReportViewer ssrsRepView;
    }
}