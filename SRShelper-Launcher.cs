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

                // ====================================================================================
                // STEP 4: GENERATE THE PLAN DATA AS A JSON STRING (SRS-focused)
                // ====================================================================================
                var srsDataExporter = new SrsDataExporter();
                string jsonOutput = srsDataExporter.ExportSrsPlanData(context.PlanSetup, context.StructureSet);

                // ====================================================================================
                // STEP 5: SAVE JSON DATA AND PARAMETER FILE FOR BROWSER LAUNCH
                // ====================================================================================
                // Save plan data to temp file
                string tempFileName = string.Format("eclipse-srs-plan-{0}.json", DateTime.Now.ToString("yyyyMMdd-HHmmss"));
                string tempFilePath = Path.Combine(Path.GetTempPath(), tempFileName);
                File.WriteAllText(tempFilePath, jsonOutput);

                // Create parameter JavaScript file (bypasses CORS restrictions)
                string paramFileName = "eclipse-launch-params.js";
                string paramFilePath = Path.Combine(Path.GetDirectoryName(htmlFilePath), paramFileName);
                
                // Create parameter JavaScript file (compatible with older .NET versions)
                // Properly escape the JSON string for JavaScript embedding
                string escapedJsonOutput = jsonOutput.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r", "").Replace("\n", "\\n");
                
                var paramJs = string.Format(@"// Eclipse launch parameters for SRShelper
window.eclipseLaunchParams = {{
  mode: ""eclipse"",
  planFile: ""{0}"",
  patientId: ""{1}"",
  courseId: ""{2}"",
  planId: ""{3}"",
  timestamp: ""{4}"",
  planData: ""{5}"",
  application: ""SRShelper""
}};

// Auto-trigger loading if function exists
if (typeof window.loadEclipseSrsPlan === 'function') {{
  console.log('DEBUG: Auto-triggering Eclipse SRS plan loading...');
  window.loadEclipseSrsPlan(window.eclipseLaunchParams);
}} else if (typeof window.loadEclipsePlan === 'function') {{
  console.log('DEBUG: Auto-triggering Eclipse plan loading (fallback)...');
  window.loadEclipsePlan(window.eclipseLaunchParams);
}}", 
                    tempFilePath.Replace("\\", "\\\\").Replace("\"", "\\\""),
                    context.Patient.Id.Replace("\"", "\\\""),
                    context.Course.Id.Replace("\"", "\\\""),
                    context.PlanSetup.Id.Replace("\"", "\\\""),
                    DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    escapedJsonOutput);
                
                File.WriteAllText(paramFilePath, paramJs);

                // ====================================================================================
                // STEP 6: LAUNCH THE HTML APP IN DEFAULT BROWSER
                // ====================================================================================
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
                
                MessageBox.Show(string.Format("SRShelper launched successfully!\n\nPlan: {0}\nTreatment Beams: {1}\nStructures: {2}\n\nThe application will open in your default browser with the plan data automatically loaded.",
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

    public class SrsDataExporter
    {
        private const int IndentSize = 2;

        public string ExportSrsPlanData(PlanSetup plan, StructureSet structureSet)
        {
            var sb = new StringBuilder();
            sb.AppendLine("{");
            sb.AppendLine(Indent(1) + "\"srsHelperData\": {");

            // Patient Information
            sb.AppendLine(FormatProperty("patientID", plan.Course.Patient.Id, 2, false));
            sb.AppendLine(FormatProperty("patientName", plan.Course.Patient.Name, 2, false));
            sb.AppendLine(FormatProperty("courseID", plan.Course.Id, 2, false));
            sb.AppendLine(FormatProperty("planID", plan.Id, 2, false));
            string planCreationDate = "";
            if (plan.CreationDateTime != null)
            {
                planCreationDate = plan.CreationDateTime.Value.ToString("yyyy-MM-dd HH:mm:ss");
            }
            sb.AppendLine(FormatProperty("planCreationDate", planCreationDate, 2, false));

            // Plan Information
            double totalPlanMU = plan.Beams.Where(b => !b.IsSetupField).Sum(b => b.Meterset.Value);
            sb.AppendLine(FormatProperty("totalPlanMU", totalPlanMU, 2, false));
            
            var treatmentBeams = plan.Beams.Where(b => !b.IsSetupField).ToList();
            sb.AppendLine(FormatProperty("numberOfBeams", treatmentBeams.Count, 2, false));
            
            // Technique Information
            string technique = DetermineTechnique(treatmentBeams);
            sb.AppendLine(FormatProperty("technique", technique, 2, false));

            // Export Structure Set Data for SRShelper
            if (structureSet != null && structureSet.Structures != null)
            {
                var targets = structureSet.Structures.Where(s => 
                    s.Id.ToUpper().Contains("GTV") || 
                    s.Id.ToUpper().Contains("PTV") || 
                    s.Id.ToUpper().Contains("CTV")).ToList();
                
                var targetStrings = targets.Select(t => BuildTargetJson(t, 3)).ToList();
                sb.AppendLine(FormatArray("targets", targetStrings, 2, false));
                
                var structureStrings = structureSet.Structures.Select(s => BuildStructureJson(s, 3)).ToList();
                sb.AppendLine(FormatArray("structures", structureStrings, 2, false));

                // Export full structure data for SRShelper compatibility
                var fullStructureData = BuildStructureSetData(structureSet, 3);
                sb.AppendLine(FormatProperty("structureSetData", fullStructureData, 2, false, false));
            }

            // Export Dose Data for SRShelper
            if (plan.Dose != null)
            {
                sb.AppendLine(FormatProperty("prescriptionDose", plan.TotalDose.Dose, 2, false));
                sb.AppendLine(FormatProperty("doseUnit", plan.TotalDose.Unit.ToString(), 2, false));
                int fractions = 1;
                if (plan.NumberOfFractions != null)
                {
                    fractions = plan.NumberOfFractions.Value;
                }
                sb.AppendLine(FormatProperty("fractions", fractions, 2, false));

                // Export dose grid data for SRShelper compatibility
                var doseData = BuildDoseData(plan.Dose, 3);
                sb.AppendLine(FormatProperty("doseData", doseData, 2, false, false));
            }

            // Beam Information (simplified for SRS analysis)
            var beamSummaryStrings = treatmentBeams.Select(b => BuildBeamSummaryJson(b, 3)).ToList();
            sb.AppendLine(FormatArray("beamSummary", beamSummaryStrings, 2, false));

            // Machine Information
            if (treatmentBeams.Any())
            {
                var firstBeam = treatmentBeams.First();
                string machineId = "";
                if (firstBeam.TreatmentUnit != null)
                {
                    machineId = firstBeam.TreatmentUnit.Id;
                }
                string energy = "";
                if (firstBeam.EnergyModeDisplayName != null)
                {
                    energy = firstBeam.EnergyModeDisplayName;
                }
                sb.AppendLine(FormatProperty("machineID", machineId, 2, false));
                sb.AppendLine(FormatProperty("energy", energy, 2, false));
            }

            sb.AppendLine(FormatProperty("exportTimestamp", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), 2, true));

            sb.AppendLine(Indent(1) + "}");
            sb.AppendLine("}");
            return sb.ToString();
        }

        private string DetermineTechnique(List<Beam> beams)
        {
            if (!beams.Any()) return "Unknown";
            
            bool hasArc = beams.Any(b => b.MLCPlanType == MLCPlanType.VMAT || 
                                        (b.ControlPoints.Count > 2 && 
                                         Math.Abs(b.ControlPoints.Last().GantryAngle - b.ControlPoints.First().GantryAngle) > 5));
            
            if (hasArc)
            {
                return beams.Count == 1 ? "Single Arc VMAT" : "Multiple Arc VMAT";
            }
            else
            {
                return beams.Count == 1 ? "Single Static Beam" : "Multiple Static Beams";
            }
        }

        private string BuildTargetJson(Structure target, int indentLevel)
        {
            var sb = new StringBuilder();
            sb.AppendLine(Indent(indentLevel) + "{");
            sb.AppendLine(FormatProperty("id", target.Id, indentLevel + 1, false));
            sb.AppendLine(FormatProperty("volume", target.Volume, indentLevel + 1, false));
            sb.AppendLine(FormatProperty("color", target.Color.ToString(), indentLevel + 1, false));
            sb.AppendLine(FormatProperty("isTarget", IsTargetStructure(target.Id), indentLevel + 1, true));
            sb.Append(Indent(indentLevel) + "}");
            return sb.ToString();
        }

        private string BuildStructureJson(Structure structure, int indentLevel)
        {
            var sb = new StringBuilder();
            sb.AppendLine(Indent(indentLevel) + "{");
            sb.AppendLine(FormatProperty("id", structure.Id, indentLevel + 1, false));
            sb.AppendLine(FormatProperty("volume", structure.Volume, indentLevel + 1, false));
            sb.AppendLine(FormatProperty("color", structure.Color.ToString(), indentLevel + 1, false));
            sb.AppendLine(FormatProperty("dicomType", structure.DicomType, indentLevel + 1, false));
            sb.AppendLine(FormatProperty("isTarget", IsTargetStructure(structure.Id), indentLevel + 1, true));
            sb.Append(Indent(indentLevel) + "}");
            return sb.ToString();
        }

        private string BuildBeamSummaryJson(Beam beam, int indentLevel)
        {
            var sb = new StringBuilder();
            sb.AppendLine(Indent(indentLevel) + "{");
            sb.AppendLine(FormatProperty("beamNumber", beam.BeamNumber, indentLevel + 1, false));
            sb.AppendLine(FormatProperty("beamName", beam.Id, indentLevel + 1, false));
            sb.AppendLine(FormatProperty("gantryStart", beam.ControlPoints.First().GantryAngle, indentLevel + 1, false));
            sb.AppendLine(FormatProperty("gantryEnd", beam.ControlPoints.Last().GantryAngle, indentLevel + 1, false));
            sb.AppendLine(FormatProperty("collimatorAngle", beam.ControlPoints.First().CollimatorAngle, indentLevel + 1, false));
            sb.AppendLine(FormatProperty("totalMU", beam.Meterset.Value, indentLevel + 1, false));
            sb.AppendLine(FormatProperty("doseRate", beam.DoseRate, indentLevel + 1, false));
            sb.AppendLine(FormatProperty("energy", beam.EnergyModeDisplayName, indentLevel + 1, true));
            sb.Append(Indent(indentLevel) + "}");
            return sb.ToString();
        }

        private bool IsTargetStructure(string structureId)
        {
            string upperName = structureId.ToUpper();
            return upperName.Contains("GTV") || upperName.Contains("PTV") || upperName.Contains("CTV");
        }

        private string BuildStructureSetData(StructureSet structureSet, int indentLevel)
        {
            var sb = new StringBuilder();
            sb.AppendLine(Indent(indentLevel) + "{");
            
            // StructureSet metadata
            sb.AppendLine(FormatProperty("sopClassUid", "1.2.840.10008.5.1.4.1.1.481.3", indentLevel + 1, false)); // RT Structure Set
            sb.AppendLine(FormatProperty("sopInstanceUid", structureSet.Id, indentLevel + 1, false));
            sb.AppendLine(FormatProperty("studyInstanceUid", "Unknown", indentLevel + 1, false));
            sb.AppendLine(FormatProperty("seriesInstanceUid", "Unknown", indentLevel + 1, false));
            
            // Safe Frame of Reference access
            string frameOfReferenceUid = "Unknown";
            try
            {
                if (structureSet.Image != null)
                {
                    frameOfReferenceUid = structureSet.Image.FOR;
                }
            }
            catch (Exception)
            {
                frameOfReferenceUid = "Not Available";
            }
            sb.AppendLine(FormatProperty("frameOfReferenceUid", frameOfReferenceUid, indentLevel + 1, false));
            
            // Structures data
            var structuresData = new List<string>();
            foreach (var structure in structureSet.Structures)
            {
                var structData = new StringBuilder();
                structData.AppendLine(Indent(indentLevel + 2) + "{");
                structData.AppendLine(FormatProperty("id", structure.Id, indentLevel + 3, false));
                structData.AppendLine(FormatProperty("dicomType", structure.DicomType, indentLevel + 3, false));
                structData.AppendLine(FormatProperty("color", string.Format("rgba({0}, {1}, {2}, 1)", structure.Color.R, structure.Color.G, structure.Color.B), indentLevel + 3, false));
                structData.AppendLine(FormatProperty("volume", structure.Volume, indentLevel + 3, false));
                structData.AppendLine(FormatProperty("assignedHU", "Not Available", indentLevel + 3, false));
                
                // Simplified contour representation
                structData.AppendLine(FormatProperty("contours", "[]", indentLevel + 3, true, false));
                
                structData.Append(Indent(indentLevel + 2) + "}");
                structuresData.Add(structData.ToString());
            }
            
            sb.AppendLine(FormatArray("structures", structuresData, indentLevel + 1, true));
            sb.Append(Indent(indentLevel) + "}");
            
            return sb.ToString();
        }

        private string BuildDoseData(Dose dose, int indentLevel)
        {
            var sb = new StringBuilder();
            sb.AppendLine(Indent(indentLevel) + "{");
            
            // Dose metadata
            sb.AppendLine(FormatProperty("sopClassUid", "1.2.840.10008.5.1.4.1.1.481.2", indentLevel + 1, false)); // RT Dose
            sb.AppendLine(FormatProperty("sopInstanceUid", dose.Id, indentLevel + 1, false));
            sb.AppendLine(FormatProperty("doseUnits", dose.DoseMax3D.Unit.ToString(), indentLevel + 1, false));
            sb.AppendLine(FormatProperty("doseType", "PHYSICAL", indentLevel + 1, false));
            sb.AppendLine(FormatProperty("doseComment", "Exported from Eclipse ESAPI", indentLevel + 1, false));
            
            // Grid properties
            sb.AppendLine(FormatProperty("xSize", dose.XSize, indentLevel + 1, false));
            sb.AppendLine(FormatProperty("ySize", dose.YSize, indentLevel + 1, false));
            sb.AppendLine(FormatProperty("zSize", dose.ZSize, indentLevel + 1, false));
            sb.AppendLine(FormatProperty("xRes", dose.XRes, indentLevel + 1, false));
            sb.AppendLine(FormatProperty("yRes", dose.YRes, indentLevel + 1, false));
            sb.AppendLine(FormatProperty("zRes", dose.ZRes, indentLevel + 1, false));
            
            // Origin
            sb.AppendLine(FormatProperty("origin", string.Format(CultureInfo.InvariantCulture, 
                "[{0:F4}, {1:F4}, {2:F4}]", dose.Origin.x, dose.Origin.y, dose.Origin.z), indentLevel + 1, false, false));
            
            // Dose statistics
            sb.AppendLine(FormatProperty("doseMax3D", dose.DoseMax3D.Dose, indentLevel + 1, false));
            sb.AppendLine(FormatProperty("doseMax3DLocation", string.Format(CultureInfo.InvariantCulture,
                "[{0:F4}, {1:F4}, {2:F4}]", dose.DoseMax3DLocation.x, dose.DoseMax3DLocation.y, dose.DoseMax3DLocation.z), indentLevel + 1, false, false));
            
            // Note: We don't export the full dose grid due to size limitations
            // The browser will need to use the Eclipse context or request specific dose values
            sb.AppendLine(FormatProperty("note", "Full dose grid available through Eclipse context - use getDoseAtPoint for specific values", indentLevel + 1, true));
            
            sb.Append(Indent(indentLevel) + "}");
            return sb.ToString();
        }

        #region JSON Building Helpers
        private string Indent(int level) { return new string(' ', level * IndentSize); }
        private string EscapeString(string s) { return s.Replace("\\", "\\\\").Replace("\"", "\\\""); }

        private string FormatProperty(string key, object value, int indentLevel, bool isLast, bool quoteValue = true)
        {
            string formattedValue;
            if (value is bool)
            {
                formattedValue = ((bool)value) ? "true" : "false";
            }
            else if (value is string && quoteValue)
            {
                formattedValue = "\"" + EscapeString((string)value) + "\"";
            }
            else if (value is double || value is float || value is decimal)
            {
                formattedValue = string.Format(CultureInfo.InvariantCulture, "{0:F4}", value);
            }
            else
            {
                formattedValue = (value != null ? value.ToString() : "null");
            }
            return string.Format("{0}\"{1}\": {2}{3}", Indent(indentLevel), key, formattedValue, isLast ? "" : ",");
        }

        private string FormatArray(string key, List<string> items, int indentLevel, bool isLast, bool formatAsObjects = true)
        {
            var sb = new StringBuilder();
            sb.Append(string.Format("{0}\"{1}\": [", Indent(indentLevel), key));
            if (items.Any())
            {
                if (formatAsObjects)
                {
                    sb.AppendLine();
                    sb.Append(string.Join(",\n", items.ToArray()));
                    sb.AppendLine();
                    sb.Append(Indent(indentLevel));
                }
                else
                {
                    sb.Append(string.Join(", ", items.ToArray()));
                }
            }
            sb.Append(string.Format("]{0}", isLast ? "" : ","));
            return sb.ToString();
        }
        #endregion
    }
}
