using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using LibreHardwareMonitor.Hardware;

namespace HardwareMonitor
{
    public partial class MainWindow : Window
    {
        private Computer computer;
        private DispatcherTimer timer;

        // Lists to store historical data
        private List<float> cpuLoadHistory = new List<float>();
        private List<float> cpuTempHistory = new List<float>(); // New list for CPU temperature
        private List<float> cpuPowerHistory = new List<float>(); // New list for CPU power
        private List<float> gpuLoadHistory = new List<float>();
        private List<float> gpuTempHistory = new List<float>(); // New list for GPU temperature
        private List<float> gpuPowerHistory = new List<float>(); // New list for GPU power
        private List<float> gpuMemoryHistory = new List<float>();
        private List<float> memoryUsageHistory = new List<float>();
        
        // Maximum number of data points to keep
        private const int MAX_HISTORY_POINTS = 60;

        public MainWindow()
        {
            InitializeComponent();
            InitializeHardwareMonitor();
            SetupTimer();

            // Make the window always stay on top of other windows
            this.Topmost = false;
            // Ensure checkbox matches initial state
            AlwaysOnTopCheckBox.IsChecked = this.Topmost;
        }

        // Add these event handlers for the Always on Top checkbox
        private void AlwaysOnTopCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            this.Topmost = true;
        }

