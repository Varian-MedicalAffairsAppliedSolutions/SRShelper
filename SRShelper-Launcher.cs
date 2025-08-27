// SRShelper-Launcher.cs - ESAPI Script for launching SRShelper application
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using VMS.TPS.Common.Model.API;
using VMS.TPS.Common.Model.Types;
using System.Globalization; // Required for culture-invariant formatting
using System.Diagnostics;   // Required for Process.Start
using System.Runtime.CompilerServices; // Required for CallerFilePath
// Note: Using Uri.EscapeDataString instead of HttpUtility for ESAPI compatibility

namespace VMS.TPS
{
    public class Script
    {
        public void Execute(ScriptContext context)
        {
            try
            {
                // ====================================================================================
                // STEP 1: CONTEXT VALIDATION CHECKS
                // ====================================================================================
                if (context.Patient == null)
                {
                    MessageBox.Show("Error: No patient is loaded. Please open a patient before running this script.",
                                    "Patient Context Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                if (context.Course == null)
                {
                    MessageBox.Show("Error: No course is loaded. Please open a course before running this script.",
                                    "Course Context Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                if (context.PlanSetup == null)
                {
                    MessageBox.Show("Error: No plan is loaded. Please open a plan before running this script.",
                                    "Plan Context Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // ====================================================================================
                // STEP 2: PREPARE AND VALIDATE HTML FILE PATH
                // ====================================================================================
                // Get the launcher path using the source file method
                string launcherPath = Path.GetDirectoryName(GetSourceFilePath());
                string htmlFileName = "SRS_Metrics_v1.0.0RC1.html";
                
                // Uncomment and modify the line below to specify a custom path to the HTML file
                //string htmlFilePath = @"C:\CustomPath\SRShelper\SRS_Metrics_v1.0.0RC1.html";
                
                // Look for HTML file in the SRShelper subdirectory relative to launcher
                string htmlFilePath = Path.Combine(launcherPath, "SRShelper", htmlFileName);
                
                // If not found in subdirectory, try same directory as launcher (backward compatibility)
                if (!File.Exists(htmlFilePath))
                {
                    htmlFilePath = Path.Combine(launcherPath, htmlFileName);
                }
                
                // Final validation
                if (!File.Exists(htmlFilePath))
                {
                    MessageBox.Show(string.Format("Error: The HTML file '{0}' was not found. Searched in:\n- {1}\n- {2}", 
                                    htmlFileName, 
                                    Path.Combine(launcherPath, "SRShelper"),
                                    launcherPath),
                                    "HTML File Not Found", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // ====================================================================================
                // STEP 3: VALIDATE SRS PLAN REQUIREMENTS
                // ====================================================================================
                // Check if this is a suitable plan for SRS analysis
                var treatmentBeams = context.PlanSetup.Beams.Where(b => !b.IsSetupField).ToList();
                if (!treatmentBeams.Any())
                {
                    MessageBox.Show("Error: No treatment beams found in the plan. SRShelper requires at least one treatment beam for analysis.",
                                    "Plan Validation Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Check for dose calculation
                if (context.PlanSetup.Dose == null)
                {
                    MessageBox.Show("Warning: No dose calculation found in the plan. Please calculate dose before using SRShelper for complete analysis.",
                                    "Dose Calculation Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                }

                // Note: Plan data export functionality removed
                // The application now works independently without Eclipse data integration

                // ====================================================================================
                // STEP 5: LAUNCH THE HTML APP IN DEFAULT BROWSER
                // ====================================================================================
                // Note: Eclipse DICOM auto-import functionality has been removed
                // The application now runs standalone without Eclipse data integration


                // Create the file:// URL for the HTML file (without parameters due to browser restrictions)
                string htmlUrl = new Uri(htmlFilePath).ToString();
                string urlWithParams = htmlUrl;

                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = urlWithParams,
                    UseShellExecute = true // This will open the URL in the default browser
                };

                Process process = Process.Start(startInfo);
                if (process == null)
                {
                    throw new ApplicationException("Failed to start the browser process.");
                }

                // Show success message for SRShelper
                string structureCount = "0";
                if (context.StructureSet != null && context.StructureSet.Structures != null)
                {
                    structureCount = context.StructureSet.Structures.Count().ToString();
                }
                string beamCount = treatmentBeams.Count.ToString();
                
                MessageBox.Show(string.Format("SRShelper launched successfully!\n\nPlan: {0}\nTreatment Beams: {1}\nStructures: {2}\n\nThe application will open in your default browser. You can now manually load DICOM files for analysis.",
                                context.PlanSetup.Id, beamCount, structureCount),
                                "SRShelper Launch Successful", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (ApplicationException appEx)
            {
                MessageBox.Show(string.Format("Application Error: {0}", appEx.Message), "Application Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                string errorMessage = string.Format("An unexpected error occurred:\n\n{0}\n\n{1}", ex.Message, ex.StackTrace);
                MessageBox.Show(errorMessage, "Critical Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Gets the source file path of the calling method (used to determine launcher directory)
        /// </summary>
        /// <param name="sourceFilePath">Automatically filled by compiler with the source file path</param>
        /// <returns>The full path to the source file</returns>
        public string GetSourceFilePath([CallerFilePath] string sourceFilePath = "")
        {
            return sourceFilePath;
        }
    }

    // SrsDataExporter class removed - Eclipse DICOM auto-import functionality no longer needed
}
