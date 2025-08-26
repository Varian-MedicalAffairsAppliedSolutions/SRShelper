# SRShelper - README

**Version:** 1.0.0  
**Author:** Taoran Li, PhD  
**Date:** August 2025

## 1. Overview

SRShelper is a web-based application designed for comprehensive analysis and comparison of stereotactic radiosurgery (SRS) treatment plans. It provides advanced dosimetric evaluation capabilities specifically tailored for SRS applications, allowing users to:

* Load and analyze RT Structure Set (`.dcm`) and RT Dose (`.dcm`) files from multiple treatment plans.
* Calculate comprehensive SRS quality metrics including:
    * **Conformity Indices:** Paddick CI and RTOG CI for dose conformity assessment.
    * **Gradient Index (GI):** Quantifies dose fall-off characteristics outside the target.
    * **Homogeneity Index (HI):** Evaluates dose uniformity within the target volume.
    * **Coverage:** Percentage of target receiving prescription dose.
    * **V12Gy:** Volume receiving 12 Gy or more (critical for normal tissue sparing).
    * **PIV (Prescription Isodose Volume):** Total volume receiving prescription dose.
* Visualize results through interactive analysis plots comparing plans across multiple metrics.
* Examine target structures and dose distributions in a real-time **orthogonal slice viewer** with:
    * Axial, sagittal, and coronal slice views.
    * Isodose line rendering (100%, 50%, and 12 Gy levels).
    * Structure contour overlays with bounding box visualization.
* Export detailed comparison results to CSV format for further analysis.
* Compare multiple treatment plans side-by-side for the same patient anatomy.

The tool runs entirely in the browser, ensuring patient data privacy and requiring no software installation. It provides quantitative insights into SRS plan quality for educational, research, and quality assurance applications.

---

## 2. System Requirements

This tool is designed to run in a modern web browser and has no other software dependencies.