        private void AlwaysOnTopCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            this.Topmost = false;
        }

        private void InitializeHardwareMonitor()
        {
            computer = new Computer
            {
                IsCpuEnabled = true,
                IsGpuEnabled = true,
                IsMemoryEnabled = true  // Make sure memory monitoring is enabled
            };
            computer.Open();
        }

        private void SetupTimer()
        {
            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(1);
            timer.Tick += Timer_Tick;
            timer.Start();
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            UpdateHardwareInfo();
        }

        private void UpdateHardwareInfo()
        {
            computer.Hardware[0].Update();  // Update CPU info

            try
            {
                computer.Hardware.ForEach(hardware =>
                {
                    hardware.Update();

                    switch (hardware.HardwareType)
                    {
                        case HardwareType.Cpu:
                            UpdateCpuInfo(hardware);
                            break;
                        case HardwareType.GpuNvidia:
                        case HardwareType.GpuAmd:
                        case HardwareType.GpuIntel:
                            UpdateGpuInfo(hardware);
                            break;
                        case HardwareType.Memory:
                            UpdateMemoryInfo(hardware);
                            break;
                    }
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error updating hardware info: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateCpuInfo(IHardware hardware)
        {
            float? clockSpeed = null;
            float? load = null;
            float? temperature = null;
            float? power = null;

            // Get CPU brand and model
            string cpuBrandModel = hardware.Name;

            foreach (var sensor in hardware.Sensors)
            {
                if (sensor.SensorType == SensorType.Clock && sensor.Name.Contains("Core #1"))
                {
                    clockSpeed = sensor.Value;
                }
                else if (sensor.SensorType == SensorType.Load && sensor.Name.Contains("Total"))
                {
                    load = sensor.Value;
                }
                else if (sensor.SensorType == SensorType.Temperature && sensor.Name.Contains("Package"))
                {
                    temperature = sensor.Value;
                }
                else if (sensor.SensorType == SensorType.Power && sensor.Name.Contains("Package"))
                {
                    power = sensor.Value;
                }
            }

            Dispatcher.Invoke(() =>
            {
                CpuBrandText.Text = cpuBrandModel;
                CpuClockText.Text = clockSpeed.HasValue ? $"{clockSpeed:F2} MHz" : "N/A";
                CpuLoadText.Text = load.HasValue ? $"{load:F2} %" : "N/A";
                CpuTempText.Text = temperature.HasValue ? $"{temperature:F2} °C" : "N/A";
                CpuPowerText.Text = power.HasValue ? $"{power:F2} W" : "N/A";

                // Add CPU load to history and update graph
                if (load.HasValue)
                {
                    cpuLoadHistory.Add(load.Value);
                    if (cpuLoadHistory.Count > MAX_HISTORY_POINTS)
                        cpuLoadHistory.RemoveAt(0);

                    // Update graph title with latest value
                    CpuLoadGraphTitle.Text = $"CPU Load - {load:F1}%";
                    DrawGraph(CpuLoadGraph, cpuLoadHistory, Colors.Red);
                }
        
                // Add CPU temperature to history and update graph
                if (temperature.HasValue)
                {
                    cpuTempHistory.Add(temperature.Value);
                    if (cpuTempHistory.Count > MAX_HISTORY_POINTS)
                        cpuTempHistory.RemoveAt(0);
                        
                    CpuTempGraphTitle.Text = $"CPU Temperature - {temperature:F1}°C";
                    DrawTempGraph(CpuTempGraph, cpuTempHistory, Colors.OrangeRed);
                }
                
                // Add CPU power to history and update graph
                if (power.HasValue)
                {
                    cpuPowerHistory.Add(power.Value);
                    if (cpuPowerHistory.Count > MAX_HISTORY_POINTS)
                        cpuPowerHistory.RemoveAt(0);
                        
                    CpuPowerGraphTitle.Text = $"CPU Power - {power:F1}W";
                    DrawPowerGraph(CpuPowerGraph, cpuPowerHistory, Colors.Purple);
                }
            });
        }

        private void UpdateGpuInfo(IHardware hardware)
        {
            float? clockSpeed = null;
            float? load = null;
            float? temperature = null;
            float? power = null;
            float? memoryUsed = null;
            float? memoryTotal = null;
            float? memoryLoad = null;

            // Get GPU brand and model
            string gpuBrandModel = hardware.Name;

            foreach (var sensor in hardware.Sensors)
            {
                if (sensor.SensorType == SensorType.Clock && sensor.Name.Contains("Core"))
                {
                    clockSpeed = sensor.Value;
                }
                else if (sensor.SensorType == SensorType.Load && sensor.Name.Contains("Core"))
                {
                    load = sensor.Value;
                }
                else if (sensor.SensorType == SensorType.Temperature && sensor.Name.Contains("Core"))
                {
                    temperature = sensor.Value;
                }
                else if (sensor.SensorType == SensorType.Power && sensor.Name.Contains("Package"))
                {
                    power = sensor.Value;
                }
                else if (sensor.SensorType == SensorType.SmallData && sensor.Name.Contains("Memory Used"))
                {
                    memoryUsed = sensor.Value / 1024;
                }
                else if (sensor.SensorType == SensorType.SmallData && sensor.Name.Contains("Memory Total"))
                {
                    memoryTotal = sensor.Value / 1024;
                }
                else if (sensor.SensorType == SensorType.Load && sensor.Name.Contains("Memory"))
                {
                    memoryLoad = sensor.Value;
                }
            }

            Dispatcher.Invoke(() =>
            {
                GpuBrandText.Text = gpuBrandModel;
                GpuClockText.Text = clockSpeed.HasValue ? $"{clockSpeed:F2} MHz" : "N/A";
                GpuLoadText.Text = load.HasValue ? $"{load:F2} %" : "N/A";
                GpuTempText.Text = temperature.HasValue ? $"{temperature:F2} °C" : "N/A";
                GpuPowerText.Text = power.HasValue ? $"{power:F2} W" : "N/A";
                
                // Update GPU Load graph
                if (load.HasValue)
                {
                    gpuLoadHistory.Add(load.Value);
                    if (gpuLoadHistory.Count > MAX_HISTORY_POINTS)
                        gpuLoadHistory.RemoveAt(0);
                        
                    GpuLoadGraphTitle.Text = $"GPU Load - {load:F1}%";
                    DrawGraph(GpuLoadGraph, gpuLoadHistory, Colors.Orange);
                }
                
                // Add GPU temperature to history and update graph
                if (temperature.HasValue)
                {
                    gpuTempHistory.Add(temperature.Value);
                    if (gpuTempHistory.Count > MAX_HISTORY_POINTS)
                        gpuTempHistory.RemoveAt(0);
                        
                    GpuTempGraphTitle.Text = $"GPU Temperature - {temperature:F1}°C";
                    DrawTempGraph(GpuTempGraph, gpuTempHistory, Colors.Crimson);
                }
                
                // Add GPU power to history and update graph
                if (power.HasValue)
                {
                    gpuPowerHistory.Add(power.Value);
                    if (gpuPowerHistory.Count > MAX_HISTORY_POINTS)
                        gpuPowerHistory.RemoveAt(0);
                        
                    GpuPowerGraphTitle.Text = $"GPU Power - {power:F1}W";
                    DrawPowerGraph(GpuPowerGraph, gpuPowerHistory, Colors.DarkOrchid);
                }

                if (memoryUsed.HasValue && memoryTotal.HasValue)
                {
                    // Convert to GB if needed and format with 2 decimal places
                    float usedGB = memoryUsed.Value;
                    float totalGB = memoryTotal.Value;
                    float percentage = memoryLoad.HasValue ? memoryLoad.Value : (usedGB / totalGB * 100);
                    
                    GpuMemoryText.Text = $"{usedGB:F2} GB / {totalGB:F2} GB ({percentage:F1}%)";
                    
                    // Add GPU memory load to history and update graph
                    gpuMemoryHistory.Add(percentage);
                    if (gpuMemoryHistory.Count > MAX_HISTORY_POINTS)
                        gpuMemoryHistory.RemoveAt(0);
                    
                    // Update graph title with latest value
                    GpuMemoryGraphTitle.Text = $"GPU Memory Usage - {percentage:F1}%";
                    DrawGraph(GpuMemoryGraph, gpuMemoryHistory, Colors.Green);
                }
                else
                {
                    GpuMemoryText.Text = memoryLoad.HasValue ? $"{memoryLoad:F2} %" : "N/A";
                    
                    // If we only have memory load percentage
                    if (memoryLoad.HasValue)
                    {
                        gpuMemoryHistory.Add(memoryLoad.Value);
                        if (gpuMemoryHistory.Count > MAX_HISTORY_POINTS)
                            gpuMemoryHistory.RemoveAt(0);
                        
                        // Update graph title with latest value
                        GpuMemoryGraphTitle.Text = $"GPU Memory Usage - {memoryLoad:F1}%";
                        DrawGraph(GpuMemoryGraph, gpuMemoryHistory, Colors.Green);
                    }
                }
            });
        }

        private void UpdateMemoryInfo(IHardware hardware)
        {
            float? memoryLoad = null;
            float? memoryUsed = null;
            float? memoryAvailable = null;
            float? memoryTotal = null;

            foreach (var sensor in hardware.Sensors)
            {
                if (sensor.Name.Contains("Virtual"))
                {
                    continue;
                }
                if (sensor.SensorType == SensorType.Load && sensor.Name.Contains("Memory"))
                {
                    memoryLoad = sensor.Value;
                }
                else if (sensor.SensorType == SensorType.Data && sensor.Name.Contains("Used"))
                {
                    memoryUsed = sensor.Value;
                }
                else if (sensor.SensorType == SensorType.Data && sensor.Name.Contains("Available"))
                {
                    memoryAvailable = sensor.Value;
                }
                else if (sensor.SensorType == SensorType.Data && sensor.Name.Contains("Total"))
                {
                    memoryTotal = sensor.Value;
                }
            }

            Dispatcher.Invoke(() =>
            {
                try
                {
                    if (!memoryTotal.HasValue && memoryUsed.HasValue && memoryAvailable.HasValue)
                    {
                        memoryTotal = memoryUsed + memoryAvailable;
                    }
                    if (memoryUsed.HasValue && memoryTotal.HasValue)
                    {
                        // Convert to GB (values are typically in MB)
                        float usedGB = memoryUsed.Value;
                        float totalGB = memoryTotal.Value;
                        float percentage = memoryLoad.HasValue ? memoryLoad.Value : (usedGB / totalGB * 100);

                        CpuMemoryText.Text = $"{usedGB:F2} GB / {totalGB:F2} GB ({percentage:F1}%)";

                        // Add memory usage to history and update graph
                        memoryUsageHistory.Add(percentage);
                        if (memoryUsageHistory.Count > MAX_HISTORY_POINTS)
                            memoryUsageHistory.RemoveAt(0);

                        // Update graph title with latest value
                        MemoryUsageGraphTitle.Text = $"System Memory Usage - {percentage:F1}%";
                        DrawGraph(MemoryUsageGraph, memoryUsageHistory, Colors.Blue);
                    }
                    else if (memoryUsed.HasValue)
                    {
                        // If only used memory is available
                        float usedGB = memoryUsed.Value;
                        CpuMemoryText.Text = $"{usedGB:F2} GB used";
                    }
                    else if (memoryLoad.HasValue)
                    {
                        // If only percentage is available
                        CpuMemoryText.Text = $"{memoryLoad:F1}%";

                        // Add memory load to history and update graph
                        memoryUsageHistory.Add(memoryLoad.Value);
                        if (memoryUsageHistory.Count > MAX_HISTORY_POINTS)
                            memoryUsageHistory.RemoveAt(0);

                        // Update graph title with latest value
                        MemoryUsageGraphTitle.Text = $"System Memory Usage - {memoryLoad:F1}%";
                        DrawGraph(MemoryUsageGraph, memoryUsageHistory, Colors.Blue);
                    }
                    else
                    {
                        // Code commented out
                    }
                }
                catch (Exception)
                {
                    CpuMemoryText.Text = "Memory info unavailable";
                }
            });
        }

        protected override void OnClosed(EventArgs e)
        {
            timer.Stop();
            computer.Close();
            base.OnClosed(e);
        }

        private void DrawTempGraph(Canvas canvas, List<float> dataPoints, Color lineColor)
        {
            canvas.Children.Clear();
        
            if (dataPoints.Count < 2)
                return;
        
            double canvasWidth = canvas.ActualWidth;
            double canvasHeight = canvas.ActualHeight;
        
            // Find min and max temperature values in the data
            float minTemp = float.MaxValue;
            float maxTemp = float.MinValue;
            foreach (var temp in dataPoints)
            {
                if (temp < minTemp) minTemp = temp;
                if (temp > maxTemp) maxTemp = temp;
            }
            
            // Add 20% buffer to the range
            float range = maxTemp - minTemp;
            float buffer = range * 0.2f;
            
            // Set adaptive bounds with buffer (ensure minTemp doesn't go below 0)
            minTemp = Math.Max(0, minTemp - buffer);
            maxTemp = maxTemp + buffer;
            
            // Ensure we have a reasonable range (at least 10 degrees)
            if (maxTemp - minTemp < 10)
            {
                float midPoint = (maxTemp + minTemp) / 2;
                minTemp = midPoint - 5;
                maxTemp = midPoint + 5;
                minTemp = Math.Max(0, minTemp); // Ensure minTemp doesn't go below 0
            }
            
            float tempRange = maxTemp - minTemp;
        
            // Create polyline for the graph
            Polyline polyline = new Polyline();
            polyline.Stroke = new SolidColorBrush(lineColor);
            polyline.StrokeThickness = 2;
        
            // Add points to the polyline
            for (int i = 0; i < dataPoints.Count; i++)
            {
                double x = i * (canvasWidth / (MAX_HISTORY_POINTS - 1));
                // Clamp temperature to our range and scale it
                float clampedTemp = Math.Max(minTemp, Math.Min(maxTemp, dataPoints[i]));
                double y = canvasHeight - ((clampedTemp - minTemp) / tempRange * canvasHeight);
                polyline.Points.Add(new Point(x, y));
            }
        
            canvas.Children.Add(polyline);
        
            // Calculate appropriate grid line intervals based on the temperature range
            int gridStep = CalculateGridStep(tempRange);
            int startTemp = (int)Math.Ceiling(minTemp / gridStep) * gridStep;
            
            // Add horizontal grid lines at appropriate intervals
            for (int temp = startTemp; temp <= maxTemp; temp += gridStep)
            {
                double y = canvasHeight - ((temp - minTemp) / tempRange * canvasHeight);
                Line gridLine = new Line
                {
                    X1 = 0,
                    Y1 = y,
                    X2 = canvasWidth,
                    Y2 = y,
                    Stroke = new SolidColorBrush(Colors.Gray),
                    StrokeThickness = 0.5,
                    StrokeDashArray = new DoubleCollection { 4, 2 }
                };
                canvas.Children.Add(gridLine);
        
                // Add temperature label
                TextBlock label = new TextBlock
                {
                    Text = $"{temp}°C",
                    Foreground = new SolidColorBrush(Colors.White),
                    FontSize = 10
                };
                
                // Position labels on the right side of the graph
                Canvas.SetRight(label, 5);
                Canvas.SetTop(label, y - 7); // Centered vertically on the line
                canvas.Children.Add(label);
            }
            
            // Add warning threshold line at 80°C if it's within our range
            if (80 >= minTemp && 80 <= maxTemp)
            {
                double warningY = canvasHeight - ((80 - minTemp) / tempRange * canvasHeight);
                Line warningLine = new Line
                {
                    X1 = 0,
                    Y1 = warningY,
                    X2 = canvasWidth,
                    Y2 = warningY,
                    Stroke = new SolidColorBrush(Colors.Yellow),
                    StrokeThickness = 1,
                    StrokeDashArray = new DoubleCollection { 2, 2 }
                };
                canvas.Children.Add(warningLine);
            }
            
            // Add critical threshold line at 90°C if it's within our range
            if (90 >= minTemp && 90 <= maxTemp)
            {
                double criticalY = canvasHeight - ((90 - minTemp) / tempRange * canvasHeight);
                Line criticalLine = new Line
                {
                    X1 = 0,
                    Y1 = criticalY,
                    X2 = canvasWidth,
                    Y2 = criticalY,
                    Stroke = new SolidColorBrush(Colors.Red),
                    StrokeThickness = 1,
                    StrokeDashArray = new DoubleCollection { 2, 2 }
                };
                canvas.Children.Add(criticalLine);
            }
        }
        
        // Helper method to calculate appropriate grid step size
        private int CalculateGridStep(float range)
        {
            if (range <= 20) return 5;
            if (range <= 50) return 10;
            if (range <= 100) return 20;
            return 25;
        }

        private void DrawGraph(Canvas canvas, List<float> dataPoints, Color lineColor)
        {
            canvas.Children.Clear();

            if (dataPoints.Count < 2)
                return;

            double canvasWidth = canvas.ActualWidth;
            double canvasHeight = canvas.ActualHeight;

            // Find max value for scaling
            float maxValue = 0;
            foreach (var point in dataPoints)
            {
                if (point > maxValue)
                    maxValue = point;
            }

            // Ensure we have a non-zero max value for scaling
            maxValue = Math.Max(maxValue, 100);

            // Create polyline for the graph
            Polyline polyline = new Polyline();
            polyline.Stroke = new SolidColorBrush(lineColor);
            polyline.StrokeThickness = 2;

            // Add points to the polyline
            for (int i = 0; i < dataPoints.Count; i++)
            {
                double x = i * (canvasWidth / (MAX_HISTORY_POINTS - 1));
                double y = canvasHeight - (dataPoints[i] / maxValue * canvasHeight);
                polyline.Points.Add(new Point(x, y));
            }

            canvas.Children.Add(polyline);

            // Add horizontal grid lines
            for (int i = 1; i <= 4; i++)
            {
                double y = canvasHeight * i / 4;
                Line gridLine = new Line
                {
                    X1 = 0,
                    Y1 = y,
                    X2 = canvasWidth,
                    Y2 = y,
                    Stroke = new SolidColorBrush(Colors.Gray),
                    StrokeThickness = 0.5,
                    StrokeDashArray = new DoubleCollection { 4, 2 }
                };
                canvas.Children.Add(gridLine);

                // Add percentage label - modified to be on the right with white color
                TextBlock label = new TextBlock
                {
                    Text = $"{100 - (i * 25)}%",
                    Foreground = new SolidColorBrush(Colors.White),
                    FontSize = 10
                };
                
                // Position labels on the right side of the graph
                Canvas.SetRight(label, 5);
                Canvas.SetTop(label, y - 7); // Centered vertically on the line
                canvas.Children.Add(label);
            }
        }

        private void DrawPowerGraph(Canvas canvas, List<float> dataPoints, Color lineColor)
        {
            canvas.Children.Clear();
        
            if (dataPoints.Count < 2)
                return;
        
            double canvasWidth = canvas.ActualWidth;
            double canvasHeight = canvas.ActualHeight;
        
            // Find min and max power values in the data
            float minPower = float.MaxValue;
            float maxPower = float.MinValue;
            foreach (var power in dataPoints)
            {
                if (power < minPower) minPower = power;
                if (power > maxPower) maxPower = power;
            }
            
            // Add 20% buffer to the range
            float range = maxPower - minPower;
            float buffer = range * 0.2f;
            
            // Set adaptive bounds with buffer (ensure minPower doesn't go below 0)
            minPower = Math.Max(0, minPower - buffer);
            maxPower = maxPower + buffer;
            
            // Ensure we have a reasonable range (at least 5 watts)
            if (maxPower - minPower < 5)
            {
                float midPoint = (maxPower + minPower) / 2;
                minPower = Math.Max(0, midPoint - 2.5f);
                maxPower = midPoint + 2.5f;
            }
            
            float powerRange = maxPower - minPower;
        
            // Create polyline for the graph
            Polyline polyline = new Polyline();
            polyline.Stroke = new SolidColorBrush(lineColor);
            polyline.StrokeThickness = 2;
        
            // Add points to the polyline
            for (int i = 0; i < dataPoints.Count; i++)
            {
                double x = i * (canvasWidth / (MAX_HISTORY_POINTS - 1));
                // Clamp power to our range and scale it
                float clampedPower = Math.Max(minPower, Math.Min(maxPower, dataPoints[i]));
                double y = canvasHeight - ((clampedPower - minPower) / powerRange * canvasHeight);
                polyline.Points.Add(new Point(x, y));
            }
        
            canvas.Children.Add(polyline);
        
            // Calculate appropriate grid line intervals based on the power range
            int gridStep = CalculatePowerGridStep(powerRange);
            int startPower = (int)Math.Ceiling(minPower / gridStep) * gridStep;
            
            // Add horizontal grid lines at appropriate intervals
            for (int power = startPower; power <= maxPower; power += gridStep)
            {
                double y = canvasHeight - ((power - minPower) / powerRange * canvasHeight);
                Line gridLine = new Line
                {
                    X1 = 0,
                    Y1 = y,
                    X2 = canvasWidth,
                    Y2 = y,
                    Stroke = new SolidColorBrush(Colors.Gray),
                    StrokeThickness = 0.5,
                    StrokeDashArray = new DoubleCollection { 4, 2 }
                };
                canvas.Children.Add(gridLine);
        
                // Add power label
                TextBlock label = new TextBlock
                {
                    Text = $"{power}W",
                    Foreground = new SolidColorBrush(Colors.White),
                    FontSize = 10
                };
                
                // Position labels on the right side of the graph
                Canvas.SetRight(label, 5);
                Canvas.SetTop(label, y - 7); // Centered vertically on the line
                canvas.Children.Add(label);
            }
        }
        
        // Helper method to calculate appropriate grid step size for power
        private int CalculatePowerGridStep(float range)
        {
            if (range <= 10) return 2;
            if (range <= 25) return 5;
            if (range <= 50) return 10;
            if (range <= 100) return 20;
            return 50;
        }

        // Remove this duplicate DrawGraph method (lines 686-736)
        // The original DrawGraph method at line 586 will be kept
    }

    public static class Extensions
    {
        public static void ForEach<T>(this IEnumerable<T> enumerable, Action<T> action)
        {
            foreach (var item in enumerable)
            {
                action(item);
            }
        }
    }
}