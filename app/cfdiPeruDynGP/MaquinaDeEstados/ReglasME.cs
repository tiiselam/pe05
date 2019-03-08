using System;
using System.Collections.Generic;
using System.Text;
using Comun;

namespace MaquinaDeEstados
{
    public class ReglasME
    {
        public string ultimoMensaje = "";
        private string _eBinarioNuevo = "000000";
        public string eBinActualConError = "100000";
        private string tipoDocumento = string.Empty;
        private string accion = string.Empty;
        private bool nodoDestinoAceptado = true;
        private string nodoDestinoEBinario = string.Empty;
        private string nodoDestinoStatusBase = "emitido";

        private Parametros _Compania = null;
        private string nodoDestinoMensaje;

        public string eBinarioNuevo
        {
            get
            {
                return _eBinarioNuevo;
            }

            set
            {
                _eBinarioNuevo = value;
            }
        }
        
        public string DestinoMensaje { get => EnLetras(DestinoEBinario, tipoDocumento); set => nodoDestinoMensaje = value; }
        public string DestinoEBinario { get => !DestinoAceptado ? eBinActualConError : eBinarioNuevo; set => nodoDestinoEBinario = value; }
        public bool DestinoAceptado { get => nodoDestinoAceptado; set => nodoDestinoAceptado = value; }
        public string TipoDocumento { get => tipoDocumento; set => tipoDocumento = value; }
        public string Accion { get => accion; set => accion = value; }
        public string DestinoStatusBase { get => nodoDestinoStatusBase; set => nodoDestinoStatusBase = value; }

        public void ActualizarNodoDestinoStatusBase()
        {
            nodoDestinoStatusBase = !nodoDestinoAceptado ? "rechazo_sunat" : "acepta_sunat";
        }
        public ReglasME( Parametros Param)
        {
            _Compania = Param;
        }

