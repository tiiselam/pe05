using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace cfd.FacturaElectronica
{
    class vwCfdiDatosDelXml_wrapper:vwCfdiDatosDelXml
    {
        public vwCfdiDatosDelXml_wrapper(string connstr)
        {
            this.ConnectionString = connstr;
            this.QuerySource = "vwCfdiDatosDelXml";
            this.MappingName = "vwCfdiDatosDelXml";
        }

    }
}
