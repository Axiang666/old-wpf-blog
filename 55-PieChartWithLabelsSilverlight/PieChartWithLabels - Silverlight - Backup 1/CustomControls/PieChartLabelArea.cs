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
using System.Linq;

namespace CustomControls
{
    /// <summary>
    /// Canvas where all labels (of type PieChartLabel) will be added. We need a separate
    /// canvas to avoid z-order issues. This prevents having a pie slice overlap a label.
    /// </summary>
    public class PieChartLabelArea : Panel
    {
        /// <summary>
        /// HasSmallArc DP - If at least one slice of the pie chart is small, this property is set
        /// to true. This is used in the "Auto" mode. When in this mode, if this property is set to
        /// true, all labels will be connected to the chart by a line.
        /// </summary>
        public bool HasSmallArc
        {
            get { return (bool)this.GetValue(HasSmallArcProperty); }
            set { this.SetValue(HasSmallArcProperty, value); }
        }

        public static readonly DependencyProperty HasSmallArcProperty =
            DependencyProperty.Register("HasSmallArc", typeof(bool), typeof(PieChartLabelArea), new PropertyMetadata(false, HasSmallArcPropertyChanged));

        /// <summary>
        /// When the HasSmallArc DO is set, all labels should be re-arranged.
        /// </summary>
        private static void HasSmallArcPropertyChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            PieChartLabelArea labelArea = obj as PieChartLabelArea;
            if (labelArea != null)
            {
                foreach (PieChartLabel label in labelArea.Children.OfType<PieChartLabel>())
                {
                    label.InvalidateArrange();
                }
            }
        }

        /// <summary>
        /// On measure, we check if there is at least one child of the canvas that is small, and we set
        /// HasSmallArc to reflect that information. 
        /// Setting HasSmallArc ends up causing all labels to be re-arranged taking this information
        /// into consideration.
        /// </summary>
        protected override Size MeasureOverride(Size constraint)
        {
            bool hasSmallArc = false;
            foreach (PieChartLabel label in this.Children.OfType<PieChartLabel>())
            {
                if (label.IsArcSmall)
                {
                    hasSmallArc = true;
                    break;
                }
            }
            HasSmallArc = hasSmallArc;

            return base.MeasureOverride(constraint);
        }

        /// <summary>
        /// On arrange, position all labels in the desired location.
        /// </summary>
        /// <param name="finalSize">Panel's final size.</param>
        /// <returns>Panel's final size.</returns>
        protected override Size ArrangeOverride(Size finalSize)
        {
            foreach (PieChartLabel label in this.Children.OfType<PieChartLabel>())
            {
                Point position = label.PositionLabel();
                label.Arrange(new Rect(position, label.DesiredSize));
            }

            return finalSize;
        }
    }
}
