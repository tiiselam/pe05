using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace cfdiPeru
{
    class vwCfdiPagosSimultaneos_wrapper:vwCfdiPagosSimultaneos
    {
        public vwCfdiPagosSimultaneos_wrapper(string connstr)
        {
            this.ConnectionString = connstr;
            this.QuerySource = "vwCfdiPagosSimultaneos";
            this.MappingName = "vwCfdiPagosSimultaneos";
        }

    }
}