        /// <summary>
        /// Valida el cambio de estado de un documento. 
        /// El parámetro eBinarioActual es una cadena de 6 bits de derecha a izquierda (little endian):
        /// En el caso de facturas:
        ///     1           emitido
        ///     2           anulado             (Sunat rechaza comprobante o acepta baja) 
        ///     3           impreso
        ///     4           publicado           (baja solicitada)
        ///     5           enviado             (Sunat acepta comprobante)
        ///     6           rechazo_sunat       (Sunat rechaza solicitud de baja)
        /// En el caso de resumen:
        ///     1           emitido
        ///     2           acepta_sunat (resumen aceptado)
        ///     3           x
        ///     4           publicado    (solicitud de acuse a la sunat)
        ///     5           x
        ///     6           rechazo_sunat   (resumen rechazado)
        /// </summary>
        /// <param name="tipoDoc">Tipo de documento</param>
        /// <param name="accion">Proceso que inicia el cambio de estado</param>
        /// <param name="eBinarioActual">Cadena de 6 bits de derecha a izquierda. Indican los estados del documento.</param>
        /// <param name="eBaseNuevo">Estado base al que se requiere cambiar</param>
        /// <param name="compania">Parámetros de la compañía</param>
        /// <returns>True si valida la transición. Cambia el atributo eBinarioNuevo: cadena de 6 bits que indica el nuevo estado del documento</returns>
        public bool ValidaTransicion(string tipoDoc, string accion, string eBinarioActual)
        {
            this.tipoDocumento = tipoDoc;
            this.accion = accion;
            ultimoMensaje = "";
            _eBinarioNuevo = eBinarioActual;
            eBinActualConError = "1" + Utiles.Derecha(eBinarioActual, 5);
            if (tipoDoc.Equals("FACTURA") && accion.Equals("EMITE XML Y PDF"))
            {
                    if (_Compania.emite && _Compania.imprime && eBinarioActual.Equals("000000"))
                    {
                        _eBinarioNuevo = "000101";                               //emitido, impreso
                        eBinActualConError = "000111";                           //emitido, rechazado/anulado, impreso
                        return true;
                    }
                    else
                        ultimoMensaje = "No está listo para emitir xml o imprimir pdf. [ValidaTransicion]" + eBinarioActual;
            }

            if (tipoDoc.Equals("FACTURA") && accion.Equals("ANULA VENTA"))
            {
                    if (_Compania.emite && eBinarioActual.Equals("000000"))
                    {
                        _eBinarioNuevo = "000001";                                //emitido
                        return true;
                    }
                    else
                        ultimoMensaje = "Probablemente ya fue anulado. [ValidaTransicion]" + eBinarioActual;
            }

            if (tipoDoc.Equals("FACTURA") && (accion.Equals("IMPRIME PDF")))            // primera impresión
            {
                    if (_Compania.emite == Utiles.Derecha(eBinarioActual, 1).Equals("1"))//emitido
                    {
                        if (_Compania.imprime)
                        {                                                               //Cambia el bit impreso
                            _eBinarioNuevo = Utiles.Izquierda(eBinarioActual, 3) + "1" + Utiles.Derecha(eBinarioActual, 2);
                            nodoDestinoStatusBase = "impreso";
                            return true;
                        }
                        else
                        {
                            ultimoMensaje = "La compañía no permite la impresión de facturas. [ValidaTransicion]";
                            return false;
                        }
                    }
                    else
                        ultimoMensaje = "Debe emitir el archivo xml antes de generar el pdf. [ValidaTransicion]" + eBinarioActual;
            }

            if (tipoDoc.Equals("FACTURA") && accion.Equals("BAJA"))
            {
                if (_Compania.emite == emitido(eBinarioActual) && !SunatRechazaComprobanteOAceptaBaja(eBinarioActual) && !SunatRechazaBaja(eBinarioActual) && !BajaSolicitada(eBinarioActual))
                {
                    _eBinarioNuevo = Utiles.Izquierda(eBinarioActual, 2) + "1" + Utiles.Derecha(eBinarioActual, 3); //baja solicitada
                    _eBinarioNuevo = "0" + Utiles.Derecha(_eBinarioNuevo, 5);
                    eBinActualConError = eBinarioActual;    // "0" + Utiles.Derecha(eBinarioActual, 5);
                    nodoDestinoStatusBase = "publicado";
                    return true;
                }
                else
                    ultimoMensaje = "Este comprobante no fue emitido, o ya tiene una solicitud de baja, o ya fue dado de baja. [ValidaTransicion] " + eBinarioActual;
            }

            if (tipoDoc.Equals("FACTURA") && accion.Equals("CONSULTA STATUS"))
            {
                if (_Compania.emite == emitido(eBinarioActual) && !SunatRechazaComprobanteOAceptaBaja(eBinarioActual) && !BajaSolicitada(eBinarioActual) && !SunatAceptaComprobante(eBinarioActual) && !SunatRechazaBaja(eBinarioActual) )
                {
                    _eBinarioNuevo = Utiles.Izquierda(eBinarioActual, 1) + "1" + Utiles.Derecha(_eBinarioNuevo, 4);     //doc aceptado por sunat
                    eBinActualConError = Utiles.Izquierda(eBinarioActual, 4) + "1" + Utiles.Derecha(_eBinarioNuevo, 1); //doc rechazado
                    nodoDestinoStatusBase = !nodoDestinoAceptado ? "rechazo_sunat" : "acepta_sunat";
                    return true;
                }
                else if (_Compania.emite == emitido(eBinarioActual) && !SunatRechazaComprobanteOAceptaBaja(eBinarioActual) && BajaSolicitada(eBinarioActual) && SunatAceptaComprobante(eBinarioActual) && !SunatRechazaBaja(eBinarioActual))
                {
                    _eBinarioNuevo = Utiles.Izquierda(eBinarioActual, 4) + "1" + Utiles.Derecha(_eBinarioNuevo, 1);         //baja aceptada
                    eBinActualConError = "1" + Utiles.Derecha(_eBinarioNuevo, 5);                                           //baja rechazada
                    return true;
                }
                else
                    ultimoMensaje = "Status inamovible"; 
            }

            if (tipoDoc.Equals("FACTURA") && accion.Equals("CONSULTA CDR"))
            {
                //if (_Compania.emite == emitido(eBinarioActual) && !Anulado(eBinarioActual) && !RechazaSunat(eBinarioActual) && publicado(eBinarioActual))
                //{
                //    _eBinarioNuevo = Utiles.Izquierda(eBinarioActual, 4) + "1" + Utiles.Derecha(_eBinarioNuevo, 1);     //rechazado o baja aceptada
                //    _eBinarioNuevo = "0" + Utiles.Derecha(_eBinarioNuevo, 5);
                //    eBinActualConError = Utiles.Izquierda(eBinarioActual, 4) + "0" + Utiles.Derecha(_eBinarioNuevo, 1); //baja rechazada
                //    eBinActualConError = "1" + Utiles.Derecha(eBinActualConError, 5);
                //    nodoDestinoStatusBase = !nodoDestinoAceptado ? "rechazo_sunat" : "anulado";
                //    return true;
                //}
                //else 
                if (_Compania.emite == emitido(eBinarioActual) && !SunatRechazaComprobanteOAceptaBaja(eBinarioActual) && !SunatRechazaBaja(eBinarioActual) && !BajaSolicitada(eBinarioActual))
                {
                    _eBinarioNuevo = Utiles.Izquierda(eBinarioActual, 4) + "0" + Utiles.Derecha(_eBinarioNuevo, 1);     //doc aceptado
                    _eBinarioNuevo = "0" + Utiles.Derecha(_eBinarioNuevo, 5);
                    eBinActualConError = Utiles.Izquierda(eBinarioActual, 4) + "1" + Utiles.Derecha(_eBinarioNuevo, 1); //doc rechazado
                    eBinActualConError = "0" + Utiles.Derecha(eBinActualConError, 5);
                    nodoDestinoStatusBase = !nodoDestinoAceptado ? "rechazo_sunat" : "acepta_sunat";
                    return true;
                }
                else
                    ultimoMensaje = "Si ya envió la baja de boleta o factura, verifique el CDR en el sitio web del OSE"; //No se puede consultar en la SUNAT porque no se hizo la solicitud, ya fue dado de baja o la baja fue rechazada. [ValidaTransicion] " + eBinarioActual;
            }
            //if (tipoDoc.Equals("FACTURA") && accion.Equals("ENVIA EMAIL"))
            //{
            //        if (_Compania.emite == Utiles.Derecha(eBinarioActual, 1).Equals("1")          //emitido
            //            && Utiles.Derecha(eBinarioActual, 2)[0].Equals('0')                      //no anulado
            //            && _Compania.imprime == Utiles.Derecha(eBinarioActual, 3)[0].Equals('1')  //impreso
            //                                                                                      //&& _Compania.publica == Utiles.Derecha(eBinarioActual, 4)[0].Equals('1')  
            //            && Utiles.Derecha(eBinarioActual, 5)[0].Equals('0'))                     //no enviado
            //        {
            //            _eBinarioNuevo = "01" + Utiles.Derecha(eBinarioActual, 4);
            //            return true;
            //            //return Convert.ToString(29, 2).PadLeft(6, '0');        //nuevo estado en binario Enviado=1
            //        }
            //        else
            //            if (Utiles.Derecha(eBinarioActual, 5)[0].Equals('1'))                     //enviado)
            //            ultimoMensaje = "Ya fue enviado anteriormente por e-mail. [ValidaTransicion] " + eBinarioActual;
            //        else
            //            ultimoMensaje = "No está listo para enviarse por e-mail. [ValidaTransicion] " + eBinarioActual;
            //}


            //if (tipoDoc.Equals("RESUMEN") && (accion.Equals("ENVIA RESUMEN")))            //envío del resumen a la SUNAT
            //{
            //        if (_Compania.emite && _Compania.publica && !RechazaSunat(eBinarioActual) && !publicado(eBinarioActual) && !AceptaSunat(eBinarioActual))
            //        {

            //            _eBinarioNuevo = Utiles.Izquierda(eBinarioActual, 2) + "1" + Utiles.Derecha(eBinarioActual, 3);
            //            _eBinarioNuevo = Utiles.Izquierda(_eBinarioNuevo, 5) + "1";         //emitido y enviado
            //            eBinActualConError = Utiles.Izquierda(eBinarioActual, 5) + "1";
            //            return true;
            //        }
            //        else
            //            ultimoMensaje = "El resumen fue enviado anteriormente. [ValidaTransicion] " + eBinarioActual;
            //}

            //if (tipoDoc.Equals("RESUMEN") && accion.Equals("CONSULTA CDR"))
            //{
            //    //if (_Compania.emite && _Compania.publica && publicado(eBinarioActual) && !RechazaSunat(eBinarioActual) && !AceptaSunat(eBinarioActual))
            //    //{
            //    //    _eBinarioNuevo = Utiles.Izquierda(eBinarioActual, 4) + "1" + Utiles.Derecha(eBinarioActual, 1);        //aceptado sunat
            //    //    eBinActualConError = "1" + Utiles.Derecha(eBinarioActual, 5);                                        //rechazado SUNAT
            //    //    eBinActualConError = Utiles.Izquierda(eBinActualConError, 4) + "0" + Utiles.Derecha(eBinActualConError, 1);     //no aceptado
            //    //    nodoDestinoStatusBase = !nodoDestinoAceptado ? "rechazo_sunat" : "acepta_sunat";
            //    //    return true;
            //    //}
            //    //else
            //    ultimoMensaje = "Verifique el CDR del resumen en el sitio web del OSE"; // "No se puede consultar el CDR porque no existe la solicitud, o el resumen ya fue aceptado/rechazado por la SUNAT. [ValidaTransicion] " + eBinarioActual;
            //}

            return false;
        }