*   **Recommended Browsers:** For the best performance and compatibility, please use the latest version of:
    *   **Google Chrome** ([Download](https://www.google.com/chrome/))
    *   **Mozilla Firefox** ([Download](https://www.mozilla.org/firefox/new/))

*   **Other Supported Browsers:** The tool is also compatible with:
    *   Microsoft Edge (latest version)
    *   Safari (version 14 or newer)

*   **Unsupported:** Internet Explorer is not supported.

*   **Hardware Requirements:**
    *   Minimum 4GB RAM (8GB+ recommended for complex cases)
    *   Modern graphics card supporting HTML5 Canvas
    *   Sufficient storage for DICOM files (typically 50-500MB per plan)

For users in environments with software installation restrictions, please contact your IT department to request that a supported browser be installed.

---

## 3. How to Use

### Step 1: Load DICOM Files

The tool requires both RT Structure Set and RT Dose files for analysis:

1.  **Single Plan Analysis:**
    * Drag and drop your RT Structure Set (`.dcm`) file onto the file drop area.
    * Drag and drop one or more RT Dose (`.dcm`) files onto the same area.
    * Files will be automatically classified and loaded.

2.  **Multi-Plan Comparison:**
    * Load the RT Structure Set file first.
    * Add multiple RT Dose files from different treatment plans.
    * Each dose file represents a separate plan for comparison.

### Step 2: Select Target Structures

* **Automatic Selection:** Use the "Select GTV/PTV/CTV" button to automatically select common target volume structures.
* **Manual Selection:** 
    * Click individual structures in the structure list.
    * Use Ctrl+Click for multi-selection.
    * Use Shift+Click for range selection.
* **Configure Prescription Doses:** Set individual prescription doses for each selected structure if they differ from the default.

### Step 3: Configure Analysis Parameters

* **Bounding Box Margin:** Adjust the calculation region margin around structures (default: 15mm).
* **Grid Resolution:** The tool automatically adapts calculation grid resolution based on target size:
    * Targets â‰¥10mm diameter: 1.0mm grid for efficiency
    * Smaller targets: Progressive refinement (1.0mm â†’ 0.5mm â†’ 0.25mm) until convergence.

### Step 4: Calculate and Analyze

* **Calculate Metrics:** Click "Calculate Metrics for Selected" to compute SRS quality indices.
* **Interactive Analysis:**
    * **Analysis Plots:** Four scatter plots showing metrics vs. effective diameter.
    * **Comparison Table:** Detailed numerical results with plan grouping.
    * **Orthogonal Viewer:** Click on plot points or table rows to visualize structures and dose distributions in axial, sagittal, and coronal slice views.
* **Export Results:** Use "Export CSV" to save detailed comparison data.
* **Clear Results:** Use "Clear Calculation Results" to reset analysis while keeping files loaded.

---

## 4. Calculated SRS Quality Metrics

### 4.1. Conformity Indices

#### 4.1.1. Paddick Conformity Index (CI)
SRS conformity assessment, defined as:
```
Paddick CI = (TV_PIV)Â² / (TV Ã— PIV)
```
Where:
- TV_PIV = Target Volume covered by Prescription Isodose Volume
- TV = Target Volume  
- PIV = Prescription Isodose Volume

**Ideal Value:** 1.0 (perfect conformity)

#### 4.1.2. RTOG Conformity Index
A simpler conformity metric defined as:
```
RTOG CI = PIV / TV
```
**Ideal Value:** 1.0

### 4.2. Gradient Index (GI)
Quantifies dose fall-off characteristics outside the target:
```
GI = V50% / V100%
```
Where V_50% is the volume receiving 50% of prescription dose.

**Lower values indicate better dose gradients**

### 4.3. Homogeneity Index (HI)
Evaluates dose uniformity within the target:
```
HI = (D2% - D98%) / D50%
```
Where D2%, D50%, D98% are dose levels to 2%, 50%, and 98% of target volume.

**Lower values indicate better homogeneity**

### 4.4. Coverage
Percentage of target volume receiving the prescription dose:
```
Coverage = TV_PIV / TV * 100%
```

### 4.5. V12Gy
Volume of normal tissue receiving 12 Gy or more, critical for minimizing radiation necrosis risk in brain SRS.

### 4.6. Additional Metrics
- **Target Volume (TV):** Structure volume in cubic centimeters
- **Maximum Dose (Dmax):** Peak dose within the target
- **Effective Diameter:** Sphere-equivalent diameter of the target volume

---

## 5. Visualization Features

### 5.1. Analysis Plots
Four interactive scatter plots display:
- **Paddick CI vs. Effective Diameter**
- **Gradient Index vs. Effective Diameter**  
- **RTOG CI vs. Effective Diameter**
- **V12Gy vs. Effective Diameter**

Each plot groups results by treatment plan, allowing direct comparison of plan performance across different target sizes.

### 5.2. Orthogonal Slice Viewer
Real-time visualization system featuring:
- **Three Orthogonal Views:** Axial, sagittal, and coronal slices
- **Isodose Rendering:** 100% (red), 50% (blue), and 12 Gy (orange) isodose lines
- **Structure Overlays:** Target volume contours with raw point visualization
- **Interactive Navigation:** Mouse wheel zoom, synchronized across all views
- **Calculation Bounding Box:** Visual indication of analysis region

### 5.3. Comparison Table
Comprehensive results table with:
- **Plan Grouping:** Results organized by treatment plan and target structure
- **Interactive Selection:** Click rows to view in orthogonal slice viewer
- **Export Capability:** CSV export for statistical analysis
- **Color-Coded Highlighting:** Selected results highlighted across interface

---

## 6. Advanced Features

### 6.1. Adaptive Grid Resolution
The calculation engine automatically optimizes grid resolution based on target characteristics:
- **Large Targets (>10mm):** Single-pass 1.0mm grid for computational efficiency
- **Small Targets (<10mm):** Multi-pass refinement with convergence criteria to ensure accuracy

### 6.2. Intelligent Clipping Planes
When analyzing multiple targets simultaneously, the tool implements spatial optimization to prevent calculation overlap and improve performance.

### 6.3. Multi-Plan Workflow
Designed for clinical plan comparison scenarios:
- Load reference anatomy once
- Add multiple dose distributions for comparison
- Analyze plan performance across identical target definitions
- Quantitative ranking for plan selection

---

## 7. File Format Support

### 7.1. Input Requirements
- **RT Structure Set:** DICOM format (`.dcm`) containing target and organ contours
- **RT Dose:** DICOM format (`.dcm`) containing 3D dose distributions
- **Transfer Syntax:** Uncompressed DICOM required (compressed formats not supported)

### 7.2. Output Formats
- **CSV Export:** Detailed numerical results suitable for statistical analysis
- **Interactive Results:** Web-based visualization and analysis interface

---

## 8. Clinical Considerations

### 8.1. Quality Assurance Applications
This tool is designed to support:
- **Plan Evaluation:** Quantitative assessment of SRS plan quality
- **Plan Comparison:** Side-by-side analysis of treatment alternatives  
- **Quality Metrics:** Standardized SRS indices for departmental QA programs
- **Educational Use:** Teaching SRS planning principles and dose optimization

### 8.2. Validation and Limitations
- **Research Tool:** Designed for educational and research applications
- **Not Clinical Software:** Results should be independently verified for clinical use
- **Calculation Method:** Monte Carlo-style sampling with adaptive resolution
- **Coordinate Systems:** Assumes standard DICOM coordinate conventions

---

## 9. References

### 9.1. SRS Quality Metrics
* Paddick, I. (2000). A simple scoring ratio to index the conformity of radiosurgical treatment plans. *Journal of Neurosurgery*, 93(3), 219-222.
* Shaw, E., Kline, R., Gillin, M., Souhami, L., Hirschfeld, A., Dinapoli, R., & Martin, L. (1993). Radiation Therapy Oncology Group: radiosurgery quality assurance guidelines. *International Journal of Radiation Oncology, Biology, Physics*, 27(5), 1231-1239.
* Nakamura, J. L., Verhey, L. J., Smith, V., Petti, P. L., Lamborn, K. R., Larson, D. A., Wara, W. M., McDermott, M. W., & Sneed, P. K. (2001). Dose conformity of gamma knife radiosurgery and risk factors for complications. *International Journal of Radiation Oncology, Biology, Physics*, 51(5), 1313-1319.

### 9.2. Gradient Index and Dose Fall-off
* Lomax, N. J., & Scheib, S. G. (2003). Quantifying the degree of conformity in radiosurgery treatment planning. *International Journal of Radiation Oncology, Biology, Physics*, 55(5), 1409-1419.
* Feuvret, L., et al. (2006). Conformity index: a review. *International Journal of Radiation Oncology, Biology, Physics*, 64(2), 333-342.

### 9.3. Clinical Guidelines
* Benedict, S. H., et al. (2010). Stereotactic body radiation therapy: the report of AAPM Task Group 101. *Medical Physics*, 37(8), 4078-4101.

---

## 10. Support and Development

### 10.1. Technical Support
For technical issues or questions:
- Check browser compatibility (Chrome/Firefox recommended)
- Verify DICOM file format (uncompressed required)  
- Ensure adequate system memory for large datasets

### 10.2. Feature Requests
This tool is actively developed for research and educational applications. Feature requests and feedback are welcome for future development consideration.

---

**IMPORTANT DISCLAIMER**

**This software is provided for educational and research purposes only. It has not been validated for clinical use and should not be used for patient treatment planning or clinical decision-making. All results should be independently verified using clinically validated software before any clinical application.**
