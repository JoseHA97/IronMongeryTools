using System;
using System.Text.RegularExpressions;

public static class Validaciones
{
    // Validación para la cédula (2-0766-0660)
    public static bool EsCedula(string cedula)
    {
        // Expresión regular para validar el formato 2-0766-0660
        const string cedulaPattern = @"^\d-\d{4}-\d{4}$";
        return Regex.IsMatch(cedula, cedulaPattern);
    }

    public static bool EsAdministradorID(string admiinistradorid)
    {
        // Expresión regular para validar el formato 2-0766-0660
        const string administradoridPattern = @"^\d-\d{4}-\d{4}$";
        return Regex.IsMatch(admiinistradorid, administradoridPattern);
    }

    public static bool EsUsuarioID(string cedula)
    {
        // Expresión regular para validar el formato 2-0766-0660
        const string cedulaPattern = @"^\d-\d{4}-\d{4}$";
        return Regex.IsMatch(cedula, cedulaPattern);
    }

    public static bool EsProveedorID(string cedula)
    {
        // Expresión regular para validar el formato 2-0766-0660
        const string cedulaPattern = @"^\d-\d{4}-\d{4}$";
        return Regex.IsMatch(cedula, cedulaPattern);
    }

    // Validación para verificar si una cadena es un número
    public static bool EsNumero(string valor)
    {
        return int.TryParse(valor, out _);
    }

    // Validación para verificar si una cadena contiene solo texto
    public static bool EsTexto(string valor)
    {
        // Verifica que el valor solo contenga letras y espacios
        const string textoPattern = @"^[a-zA-Z\s]+$";
        return Regex.IsMatch(valor, textoPattern);
    }
}