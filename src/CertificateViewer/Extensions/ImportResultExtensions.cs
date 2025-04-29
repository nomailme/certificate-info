using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using CertificateViewer.Controls.Dialogs;
using CertificateViewer.Logic;
using CertificateViewer.Logic.ImportServices;

namespace CertificateViewer.Extensions;

public static class ImportResultExtensions
{
    public static DialogResult ToDialogResult(this ImportResult value, CertificateType type) => new()
    {
        Certificates = value.Certificates ?? new List<X509Certificate2>(),
        Success = value.Success ? DialogResult.OperationResult.Success : DialogResult.OperationResult.Failure,
        Type = type,
        Error = value.Error
    };
}
