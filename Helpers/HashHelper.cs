using System.Security.Cryptography;
using System.Text;

namespace ServiOS.Helpers;

public static class HashHelper
{
    public static string Hashear(string password)
    {
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(password);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToBase64String(hash);
    }

    public static bool Verificar(string password, string hashAlmacenado)
    {
        string hashInput = Hashear(password);
        return hashInput == hashAlmacenado;
    }
}