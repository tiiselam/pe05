using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cfdiPeruInterfaces
{
    public interface ICfdiPeruDocumento
    {
        String FormatearDocElectronico(String tipoDocumento, OpenInvoicePeru.Comun.Dto.Modelos.DocumentoElectronico docElectronico);
        String FormatearResumenElectronico(String tipoDocumento, OpenInvoicePeru.Comun.Dto.Modelos.ResumenDiarioNuevo docResumen);
    }
}
