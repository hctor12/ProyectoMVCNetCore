using System.Security.Cryptography;
using System.Text;

namespace ProyectoMVCNetCore.Helpers
{
    public class HelperCryptography
    {
        public static byte[] EncryptPassword(string password, string salt)
        {
            string contenido = password + salt;
            SHA512 managed = SHA512.Create();
            byte[] salida = Encoding.UTF8.GetBytes(contenido);
            for (int i = 0; i < 30; i++)
            {
                salida = managed.ComputeHash(salida);
            }
            managed.Clear();
            return salida;
        }
    }
}