        /// <summary>
        /// Valida el cambio de estado de un documento. 
        /// El parámetro eBinarioActual es una cadena de 6 bits de derecha a izquierda (little endian):
        /// En el caso de facturas:
        ///     1           emitido
        ///     2           anulado      (comprobante rechazado o baja aceptada) 
        ///     3           impreso
        ///     4           publicado    (baja solicitada)
        ///     5           enviado      (email al cliente)
        ///     6           baja_rechazo_sunat   (solicitud de baja rechazada)
        /// En el caso de resumen:
        ///     1           emitido
        ///     2           acepta_sunat (resumen aceptado)
        ///     3           x
        ///     4           publicado    (solicitud de acuse a la sunat)
        ///     5           x
        ///     6           rechazo_sunat   (resumen rechazado)
        /// </summary>
        /// <param name="tipoDoc">Tipo de documento</param>
        /// <param name="accion">Proceso que inicia el cambio de estado</param>
        /// <param name="eBinarioActual">Cadena de 6 bits de derecha a izquierda. Indican los estados del documento.</param>
        /// <param name="eBaseNuevo">Estado base al que se requiere cambiar</param>
        /// <param name="compania">Parámetros de la compañía</param>
        /// <returns>True si valida la transición. Cambia el atributo eBinarioNuevo: cadena de 6 bits que indica el nuevo estado del documento</returns>
        public bool ValidaTransicion(string tipoDoc, string accion, string eBinarioActual, string eBaseNuevo)
        {
            this.tipoDocumento = tipoDoc;
            this.accion = accion;
            //ultimoMensaje = "";
            _eBinarioNuevo = eBinarioActual;
            eBinActualConError = "1" + Utiles.Derecha(eBinarioActual, 5);
            //if (tipoDoc.Equals("FACTURA") && accion.Equals("ENVIA EMAIL"))
            //{
            //    if (eBaseNuevo.Equals("enviado"))
            //        if (_Compania.emite == Utiles.Derecha(eBinarioActual, 1).Equals("1")          //emitido
            //            && Utiles.Derecha(eBinarioActual, 2)[0].Equals('0')                      //no anulado
            //            && _Compania.imprime == Utiles.Derecha(eBinarioActual, 3)[0].Equals('1')  //impreso
            //            //&& _Compania.publica == Utiles.Derecha(eBinarioActual, 4)[0].Equals('1')  
            //            && Utiles.Derecha(eBinarioActual, 5)[0].Equals('0'))                     //no enviado
            //        {
            //            _eBinarioNuevo = "01"+Utiles.Derecha(eBinarioActual, 4);
            //            return true;
            //            //return Convert.ToString(29, 2).PadLeft(6, '0');        //nuevo estado en binario Enviado=1
            //        }
            //        else
            //            if (Utiles.Derecha(eBinarioActual, 5)[0].Equals('1'))                     //enviado)
            //                ultimoMensaje = "Ya fue enviado anteriormente por e-mail. [ValidaTransicion] " + eBinarioActual;
            //            else
            //                ultimoMensaje = "No está listo para enviarse por e-mail. [ValidaTransicion] "+eBinarioActual;
            //}

            if (tipoDoc.Equals("FACTURA") && accion.Equals("EMITE XML Y PDF"))
            {
                if (eBaseNuevo.Equals("emitido/impreso"))
                    if (_Compania.emite && _Compania.imprime && eBinarioActual.Equals("000000"))
                    {
                        _eBinarioNuevo = "000101";                               //emitido, impreso
                        eBinActualConError = "000111";                           //emitido, rechazado/anulado, impreso
                        return true;
                    }
                    else
                        ultimoMensaje = "No está listo para emitir xml o imprimir pdf. [ValidaTransicion]" + eBinarioActual;
            }

            if (tipoDoc.Equals("FACTURA") && accion.Equals("ANULA VENTA"))
            {
                if (eBaseNuevo.Equals("emitido"))
                    if (_Compania.emite && eBinarioActual.Equals("000000"))
                    {
                        _eBinarioNuevo = "000001";                                //emitido
                        return true;
                    }
                    else
                        ultimoMensaje = "Probablemente ya fue anulado. [ValidaTransicion]" + eBinarioActual;
            }

            if (tipoDoc.Equals("FACTURA") && (accion.Equals("IMPRIME PDF")))            // primera impresión
            {
                if (eBaseNuevo.Equals("impreso"))
                    if (_Compania.emite == Utiles.Derecha(eBinarioActual, 1).Equals("1"))//emitido
                    {
                        if (_Compania.imprime)
                        {                                                               //Cambia el bit impreso
                            _eBinarioNuevo = Utiles.Izquierda(eBinarioActual, 3)+ "1" + Utiles.Derecha(eBinarioActual, 2);
                            //_eBinarioNuevo = "0" + Utiles.Derecha(_eBinarioNuevo, 5);
                            return true;
                        }
                        else
                        {
                            ultimoMensaje = "La compañía no permite la impresión de facturas. [ValidaTransicion]";
                            return false;
                        }
                    }
                    else
                        ultimoMensaje = "Debe emitir el archivo xml antes de generar el pdf. [ValidaTransicion]" + eBinarioActual;
            }

            if (tipoDoc.Equals("FACTURA") && accion.Equals("DAR DE BAJA"))
            {
                if (eBaseNuevo.Equals("baja solicitada"))
                    if (_Compania.emite == emitido(eBinarioActual) && !SunatRechazaComprobanteOAceptaBaja(eBinarioActual) && !SunatRechazaBaja(eBinarioActual) && !BajaSolicitada(eBinarioActual))
                    {
                        _eBinarioNuevo = Utiles.Izquierda(eBinarioActual, 2) + "1" + Utiles.Derecha(eBinarioActual, 3); //baja solicitada
                        _eBinarioNuevo = "0" + Utiles.Derecha(_eBinarioNuevo, 5);
                        eBinActualConError = eBinarioActual;    // "0" + Utiles.Derecha(eBinarioActual, 5);
                        return true;
                    }
                    else
                        ultimoMensaje = "Este comprobante no fue emitido, o ya tiene una solicitud de baja, o ya fue dado de baja. [ValidaTransicion] " + eBinarioActual;
            }

            if (tipoDoc.Equals("FACTURA") && accion.Equals("CONSULTA CDR"))
            {
                if (eBaseNuevo.Equals("consulta a la sunat"))
                    if (_Compania.emite == emitido(eBinarioActual) && !SunatRechazaComprobanteOAceptaBaja(eBinarioActual) && BajaSolicitada(eBinarioActual) && !SunatRechazaBaja(eBinarioActual))
                    {
                        _eBinarioNuevo = Utiles.Izquierda(eBinarioActual, 4) + "1" + Utiles.Derecha(_eBinarioNuevo, 1);     //baja aceptada
                        _eBinarioNuevo = "0" + Utiles.Derecha(_eBinarioNuevo, 5);
                        eBinActualConError = Utiles.Izquierda(eBinarioActual, 4) + "0" + Utiles.Derecha(_eBinarioNuevo, 1); //baja rechazada
                        eBinActualConError = "1" + Utiles.Derecha(eBinActualConError, 5);
                        return true;
                    }
                    else
                        ultimoMensaje = "No se puede consultar en la SUNAT porque no se hizo la solicitud, ya fue dado de baja o la baja fue rechazada. [ValidaTransicion] " + eBinarioActual;
            }

            if (tipoDoc.Equals("FACTURA") && accion.Equals("CONSULTA STATUS"))
            {
                if (_Compania.emite == emitido(eBinarioActual) && !SunatRechazaComprobanteOAceptaBaja(eBinarioActual) && !BajaSolicitada(eBinarioActual) && !SunatAceptaComprobante(eBinarioActual) && !SunatRechazaBaja(eBinarioActual))
                {
                    _eBinarioNuevo = Utiles.Izquierda(eBinarioActual, 1) + "1" + Utiles.Derecha(_eBinarioNuevo, 4);     //doc aceptado por sunat
                    eBinActualConError = Utiles.Izquierda(eBinarioActual, 4) + "1" + Utiles.Derecha(_eBinarioNuevo, 1); //doc rechazado
                    nodoDestinoStatusBase = !nodoDestinoAceptado ? "rechazo_sunat" : "acepta_sunat";
                    return true;
                }
                else if (_Compania.emite == emitido(eBinarioActual) && !SunatRechazaComprobanteOAceptaBaja(eBinarioActual) && BajaSolicitada(eBinarioActual) && !SunatAceptaComprobante(eBinarioActual) && !SunatRechazaBaja(eBinarioActual))
                {
                    _eBinarioNuevo = "1" + Utiles.Derecha(_eBinarioNuevo, 5);                                           //baja rechazada
                    eBinActualConError = Utiles.Izquierda(eBinarioActual, 4) + "1" + Utiles.Derecha(_eBinarioNuevo, 1); //baja aceptada
                    nodoDestinoStatusBase = !nodoDestinoAceptado ? "rechazo_sunat" : "acepta_sunat";
                    return true;
                }
                else
                    ultimoMensaje = "Status inamovible";
            }
            //if (tipoDoc.Equals("RESUMEN") && (accion.Equals("ENVIA RESUMEN")))            //envío del resumen a la SUNAT
            //{
            //    if (eBaseNuevo.Equals("emitido/enviado a la sunat"))
            //        if (_Compania.emite && _Compania.publica && !RechazaSunat(eBinarioActual) && !publicado(eBinarioActual) && !AceptaSunat(eBinarioActual))
            //        {

            //            _eBinarioNuevo = Utiles.Izquierda(eBinarioActual, 2) + "1" + Utiles.Derecha(eBinarioActual, 3); 
            //            _eBinarioNuevo = Utiles.Izquierda(_eBinarioNuevo, 5) + "1";         //emitido y enviado
            //            eBinActualConError = Utiles.Izquierda(eBinarioActual, 5) + "1"; 
            //            return true;
            //        }
            //        else
            //            ultimoMensaje = "El resumen fue enviado anteriormente. [ValidaTransicion] " + eBinarioActual;
            //}

            //if (tipoDoc.Equals("RESUMEN") && accion.Equals("CONSULTA CDR"))
            //{
            //    if (eBaseNuevo.Equals("consulta a la sunat"))
            //        if (_Compania.emite && _Compania.publica && publicado(eBinarioActual) && !RechazaSunat(eBinarioActual) && !AceptaSunat(eBinarioActual))
            //        {
            //            _eBinarioNuevo = Utiles.Izquierda(eBinarioActual, 4)+"1"+ Utiles.Derecha(eBinarioActual, 1);        //aceptado sunat
            //            eBinActualConError = "1"+ Utiles.Derecha(eBinarioActual, 5);                                        //rechazado SUNAT
            //            eBinActualConError = Utiles.Izquierda(eBinActualConError, 4)+"0"+Utiles.Derecha(eBinActualConError, 1);     //no aceptado
            //            return true;
            //        }
            //        else
            //            ultimoMensaje = "No se puede consultar el CDR porque no existe la solicitud, o el resumen ya fue aceptado/rechazado por la SUNAT. [ValidaTransicion] " + eBinarioActual;
            //}

            return false;
        }

