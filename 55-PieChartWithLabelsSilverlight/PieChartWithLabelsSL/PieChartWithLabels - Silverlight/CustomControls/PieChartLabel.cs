﻿using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows.Controls.DataVisualization.Charting;
using System.Windows.Data;
using System.Linq;

namespace CustomControls
{
    /// <summary>
    /// This is the control that holds the label. It includes an optional line connecting it to the chart, and whatever
    /// content the developer specifies in the DataTemplate.
    /// </summary>
    [TemplatePart(Name = "Canvas_PART", Type = typeof(Canvas))]
    [TemplatePart(Name = "Content_PART", Type = typeof(FrameworkElement))]
    [TemplatePart(Name = "Polyline_PART", Type = typeof(Polyline))]
    public class PieChartLabel : ContentControl
    {
        private FrameworkElement contentPart;
        private Canvas canvasPart;
        private Polyline polylinePart;
        private Point center;
        private Point arcMidpoint;

        public bool IsArcSmall { get; set; }

        public PieChartLabel()
        {
            this.DefaultStyleKey = typeof(PieChartLabel);
        }

        //static PieChartLabel()
        //{
        //    DefaultStyleKeyProperty.OverrideMetadata(typeof(PieChartLabel), new PropertyMetadata(typeof(PieChartLabel)));
        //}

        /// <summary>
        /// FormattedRation DP - When a PieChartLabel is created, this property is data bound to the PieDataPoint's equivalent
        /// property. It will typically be used within the DataTemplate for PieChartLabel.
        /// </summary>
        public string FormattedRatio
        {
            get { return (string)this.GetValue(FormattedRatioProperty); }
            set { this.SetValue(FormattedRatioProperty, value); }
        }

        //public static readonly DependencyProperty FormattedRatioProperty = PieDataPoint.FormattedRatioProperty.AddOwner(typeof(PieChartLabel), null);

        public static readonly DependencyProperty FormattedRatioProperty =
            DependencyProperty.Register("FormattedRatio", typeof(string), typeof(PieChartLabel), new PropertyMetadata(String.Empty));

        /// <summary>
        /// Geometry DP - When a PieChartLabel is created, this property is data bound to the PieDataPoint's equivalent
        /// property. We need this information to calculate the position of the label.
        /// </summary>
        public Geometry Geometry
        {
            get { return (Geometry)this.GetValue(GeometryProperty); }
            set { this.SetValue(GeometryProperty, value); }
        }


        public static readonly DependencyProperty GeometryProperty =
            DependencyProperty.Register("Geometry", typeof(Geometry), typeof(PieChartLabel), new PropertyMetadata(null, GeometryPropertyChanged));

        //public static readonly DependencyProperty GeometryProperty = PieDataPoint.GeometryProperty.AddOwner(typeof(PieChartLabel), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsParentMeasure | FrameworkPropertyMetadataOptions.AffectsArrange, GeometryPropertyChanged));

        private static void GeometryPropertyChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            PieChartLabel label = obj as PieChartLabel;
            if (label != null)
            {
                bool isArcSmall;
                PieChartHelper.GetPieChartInfo(e.NewValue as Geometry, out label.center, out label.arcMidpoint, out isArcSmall);
                label.IsArcSmall = isArcSmall;

                label.InvalidateArrange();
                FrameworkElement fe = label.Parent as FrameworkElement;
                if (fe != null)
                {
                    fe.InvalidateMeasure();
                }
            }
        }

        /// <summary>
        /// DisplayMode DP - The developer can set this DP to control how the labels should be displayed.
        /// This property can have one of the following values:
        /// ArcMidpoint - Center of the label is positioned at the arc midpoint.
        /// Connected - A line connecting the arc midpoint and label is displayed.
        /// Auto - If at least one pie slice is very small, all pie slices use the Connected display mode. Otherwise, they all use ArcMidpoint.
        /// AutoMixed - Connected display mode is used for all small pie slices, and ArcMidpoint is used for all other slices.
        /// </summary>
        public DisplayMode DisplayMode
        {
            get { return (DisplayMode)this.GetValue(DisplayModeProperty); }
            set { this.SetValue(DisplayModeProperty, value); }
        }

        public static readonly DependencyProperty DisplayModeProperty =
            DependencyProperty.Register("DisplayMode", typeof(DisplayMode), typeof(PieChartLabel), new PropertyMetadata(DisplayMode.ArcMidpoint, DisplayModePropertyChanged));
            //DependencyProperty.Register("DisplayMode", typeof(DisplayMode), typeof(PieChartLabel), new FrameworkPropertyMetadata(DisplayMode.ArcMidpoint, FrameworkPropertyMetadataOptions.AffectsArrange));

