namespace Ares.Visualizer.Demo;

public static class SamplePlotlySpecs
{
  public static readonly Dictionary<string, string> Presets = new()
    {
        { "Tensile Stress-Strain Curves (Temp Dependent)", TensileTestCurves },
        { "Nanoindentation Elastic Modulus Map (Microstructure)", NanoindentationHeatmap },
        { "XRD Crystallographic Diffraction Pattern", XrdDiffractionPattern },
        { "Differential Scanning Calorimetry (DSC) Thermal Analysis", DscThermalAnalysis },
        { "3D Atom Probe Tomography (APT) Nano-Cluster", AtomProbeTomography3D },
        { "3D Von Mises Stress Yield Surface", VonMisesYieldSurface3D },
        { "3D XRD Reciprocal Space Map (RSM)", ReciprocalSpaceMap3D }
    };

  #region 1. 3D Atom Probe Tomography (APT) Reconstruction
  public const string AtomProbeTomography3D = @"{
      ""data"": [
        {
          ""x"": [0.1, 0.4, 0.2, 0.8, 1.1, 0.9, 1.4, 1.2, 0.3, 0.7, 1.5, 0.5],
          ""y"": [0.2, 0.5, 0.8, 0.3, 0.6, 1.0, 0.1, 0.9, 1.2, 1.1, 0.4, 0.7],
          ""z"": [1.1, 1.4, 1.2, 2.1, 2.3, 2.0, 3.1, 3.5, 3.2, 2.8, 3.0, 1.8],
          ""mode"": ""markers"",
          ""type"": ""scatter3d"",
          ""name"": ""Matrix (Fe)"",
          ""marker"": { ""size"": 3, ""color"": ""#7f8c8d"", ""opacity"": 0.3 }
        },
        {
          ""x"": [2.1, 2.3, 2.2, 2.5, 2.4, 2.2, 2.6, 2.3, 2.1, 2.5],
          ""y"": [2.0, 2.2, 2.4, 2.1, 2.3, 2.5, 2.2, 2.0, 2.1, 2.4],
          ""z"": [5.1, 5.3, 5.0, 5.2, 5.5, 5.4, 5.1, 5.3, 5.6, 5.2],
          ""mode"": ""markers"",
          ""type"": ""scatter3d"",
          ""name"": ""Precipitate (Cu Clusters)"",
          ""marker"": { ""size"": 7, ""color"": ""#e67e22"", ""opacity"": 0.95 }
        },
        {
          ""x"": [2.0, 2.2, 2.5, 2.1, 2.3, 2.4],
          ""y"": [1.9, 2.1, 2.2, 2.3, 2.0, 2.5],
          ""z"": [4.9, 5.1, 5.3, 5.4, 5.2, 5.0],
          ""mode"": ""markers"",
          ""type"": ""scatter3d"",
          ""name"": ""Solute Segregation (Ni/Al)"",
          ""marker"": { ""size"": 5, ""color"": ""#00bc8c"", ""opacity"": 0.85 }
        }
      ],
      ""layout"": {
        ""title"": ""Atom Probe Tomography (3D Nanoscale Phase Separation)"",
        ""paper_bgcolor"": ""rgba(0,0,0,0)"",
        ""font"": { ""color"": ""#e0e0e0"" },
        ""scene"": {
          ""xaxis"": { ""title"": ""X (nm)"", ""gridcolor"": ""#444"" },
          ""yaxis"": { ""title"": ""Y (nm)"", ""gridcolor"": ""#444"" },
          ""zaxis"": { ""title"": ""Z Tip Height (nm)"", ""gridcolor"": ""#444"" },
          ""aspectratio"": { ""x"": 1, ""y"": 1, ""z"": 2 }
        }
      }
    }";
  #endregion

  #region 2. 3D Von Mises Stress Yield Surface
  public const string VonMisesYieldSurface3D = @"{
      ""data"": [
        {
          ""z"": [
            [200, 195, 180, 150, 100, 0, -100, -150, -180, -195, -200],
            [195, 210, 200, 170, 120, 0, -120, -170, -200, -210, -195],
            [180, 200, 220, 200, 150, 0, -150, -200, -220, -200, -180],
            [150, 170, 200, 210, 180, 0, -180, -210, -200, -170, -150],
            [100, 120, 150, 180, 200, 0, -200, -180, -150, -120, -100],
            [0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0],
            [-100, -120, -150, -180, -200, 0, 200, 180, 150, 120, 100],
            [-150, -170, -200, -210, -180, 0, 180, 210, 200, 170, 150],
            [-180, -200, -220, -200, -150, 0, 150, 200, 220, 200, 180],
            [-195, -210, -200, -170, -120, 0, 120, 170, 200, 210, 195],
            [-200, -195, -180, -150, -100, 0, 100, 150, 180, 195, 200]
          ],
          ""x"": [-250, -200, -150, -100, -50, 0, 50, 100, 150, 200, 250],
          ""y"": [-250, -200, -150, -100, -50, 0, 50, 100, 150, 200, 250],
          ""type"": ""surface"",
          ""colorscale"": ""Plasma"",
          ""colorbar"": { ""title"": ""Von Mises Yield Limit (MPa)"" }
        }
      ],
      ""layout"": {
        ""title"": ""3D Plastic Yield Envelope (Principal Stress State)"",
        ""paper_bgcolor"": ""rgba(0,0,0,0)"",
        ""font"": { ""color"": ""#e0e0e0"" },
        ""scene"": {
          ""xaxis"": { ""title"": ""σ1 (MPa)"", ""gridcolor"": ""#444"" },
          ""yaxis"": { ""title"": ""σ2 (MPa)"", ""gridcolor"": ""#444"" },
          ""zaxis"": { ""title"": ""σ3 (MPa)"", ""gridcolor"": ""#444"" }
        }
      }
    }";
  #endregion

  #region 3. 3D XRD Reciprocal Space Map (RSM)
  public const string ReciprocalSpaceMap3D = @"{
      ""data"": [
        {
          ""z"": [
            [12, 18, 30, 45, 30, 18, 12],
            [18, 45, 120, 250, 120, 45, 18],
            [30, 120, 850, 3200, 850, 120, 30],
            [45, 250, 3200, 15000, 3200, 250, 45],
            [30, 120, 850, 3200, 850, 120, 30],
            [18, 45, 120, 250, 120, 45, 18],
            [12, 18, 30, 45, 30, 18, 12]
          ],
          ""x"": [2.21, 2.22, 2.23, 2.24, 2.25, 2.26, 2.27],
          ""y"": [4.40, 4.41, 4.42, 4.43, 4.44, 4.45, 4.46],
          ""type"": ""surface"",
          ""colorscale"": ""Jet"",
          ""colorbar"": { ""title"": ""Intensity (Log CPS)"", ""type"": ""log"" }
        }
      ],
      ""layout"": {
        ""title"": ""3D High-Resolution XRD Reciprocal Space Map (Epitaxial Strain)"",
        ""paper_bgcolor"": ""rgba(0,0,0,0)"",
        ""font"": { ""color"": ""#e0e0e0"" },
        ""scene"": {
          ""xaxis"": { ""title"": ""Qx (1/Å)"", ""gridcolor"": ""#444"" },
          ""yaxis"": { ""title"": ""Qz (1/Å)"", ""gridcolor"": ""#444"" },
          ""zaxis"": { ""title"": ""Diffraction Intensity"", ""gridcolor"": ""#444"", ""type"": ""log"" }
        }
      }
    }";
  #endregion

  #region 4. Temperature-Dependent Tensile Stress-Strain Curves
  public const string TensileTestCurves = @"{
      ""data"": [
        {
          ""x"": [0.0, 0.002, 0.01, 0.03, 0.06, 0.09, 0.12, 0.14],
          ""y"": [0, 450, 680, 820, 890, 910, 880, 750],
          ""type"": ""scatter"",
          ""mode"": ""lines"",
          ""name"": ""25°C (Room Temp)"",
          ""line"": { ""color"": ""#3498db"", ""width"": 3 }
        },
        {
          ""x"": [0.0, 0.002, 0.01, 0.04, 0.08, 0.13, 0.18, 0.22],
          ""y"": [0, 380, 520, 640, 700, 710, 680, 520],
          ""type"": ""scatter"",
          ""mode"": ""lines"",
          ""name"": ""450°C (Intermediate)"",
          ""line"": { ""color"": ""#f39c12"", ""width"": 3 }
        },
        {
          ""x"": [0.0, 0.002, 0.01, 0.05, 0.12, 0.20, 0.30, 0.38],
          ""y"": [0, 210, 290, 360, 410, 420, 390, 280],
          ""type"": ""scatter"",
          ""mode"": ""lines"",
          ""name"": ""750°C (High Temp Creep Region)"",
          ""line"": { ""color"": ""#e74c3c"", ""width"": 3 }
        }
      ],
      ""layout"": {
        ""title"": ""Ti-6Al-4V Tensile Deformation Across Temperature Regimes"",
        ""paper_bgcolor"": ""rgba(0,0,0,0)"",
        ""plot_bgcolor"": ""rgba(0,0,0,0)"",
        ""font"": { ""color"": ""#e0e0e0"" },
        ""xaxis"": { ""title"": ""Engineering Strain (mm/mm)"", ""gridcolor"": ""#333333"" },
        ""yaxis"": { ""title"": ""Engineering Stress (MPa)"", ""gridcolor"": ""#333333"" },
        ""annotations"": [
          {
            ""x"": 0.09, ""y"": 910,
            ""text"": ""UTS: 910 MPa"",
            ""showarrow"": true,
            ""arrowhead"": 2,
            ""ax"": 0, ""ay"": -30,
            ""font"": { ""color"": ""#ffffff"" },
            ""arrowcolor"": ""#ffffff""
          }
        ]
      }
    }";
  #endregion

  #region 5. Nanoindentation Elastic Modulus Map
  public const string NanoindentationHeatmap = @"{
      ""data"": [
        {
          ""z"": [
            [210, 212, 215, 160, 158, 155],
            [208, 218, 211, 162, 161, 159],
            [214, 216, 185, 165, 157, 156],
            [165, 170, 162, 210, 215, 218],
            [158, 160, 159, 212, 220, 216],
            [155, 157, 161, 214, 217, 219]
          ],
          ""x"": [""0 μm"", ""5 μm"", ""10 μm"", ""15 μm"", ""20 μm"", ""25 μm""],
          ""y"": [""0 μm"", ""5 μm"", ""10 μm"", ""15 μm"", ""20 μm"", ""25 μm""],
          ""type"": ""heatmap"",
          ""colorscale"": ""Viridis"",
          ""colorbar"": { ""title"": ""Elastic Modulus E (GPa)"" }
        }
      ],
      ""layout"": {
        ""title"": ""Microstructural Nanoindentation Array (Ferrite vs Martensite Phases)"",
        ""paper_bgcolor"": ""rgba(0,0,0,0)"",
        ""plot_bgcolor"": ""rgba(0,0,0,0)"",
        ""font"": { ""color"": ""#e0e0e0"" },
        ""xaxis"": { ""title"": ""X Grid Spatial Coordinate"", ""gridcolor"": ""#333333"" },
        ""yaxis"": { ""title"": ""Y Grid Spatial Coordinate"", ""gridcolor"": ""#333333"" }
      }
    }";
  #endregion

  #region 6. Crystallographic XRD Diffraction Pattern
  public const string XrdDiffractionPattern = @"{
      ""data"": [
        {
          ""x"": [20, 35, 38.5, 40, 44.7, 50, 65.1, 70, 78.2, 85],
          ""y"": [120, 140, 8500, 160, 4200, 180, 2100, 150, 3100, 130],
          ""type"": ""scatter"",
          ""mode"": ""lines"",
          ""name"": ""Measured Intensity"",
          ""line"": { ""color"": ""#00bc8c"", ""width"": 2 }
        }
      ],
      ""layout"": {
        ""title"": ""X-Ray Diffraction Spectrum(FCC Austenite Phase Verification)"",
        ""paper_bgcolor"": ""rgba(0,0,0,0)"",
        ""plot_bgcolor"": ""rgba(0,0,0,0)"",
        ""font"": { ""color"": ""#e0e0e0"" },
        ""xaxis"": { ""title"": ""Diffraction Angle 2θ(degrees)"", ""gridcolor"": ""#333333"" },
        ""yaxis"": { ""title"": ""Intensity(Counts)"", ""gridcolor"": ""#333333"" },
        ""annotations"": [
          { ""x"": 38.5, ""y"": 8500, ""text"": ""(111)"", ""showarrow"": true, ""arrowhead"": 1, ""ax"": 0, ""ay"": -25 },
          { ""x"": 44.7, ""y"": 4200, ""text"": ""(200)"", ""showarrow"": true, ""arrowhead"": 1, ""ax"": 0, ""ay"": -25 },
          { ""x"": 65.1, ""y"": 2100, ""text"": ""(220)"", ""showarrow"": true, ""arrowhead"": 1, ""ax"": 0, ""ay"": -25 },
          { ""x"": 78.2, ""y"": 3100, ""text"": ""(311)"", ""showarrow"": true, ""arrowhead"": 1, ""ax"": 0, ""ay"": -25 }
        ]
      }
    }";
    #endregion

#region 7. Differential Scanning Calorimetry (DSC)
    public const string DscThermalAnalysis = @"{
      ""data"": [
        {
          ""x"": [100, 150, 200, 220, 235, 250, 280, 320, 340, 355, 370, 400],
          ""y"": [0.02, 0.03, 0.04, -0.45, 0.12, 0.05, 0.03, 0.85, 0.92, 0.10, 0.02, 0.01],
          ""type"": ""scatter"",
          ""mode"": ""lines"",
          ""name"": ""Heat Flow (mW/mg)"",
          ""line"": { ""color"": ""#e93b81"", ""width"": 3 }
        }
      ],
      ""layout"": {
        ""title"": ""DSC Thermal Analysis (Glass Transition, Crystallization & Melting)"",
        ""paper_bgcolor"": ""rgba(0,0,0,0)"",
        ""plot_bgcolor"": ""rgba(0,0,0,0)"",
        ""font"": { ""color"": ""#e0e0e0"" },
        ""xaxis"": { ""title"": ""Temperature (°C)"", ""gridcolor"": ""#333333"" },
        ""yaxis"": { ""title"": ""Heat Flow (mW/mg, Endothermic Down)"", ""gridcolor"": ""#333333"" },
        ""annotations"": [
          { ""x"": 220, ""y"": -0.45, ""text"": ""Glass Transition (Tg)"", ""showarrow"": true, ""ax"": -40, ""ay"": -30 },
          { ""x"": 340, ""y"": 0.92, ""text"": ""Melting Peak (Tm)"", ""showarrow"": true, ""ax"": 40, ""ay"": -30 }
        ]
      }
    }";
    #endregion
}