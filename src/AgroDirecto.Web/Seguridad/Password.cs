using System.Security.Cryptography;

namespace AgroDirecto.Web.Seguridad;

// Cifrado de contraseñas con PBKDF2 (incluido en .NET, sin paquetes extra).
// El hash guardado tiene el formato:  iteraciones.saltBase64.hashBase64
// La sal se genera al azar en cada registro, así dos usuarios con la
// misma contraseña producen hashes distintos.
public static class Password
{
    private const int Iteraciones = 100_000;
    private const int BytesSal = 16;
    private const int BytesHash = 32;

    public static string Cifrar(string password)
    {
        byte[] sal = RandomNumberGenerator.GetBytes(BytesSal);
        byte[] hash = Derivar(password, sal);

        return $"{Iteraciones}.{Convert.ToBase64String(sal)}.{Convert.ToBase64String(hash)}";
    }

    public static bool Verificar(string password, string hashGuardado)
    {
        var partes = hashGuardado.Split('.');
        if (partes.Length != 3) return false;

        if (!int.TryParse(partes[0], out int iteraciones)) return false;

        byte[] sal, hashEsperado;
        try
        {
            sal = Convert.FromBase64String(partes[1]);
            hashEsperado = Convert.FromBase64String(partes[2]);
        }
        catch (FormatException)
        {
            return false;
        }

        byte[] hashCalculado = Rfc2898DeriveBytes.Pbkdf2(
            password, sal, iteraciones, HashAlgorithmName.SHA256, hashEsperado.Length);

        // Comparación en tiempo constante: no revela información según
        // cuántos bytes coincidieron antes de fallar.
        return CryptographicOperations.FixedTimeEquals(hashCalculado, hashEsperado);
    }

    private static byte[] Derivar(string password, byte[] sal) =>
        Rfc2898DeriveBytes.Pbkdf2(password, sal, Iteraciones, HashAlgorithmName.SHA256, BytesHash);
}
