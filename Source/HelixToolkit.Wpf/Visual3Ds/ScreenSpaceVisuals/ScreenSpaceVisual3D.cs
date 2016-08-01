// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ScreenSpaceVisual3D.cs" company="Helix Toolkit">
//   Copyright (c) 2014 Helix Toolkit contributors
// </copyright>
// <summary>
//   An abstract base class for visuals that use screen space dimensions when rendering.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace HelixToolkit.Wpf
{
    using System;
    using System.Windows;
    using System.Windows.Media;
    using System.Windows.Media.Media3D;

    /// <summary>
    /// An abstract base class for visuals that use screen space dimensions when rendering.
    /// </summary>
    public abstract class ScreenSpaceVisual3D : RenderingModelVisual3D
    {
        /// <summary>
        /// Identifies the <see cref="EmissiveBrush"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty EmissiveBrushProperty = DependencyProperty.Register(
            "EmissiveBrush", typeof(Brush), typeof(ScreenSpaceVisual3D), new UIPropertyMetadata(Brushes.Black, BrushChanged));

        /// <summary>
        /// Identifies the <see cref="DiffuseBrush"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty DiffuseBrushProperty = DependencyProperty.Register(
            "DiffuseBrush", typeof(Brush), typeof(ScreenSpaceVisual3D), new UIPropertyMetadata(Brushes.Black, BrushChanged));

        /// <summary>
        /// Identifies the <see cref="DepthOffset"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty DepthOffsetProperty = DependencyProperty.Register(
            "DepthOffset", typeof(double), typeof(ScreenSpaceVisual3D), new UIPropertyMetadata(0.0, GeometryChanged));

        /// <summary>
        /// Identifies the <see cref="Points"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty PointsProperty = DependencyProperty.Register(
            "Points", typeof(Point3DCollection), typeof(ScreenSpaceVisual3D), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsMeasure, PointsChanged));

        /// <summary>
        /// The is rendering flag.
        /// </summary>
        private bool isRendering;

        /// <summary>
        /// The listening to collection
        /// </summary>
        private Point3DCollection collectionBeingListenedTo;

        /// <summary>
        /// Initializes a new instance of the <see cref = "ScreenSpaceVisual3D" /> class.
        /// </summary>
        protected ScreenSpaceVisual3D()
        {
            this.Mesh = new MeshGeometry3D();
            this.Model = new GeometryModel3D { Geometry = this.Mesh };
            this.Content = this.Model;
            this.Points = new Point3DCollection();
            this.BrushChanged();
        }

        /// <summary>
        /// Gets or sets the brush applied to the emissive material.
        /// </summary>
        /// <value>
        /// The brush.
        /// </value>
        public Brush EmissiveBrush
        {
            get
            {
                return (Brush)this.GetValue(EmissiveBrushProperty);
            }

            set
            {
                this.SetValue(EmissiveBrushProperty, value);
            }
        }

        /// <summary>
        /// Gets or sets the brush applied to the diffuse material.
        /// </summary>
        /// <value>
        /// The brush.
        /// </value>
        public Brush DiffuseBrush
        {
            get
            {
                return (Brush)this.GetValue(DiffuseBrushProperty);
            }

            set
            {
                this.SetValue(DiffuseBrushProperty, value);
            }
        }

        /// <summary>
        /// Gets or sets the depth offset.
        /// A small positive number (0.0001) will move the visual slightly in front of other objects.
        /// </summary>
        /// <value>
        /// The depth offset.
        /// </value>
        public double DepthOffset
        {
            get
            {
                return (double)this.GetValue(DepthOffsetProperty);
            }

            set
            {
                this.SetValue(DepthOffsetProperty, value);
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is being rendered.
        /// When the visual is removed from the visual tree, this property should be set to false.
        /// </summary>
        public bool IsRendering
        {
            get
            {
                return this.isRendering;
            }

            set
            {
                if (value != this.isRendering)
                {
                    this.isRendering = value;
                    if (this.isRendering)
                    {
                        this.SubscribeToRenderingEvent();
                    }
                    else
                    {
                        this.UnsubscribeRenderingEvent();
                    }
                }
            }
        }

        /// <summary>
        /// Gets or sets the points collection.
        /// </summary>
        /// <value>
        /// The points collection.
        /// </value>
        public Point3DCollection Points
        {
            get
            {
                return (Point3DCollection)this.GetValue(PointsProperty);
            }

            set
            {
                this.SetValue(PointsProperty, value);
            }
        }

        /// <summary>
        /// Gets or sets the clipping object.
        /// </summary>
        protected CohenSutherlandClipping Clipping { get; set; }

        /// <summary>
        /// Gets or sets the mesh.
        /// </summary>
        protected MeshGeometry3D Mesh { get; set; }

        /// <summary>
        /// Gets or sets the model.
        /// </summary>
        protected GeometryModel3D Model { get; set; }

        /// <summary>
        /// Called when geometry properties have changed.
        /// </summary>
        /// <param name="sender">
        /// The sender.
        /// </param>
        /// <param name="e">
        /// The <see cref="DependencyPropertyChangedEventArgs"/> instance containing the event data.
        /// </param>
        protected static void GeometryChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            ((ScreenSpaceVisual3D)sender).UpdateGeometry();
        }

        /// <summary>
        /// Called when points have changed.
        /// </summary>
        /// <param name="sender">
        /// The sender.
        /// </param>
        /// <param name="e">
        /// The <see cref="DependencyPropertyChangedEventArgs"/> instance containing the event data.
        /// </param>
        private static void PointsChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var screenSpaceVisual3D = (ScreenSpaceVisual3D)sender;
            screenSpaceVisual3D.UpdateGeometry();

            if (screenSpaceVisual3D.collectionBeingListenedTo != null && !screenSpaceVisual3D.collectionBeingListenedTo.IsFrozen)
            {
                screenSpaceVisual3D.collectionBeingListenedTo.Changed -= screenSpaceVisual3D.HandlePointsChanged;
            }

            var pc = e.NewValue as Point3DCollection;
            if (pc != null && !pc.IsFrozen)
            {
                screenSpaceVisual3D.collectionBeingListenedTo = pc;

                // TODO: use a weak event manager
                pc.Changed += screenSpaceVisual3D.HandlePointsChanged;
            }
            else
            {
                screenSpaceVisual3D.collectionBeingListenedTo = pc;
            }
        }

        /// <summary>
        /// The composition target_ rendering.
        /// </summary>
        /// <param name="sender">
        /// The sender.
        /// </param>
        /// <param name="e">
        /// The event arguments.
        /// </param>
        protected override void OnCompositionTargetRendering(object sender, RenderingEventArgs e)
        {
            if (this.isRendering)
            {
                if (!Visual3DHelper.IsAttachedToViewport3D(this))
                {
                    return;
                }

                if (this.UpdateTransforms())
                {
                    this.UpdateClipping();
                    this.UpdateGeometry();
                }
            }
        }

        /// <summary>
        /// Called when the parent of the 3-D visual object is changed.
        /// </summary>
        /// <param name="oldParent">
        /// A value of type <see cref="T:System.Windows.DependencyObject"/> that represents the previous parent of the <see cref="T:System.Windows.Media.Media3D.Visual3D"/> object. If the <see cref="T:System.Windows.Media.Media3D.Visual3D"/> object did not have a previous parent, the value of the parameter is null.
        /// </param>
        protected override void OnVisualParentChanged(DependencyObject oldParent)
        {
            base.OnVisualParentChanged(oldParent);
            var parent = VisualTreeHelper.GetParent(this);
            this.IsRendering = parent != null;
        }

        /// <summary>
        /// Updates the geometry.
        /// </summary>
        protected abstract void UpdateGeometry();

        /// <summary>
        /// Updates the transforms.
        /// </summary>
        /// <returns>
        /// True if the transform is updated.
        /// </returns>
        protected abstract bool UpdateTransforms();

        /// <summary>
        /// Changes the material when the color changed.
        /// </summary>
        /// <param name="sender">
        /// The sender.
        /// </param>
        /// <param name="e">
        /// The <see cref="DependencyPropertyChangedEventArgs"/> instance containing the event data.
        /// </param>
        private static void BrushChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            ((ScreenSpaceVisual3D)sender).BrushChanged();
        }

        /// <summary>
        /// Handles changes in the <see cref="Points" /> collection.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The event arguments.</param>
        private void HandlePointsChanged(object sender, EventArgs e)
        {
            this.UpdateGeometry();
        }

        /// <summary>
        /// Changes the material when the color changed.
        /// </summary>
        private void BrushChanged()
        {
            var mg = new MaterialGroup();
            Brush db = this.DiffuseBrush;
            if (db != null)
            {
                mg.Children.Add(new DiffuseMaterial(db));
            }
            Brush eb = this.EmissiveBrush;
            if (eb != null)
            {
                mg.Children.Add(new EmissiveMaterial(eb));
            }
            mg.Freeze();
            this.Model.Material = mg;
        }

        /// <summary>
        /// Updates the clipping object.
        /// </summary>
        private void UpdateClipping()
        {
            var vp = this.GetViewport3D();
            if (vp == null)
            {
                return;
            }

            this.Clipping = new CohenSutherlandClipping(10, vp.ActualWidth - 20, 10, vp.ActualHeight - 20);
        }
    }
}