using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Drawing.Imaging;
using ThoughtWorks.QRCode.Codec;
using ThoughtWorks.QRCode.Codec.Data;
using ThoughtWorks.QRCode.Codec.Util;

namespace QRCodeLib
{
    public class CodigoDeBarras
    {
        public string strMensajeErr = "";
        public int iErr = 0;
        QRCodeEncoder qrCodeEncoder;
        public Image imagen;
        ImageFormat formatoImagen;

        public CodigoDeBarras()
        {
            try
            {
                qrCodeEncoder = new QRCodeEncoder();
                qrCodeEncoder.QRCodeEncodeMode = QRCodeEncoder.ENCODE_MODE.BYTE;        //mode
                qrCodeEncoder.QRCodeScale = 3;                                          //size
                qrCodeEncoder.QRCodeVersion = 7;                                        //version
                qrCodeEncoder.QRCodeErrorCorrect = QRCodeEncoder.ERROR_CORRECTION.M;    //error correction level
                formatoImagen = ImageFormat.Jpeg;
            }
            catch (Exception qr){
                strMensajeErr = "Error al inicializar. [CodigoDeBarras]" + qr.Message;
                iErr++;
            }
        }

        /// <summary>
        /// Genera el código de barras bidimensional y lo guarda en un archivo jpg.
        /// </summary>
        /// <param name="strData">Texto para codificar.</param>
        /// <param name="strRuta">Ruta donde se guarda la imagen.</param>
        /// <returns>void</returns>
        public void GenerarQRBidimensional(string strData, string strRuta)
        {
            strMensajeErr = "";
            iErr = 0;
            if (strData.Trim() == String.Empty)
            {
                strMensajeErr = "El texto vacío. No se puede generar el código bidimensional. [GenerarQRBidimensional]";
                iErr++;
                return;
            }
            
            try
            {
                imagen = qrCodeEncoder.Encode(strData.Trim());
                imagen.Save(strRuta, formatoImagen);
            }
            catch (DirectoryNotFoundException)
            {
                strMensajeErr = "La ruta no pudo ser encontrada: " + strRuta + "[GenerarQRBidimensional]";
                iErr++;
            }
            catch (IOException)
            {
                strMensajeErr = "Verifique permisos de escritura en: " + strRuta + ". No se pudo guardar el archivo. [GenerarQRBidimensional]";
                iErr++;
            }
            catch(Exception qr)
            {
                if (qr.Message.Contains("denied"))
                    strMensajeErr = "Elimine el archivo antes de volver a generar uno nuevo. Luego vuelva a intentar. [GenerarQRBidimensional] " + qr.Message;
                else
                    strMensajeErr = "Error al generar o guardar el código bidimensional. [GenerarQRBidimensional]" + qr.Message;
                iErr++;
            }
            //picEncode.Image = image;
        }
    }
}
