using System;
using System.IO;

namespace UnityEditor.PackageManager.DocumentationTools.UI
{
    public class Validator
    {
        public static void Validate(string buildLog)
        {
            #region run-validation
            
            if (GlobalSettings.Validate)
            {
                ValidationSuite.ValidationSuite.ValidatePackage(GlobalSettings.PackageInformation.name, GlobalSettings.PackageInformation.version, ValidationSuite.ValidationType.LocalDevelopmentInternal);
                var validationReport = ValidationSuite.ValidationSuite.GetValidationSuiteReport(GlobalSettings.PackageInformation.name, GlobalSettings.PackageInformation.version);
                SaveWarningReport(buildLog, validationReport);
            }
            else
            {
                SaveWarningReport(buildLog, "\n============\n\nValidate option not selected -- missing doc report not created.");
            }
            #endregion
        }

        private static void SaveWarningReport(string DocFXOutput, string ValidationOutput)
        {
            string report = "Generated for " + GlobalSettings.PackageInformation.packageId + "\n";
            report += "Generated on: " + DateTime.Now + "\n\n";
            report += "====================================\n\n";

            string[] lines = (DocFXOutput + "\n\n" + ValidationOutput).Split(
                new[] { Environment.NewLine },
                StringSplitOptions.None
            );
            foreach(var line in lines)
            {
                report += line + Environment.NewLine;
            }

            var reportPath = Path.Combine(DocumentationPackageManagerUI.ReportDir,
                GlobalSettings.PackageInformation.name +
                "@" + GlobalSettings.PackageInformation.version +
                ".txt");

            if (!Directory.Exists(DocumentationPackageManagerUI.ReportDir))
                Directory.CreateDirectory(DocumentationPackageManagerUI.ReportDir);

            File.WriteAllText(reportPath, report);
        }
    }
}