using System;
using System.Collections.Generic;
using System.Text;

namespace EjecutableEncriptador
{
    class vwCfdiDocumentosCobro:vwCfdiTrxCobros
    {
        public vwCfdiDocumentosCobro(string connstr)
        {
            this.ConnectionString = connstr;
            this.QuerySource = "vwCfdiTrxCobros";
            this.MappingName = "vwCfdiTrxCobros";
        }
    }
}
