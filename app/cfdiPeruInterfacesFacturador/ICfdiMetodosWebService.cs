using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cfdiPeruInterfaces
{
    public interface ICfdiMetodosWebService
    {
        string TimbraYEnviaASunat(string ruc, string usuario, string usuarioPassword, string texto);
    }
}
