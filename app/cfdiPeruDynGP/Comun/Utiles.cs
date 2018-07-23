using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Security.AccessControl;
using Ionic.Zip;

namespace Comun
{
    public class Utiles
    {
        static public int numErr = 0;
        static public string msgErr = "";
        /// <summary>
        /// Devuelve los últimos caracteres a la derecha del Texto
        /// </summary>
        /// <param name="Texto">Texto a procesar</param>
        /// <param name="Cuantos">Número de caracteres a retornar</param>
        /// <returns>String Devuelve los últimos caracteres a la derecha del Texto</returns>
        static public string Derecha(string Texto, short Cuantos)
        {
            if (Texto.Length > Cuantos && Cuantos > 0)
                return Texto.Remove(0, Texto.Length - Cuantos);
            else
                return Texto;
        }

        static public string Derecha(string Texto, int Cuantos)
        {
            if (Texto.Length > Cuantos && Cuantos > 0)
            {
                return Texto.Remove(0, Texto.Length - Cuantos);
            }
            else
                return Texto;
        }

        static public string Izquierda(string Texto, int Cuantos)
        {
            if (Texto.Length > Cuantos && Cuantos > 0)
                return Texto.Substring(0, Cuantos);
            else
                return Texto;
        }

        static public void SetRule(string filePath, string account, FileSystemRights rights, AccessControlType controlType)
        {
            FileSecurity fSecurity = File.GetAccessControl(filePath);
            fSecurity.ResetAccessRule(new FileSystemAccessRule(account, rights, controlType));
            File.SetAccessControl(filePath, fSecurity);
        }

        static public string FormatoNombreArchivo(string prefijo, string nombre, int largo)
        {
            string pre = prefijo.Trim().Replace(" ", "").Replace("'", "").Replace("&", "").Replace("<", "").Replace(">", "").Replace("/", "").Replace(@"\", "").Replace(",", "").Replace(".", "").Replace(";", "").Replace("@", "");
            string nom = nombre.Trim().PadRight(largo, '_').Substring(0, largo - 1).Replace(" ", "").Replace("'", "").Replace("&", "").Replace("<", "").Replace(">", "").Replace("/", "").Replace(@"\", "").Replace(",", "").Replace(".", "").Replace(";", "").Replace("@", "");
            return pre + "_" + nom;
        }

        static public void Zip(string fileName, string extension)
        {
            numErr = 0;
            msgErr = "";
            try
            {
                using (ZipFile zip = new ZipFile())
                {
                    zip.AddFile(fileName + extension, "");
                    zip.Save(fileName + ".zip");
                }
            }
            catch (Exception zp)
            {
                numErr++;
                msgErr = "Error [Zip] " + zp.Message;
            }
        }
    }
}