        /// <summary>
        /// Traducción en palabras del estado binario 
        /// </summary>
        /// <param name="eBinario">Cadena de 6 bits de derecha a izquierda que indica los estados del documento.</param>
        /// <param name="compania">Parámetros de la compañía</param>
        /// <returns>Traducción del estado binario.</returns>
        public string EnLetras(string eBinario, string tipoDoc)
        {
            string eBaseDescripcion = "";

            if (tipoDoc.Equals("FACTURA"))
            {
                if (_Compania.emite)
                    if (emitido(eBinario))
                        eBaseDescripcion += "Xml emitido. ";
                    else
                        eBaseDescripcion += "Xml no emitido. ";

                if (_Compania.imprime)
                    if (impreso(eBinario))
                        eBaseDescripcion += "Pdf impreso. ";
                    else
                        eBaseDescripcion += "Pdf no impreso. ";

                if (_Compania.envia)
                    if (SunatAceptaComprobante(eBinario))
                        eBaseDescripcion += "Aceptado Sunat. ";

                if (_Compania.publica)
                    if (BajaSolicitada(eBinario))
                        eBaseDescripcion += "Baja solicitada. ";

                if (SunatRechazaComprobanteOAceptaBaja(eBinario))
                    eBaseDescripcion += "Anulado Sunat. ";

                if (SunatRechazaBaja(eBinario))
                    eBaseDescripcion += "Baja rechazada Sunat. ";
            }
            else
            {
                if (_Compania.emite)
                    if (emitido(eBinario))
                        eBaseDescripcion += "Xml emitido. ";
                    else
                        eBaseDescripcion += "Xml no emitido. ";

                if (SunatRechazaComprobanteOAceptaBaja(eBinario))
                    eBaseDescripcion += "Aceptado Sunat. ";

                if (_Compania.imprime)
                    if (impreso(eBinario))
                        eBaseDescripcion += "";
                    else
                        eBaseDescripcion += "";

                if (_Compania.publica)
                    if (BajaSolicitada(eBinario))
                        eBaseDescripcion += "Enviado Sunat. ";
                    else
                        eBaseDescripcion += "No enviado Sunat. ";

                //if (_Compania.envia)
                //    if (enviado(eBinario))
                //        eBase += "E-mail enviado.";
                //    else
                //        eBase += "E-mail no enviado. ";

                if (SunatRechazaBaja(eBinario))
                    eBaseDescripcion += "Rechazado Sunat. ";
            }

            return eBaseDescripcion;
        }

        public bool emitido(string eBinario)
        {
            return Utiles.Derecha(eBinario, 1).Equals("1");
        }
        private bool SunatRechazaComprobanteOAceptaBaja(string eBinario)
        {
            return Utiles.Derecha(eBinario, 2)[0].Equals('1');
        }
        //private bool AceptaSunat(string eBinario)
        //{
        //    return Utiles.Derecha(eBinario, 2)[0].Equals('1');
        //}
        public bool impreso(string eBinario)
        {
            //Console.WriteLine(Utiles.Derecha(eBinario, 3)[0].Equals('1') + " - " + Utiles.Derecha(eBinario, 3)[0].Equals('1'));
            return Utiles.Derecha(eBinario, 3)[0].Equals('1');
        }
        public bool BajaSolicitada(string eBinario)
        {
            return Utiles.Derecha(eBinario, 4)[0].Equals('1');
        }
        public bool SunatAceptaComprobante(string eBinario)
        {
            return Utiles.Derecha(eBinario, 5)[0].Equals('1');
        }
        private bool SunatRechazaBaja(string eBinario)
        {
            return Utiles.Derecha(eBinario, 6)[0].Equals('1');
        }

    }
}