        private static void DisplayModePropertyChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            PieChartLabel label = obj as PieChartLabel;
            if (label != null)
            {
                label.InvalidateArrange();
            }            
        }

        /// <summary>
        /// LineStroke DP - Determines the brush of the line when in connected mode.
        /// </summary>
        public Brush LineStroke
        {
            get { return (Brush)this.GetValue(LineStrokeProperty); }
            set { this.SetValue(LineStrokeProperty, value); }
        }

        public static readonly DependencyProperty LineStrokeProperty =
            DependencyProperty.Register("LineStroke", typeof(Brush), typeof(PieChartLabel), new PropertyMetadata(new SolidColorBrush(Colors.Gray)));

        /// <summary>
        /// LineStrokeThickness DP - Determines the thickness of the line when in connected mode.
        /// </summary>
        public double LineStrokeThickness
        {
            get { return (double)this.GetValue(LineStrokeThicknessProperty); }
            set { this.SetValue(LineStrokeThicknessProperty, value); }
        }

        public static readonly DependencyProperty LineStrokeThicknessProperty =
            DependencyProperty.Register("LineStrokeThickness", typeof(double), typeof(PieChartLabel), new PropertyMetadata(1.0));

        /// <summary>
        /// On arrange, re-position the label.
        /// </summary>
        protected override Size ArrangeOverride(Size arrangeBounds)
        {
            this.PositionLabel();
            return base.ArrangeOverride(arrangeBounds);
        }

        /// <summary>
        /// When the template is applied, get the template parts. Also, ensure that if the 
        /// content of the label changes, the label is re-positioned.
        /// </summary>
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            this.contentPart = this.GetTemplateChild("Content_PART") as FrameworkElement;
			this.canvasPart = this.GetTemplateChild("Canvas_PART") as Canvas;
			this.polylinePart = this.GetTemplateChild("Polyline_PART") as Polyline;
        }

        /// <summary>
        /// Positions the label depending on the display mode specified by the developer.
        /// </summary>
        public void PositionLabel()
        {
            switch (this.DisplayMode)
            {
                case DisplayMode.ArcMidpoint:
                    this.PositionArcMidpoint();
					break;
                case DisplayMode.Connected:
                    this.PositionConnected();
					break;
                case DisplayMode.AutoMixed:
                    this.PositionAutoMixed();
					break;
                case DisplayMode.Auto:
                    this.PositionAuto();
					break;
            }
        }

        /// <summary>
        /// Positions the label with its center in the same location as the midpoint of the pie slice arc.
        /// </summary>
        /// <returns>Return the top and left position of the label in the label area panel.</returns>
        private void PositionArcMidpoint()
        {
            //this.RemovePolyline();
            this.SetPolylineVisibility(Visibility.Collapsed);

            if (this.contentPart != null)
            {
                Canvas.SetTop(this.contentPart, this.arcMidpoint.Y - 0.5 * this.contentPart.DesiredSize.Height);
                Canvas.SetLeft(this.contentPart, this.arcMidpoint.X - 0.5 * this.contentPart.DesiredSize.Width);
            }
        }

        /// <summary>
        /// Adds a line that connects the arc midpoint to the label and positions the label appropriately.
        /// Ideally, I would add the Polyline in the template and I would only change its points here. Unfortunately,
        /// because of a WPF bug, the Polyline doesn't render in certain corner-case scenarios. As a workaround,
        /// I create a new Polyline everytime the label is positioned, as can be seen in this method.
        /// </summary>
        /// <returns>Return the top and left position of the label in the label area panel.</returns>
        private void PositionConnected()
        {
            //this.RemovePolyline();
            this.SetPolylineVisibility(Visibility.Visible);
			
			if (this.contentPart != null && this.polylinePart != null)
            {
                PointCollection newPoints = new PointCollection();

                // First point
                newPoints.Add(this.SnapPoint(this.arcMidpoint));

                // Second point
                Vector radialDirection = Vector.Subtract(this.arcMidpoint, this.center);
                radialDirection.Normalize();
                Point secondPoint = this.arcMidpoint + (radialDirection * 10);
                newPoints.Add(this.SnapPoint(secondPoint));

                // Third point
                int sign = Math.Sign(radialDirection.X); // 1 if label is on the right side, -1 if it's on the left.
                Point thirdPoint = secondPoint + new Vector(sign * 20, 0);
                newPoints.Add(this.SnapPoint(thirdPoint));

				this.polylinePart.Points = newPoints;

                double contentX = (sign == 1) ? thirdPoint.X : thirdPoint.X - this.contentPart.DesiredSize.Width;
                double contentY = thirdPoint.Y - 0.5 * this.contentPart.DesiredSize.Height;
                Canvas.SetTop(this.contentPart, contentY);
                Canvas.SetLeft(this.contentPart, contentX);

                //Polyline polyline = new Polyline();
                //polyline.Points = newPoints;
                //polyline.SetBinding(Polyline.StrokeThicknessProperty, new Binding("LineStrokeThickness") { Source = this });
                //polyline.SetBinding(Polyline.StrokeProperty, new Binding("LineStroke") { Source = this });
                //polyline.StrokeLineJoin = PenLineJoin.Round;

                //this.canvasPart.Children.Add(polyline);
            }
        }

		/// <summary>
		/// Sets the polyline's visibility, if it exists.
		/// </summary>
		/// <param name="visibility">The visibility to use.</param>
        private void SetPolylineVisibility(Visibility visibility)
        {
			if (this.polylinePart != null)
			{
				this.polylinePart.Visibility = visibility;
            }
        }

        ///// <summary>
        ///// Removes the Polyline from the canvas part.
        ///// </summary>
        //private void RemovePolyline()
        //{
        //    if (this.canvasPart != null)
        //    {
        //        Polyline polyline = canvasPart.Children.OfType<Polyline>().FirstOrDefault();
        //        if (polyline != null)
        //        {
        //            canvasPart.Children.Remove(polyline);
        //        }
        //    }
        //}

        /// <summary>
        /// Positions the label in the arc midpoint if it's big enough, and displays it connected by a line if it's
        /// small.
        /// </summary>
        /// <returns>Return the top and left position of the label in the label area panel.</returns>
        private void PositionAutoMixed()
        {
            if (this.IsArcSmall)
            {
                this.PositionConnected();
            }
            else
            {
                this.PositionArcMidpoint();
            }
        }

        /// <summary>
        /// If at least one arc is small, all labels are displayed connected by a line. Otherwise, they're positioned
        /// in the arc midpoint.
        /// </summary>
        /// <returns>Return the top and left position of the label in the label area panel.</returns>
        private void PositionAuto()
        {
            Chart chart = TreeHelper.FindAncestor<Chart>(this);
            if (chart != null)
            {
                //PieChartLabelArea labelArea = chart.Template.FindName("LabelArea_PART", chart) as PieChartLabelArea;
                PieChartLabelArea labelArea = TreeHelper.FindDescendent(chart, "LabelArea_PART") as PieChartLabelArea;
                if (labelArea != null && labelArea.HasSmallArc)
                {
                    this.PositionConnected();
                }
                else
                {
                    this.PositionArcMidpoint();
                }
            }
        }

        /// <summary>
        /// Ensures that the line connecting the label is positioned in such a way that whole pixels are used to render
        /// it. This calculation depends on whether the thickness of the line is odd or even.
        /// </summary>
        /// <param name="point">A point belonging to the polyline.</param>
        /// <returns>The point that will cause the line to snap to pixels.</returns>
        private Point SnapPoint(Point point)
        {
            double lineThickness = this.LineStrokeThickness;
            int intLineThickness = (int)lineThickness;
            if (lineThickness == intLineThickness)
            {
                if ((intLineThickness % 2) == 1)
                {
                    return new Point(Math.Floor(point.X) + 0.5, Math.Floor(point.Y) + 0.5);
                }
                else
                {
                    return new Point(Math.Round(point.X), Math.Round(point.Y));
                }
            }
            return point;
        }

        /// <summary>
        /// When a label is clicked, the corresponding PieDataPoint becomes selected.
        /// This enables master-detail scenario driven by clicking on labels.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            Chart chart = TreeHelper.FindAncestor<Chart>(this);
            if (chart != null)
            {
                PieSeries pieSeries = chart.Series.OfType<PieSeries>().FirstOrDefault();
                if (pieSeries != null)
                {
                    pieSeries.SelectedItem = this.Content;
                }
            }
        }
    }

    /// <summary>
    /// Modes used to control how the labels are positioned.
    /// </summary>
    public enum DisplayMode
    {
        ArcMidpoint,    // Center of the label is positioned at the arc midpoint.
        Connected,      // A line connecting the arc midpoint and label is displayed.
        Auto,           // If at least one pie slice is very small, all pie slices use the Connected display mode. Otherwise, they all use ArcMidpoint.
        AutoMixed       // Connected display mode is used for all small pie slices, and ArcMidpoint is used for all other slices.
    }
}
