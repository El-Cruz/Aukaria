using System.Text;

namespace Aukaria.Tests.Helpers;

public static class PdfTestHelper
{
    public static byte[] GenerarPdfMinimoCtl(string fmi = "50C-123456", string orip = "BOGOTA", string predio = "PREDIO SAN JOSE")
    {
        string contenidoTexto = $"BT\n/F1 12 Tf\n50 700 Td\n(CERTIFICADO DE TRADICION Y LIBERTAD) Tj\n0 -20 Td\n(MATRICULA INMOBILIARIA: {fmi}) Tj\n0 -20 Td\n(ORIP: {orip}) Tj\n0 -20 Td\n(NOMBRE DEL PREDIO: {predio}) Tj\nET";
        byte[] streamBytes = Encoding.ASCII.GetBytes(contenidoTexto);
        int streamLength = streamBytes.Length;

        var sb = new StringBuilder();
        sb.Append("%PDF-1.4\n");
        int offsetObj1 = sb.Length;
        sb.Append("1 0 obj\n<< /Type /Catalog /Pages 2 0 R >>\nendobj\n");
        int offsetObj2 = sb.Length;
        sb.Append("2 0 obj\n<< /Type /Pages /Kids [3 0 R] /Count 1 >>\nendobj\n");
        int offsetObj3 = sb.Length;
        sb.Append("3 0 obj\n<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Resources << /Font << /F1 << /Type /Font /Subtype /Type1 /BaseFont /Helvetica >> >> >> /Contents 4 0 R >>\nendobj\n");
        int offsetObj4 = sb.Length;
        sb.Append($"4 0 obj\n<< /Length {streamLength} >>\nstream\n");
        sb.Append(contenidoTexto);
        sb.Append("\nendstream\nendobj\n");
        int startXref = sb.Length;
        sb.Append("xref\n0 5\n0000000000 65535 f \n");
        sb.Append($"{offsetObj1:D10} 00000 n \n");
        sb.Append($"{offsetObj2:D10} 00000 n \n");
        sb.Append($"{offsetObj3:D10} 00000 n \n");
        sb.Append($"{offsetObj4:D10} 00000 n \n");
        sb.Append($"trailer\n<< /Size 5 /Root 1 0 R >>\nstartxref\n{startXref}\n%%EOF\n");

        return Encoding.ASCII.GetBytes(sb.ToString());
    }
}
