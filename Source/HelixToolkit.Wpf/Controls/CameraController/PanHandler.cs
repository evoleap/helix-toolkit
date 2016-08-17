// --------------------------------------------------------------------------------------------------------------------
// <copyright file="PanHandler.cs" company="Helix Toolkit">
//   Copyright (c) 2014 Helix Toolkit contributors
// </copyright>
// <summary>
//   Handles panning.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace HelixToolkit.Wpf
{
    using System;
    using System.Collections.Generic;
    using System.Windows;
    using System.Windows.Input;
    using System.Windows.Media.Media3D;

    /// <summary>
    /// Handles panning.
    /// </summary>
    internal class PanHandler : MouseGestureHandler
    {
        /// <summary>
        /// The 3D pan origin.
        /// </summary>
        private Point3D panPoint3D;

        /// <summary>
        /// Whether this handler changes the camera path offset.
        /// </summary>
        private bool changePathOffset;

        /// <summary>
        /// Gets or sets the camera path offset.
        /// </summary>
        protected Vector3D CameraPathOffset
        {
            get { return this.Controller.CameraPathOffset; }
            set { this.Controller.CameraPathOffset = value; }
        }

        protected Point3D CameraPlanePoint
        {
            get { return this.Controller.CameraPlanePoint; }
        }

        protected Vector3D CameraPlaneNormal
        {
            get { return this.Controller.CameraPlaneNormal; }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PanHandler"/> class.
        /// </summary>
        /// <param name="controller">
        /// The controller.
        /// </param>
        /// <param name="changePathOffset">Whether this handler changes the path offset</param>
        public PanHandler(CameraController controller, bool changePathOffset = false)
            : base(controller)
        {
            this.changePathOffset = changePathOffset;
        }

        /// <summary>
        /// Occurs when the position is changed during a manipulation.
        /// </summary>
        /// <param name="e">The <see cref="ManipulationEventArgs"/> instance containing the event data.</param>
        public override void Delta(ManipulationEventArgs e)
        {
            base.Delta(e);
            var thisPoint3D = this.UnProject(e.CurrentPosition, this.panPoint3D, this.Controller.CameraLookDirection);

            if (this.LastPoint3D == null || thisPoint3D == null)
            {
                return;
            }

            Vector3D delta3D = this.LastPoint3D.Value - thisPoint3D.Value;
            this.Pan(delta3D);

            this.LastPoint = e.CurrentPosition;
            this.LastPoint3D = this.UnProject(e.CurrentPosition, this.panPoint3D, this.Controller.CameraLookDirection);
        }

        /// <summary>
        /// Pans the camera by the specified 3D vector (world coordinates).
        /// </summary>
        /// <param name="delta">
        /// The panning vector.
        /// </param>
        public void Pan(Vector3D delta)
        {
            if (!this.Controller.IsPanEnabled)
            {
                return;
            }

            if (this.CameraMode == CameraMode.FixedPosition)
            {
                return;
            }

            if (this.CameraMode == CameraMode.InspectPath)
            {
                if (this.changePathOffset)
                {
                    this.CameraPathOffset -= delta;
                    Controller.ShowTargetAdorner(this.Project(CameraTarget + CameraPathOffset));
                    return;
                }
                else
                {
                    if (PanAlongPath(delta))
                    {
                        Controller.ShowTargetAdorner(this.Project(CameraTarget + CameraPathOffset));
                        return;
                    }
                }
            }

            if (this.CameraMode == CameraMode.InspectPlane)
            {
                PanAlongPlane(delta);
                return;
            }

            this.CameraPosition += delta;
        }

        private bool PanAlongPath(Vector3D delta)
        {
            var path = this.CameraPath;
            if (path == null)
                return false;
            return PanAlongPath(delta, path);
        }
        private bool PanAlongPath(Vector3D delta, Point3DCollection path)
        {
            var start = this.CameraTarget + CameraPathOffset;
            var end = start + delta;

            var mouseSegment = GetSegmentNearestPoint(path, end);
            if (!mouseSegment.HasValue)
                return false;

            // The new delta has the same distance, but a different direction, without going outside of segment bounds.
            var newDelta = mouseSegment.Value.NearestPoint - start;
            if (newDelta.Length > 0)
            {
                newDelta.Normalize();
                newDelta *= delta.Length;
                var newEnd = ConstrainToSegment(mouseSegment.Value.Segment, start + newDelta);
                newDelta = newEnd - start;
                this.CameraPosition += newDelta;
            }

            return true;
        }
        private Point3D ConstrainToSegment(PathSegment segment, Point3D point)
        {
            return GetNearestPointInSegment(segment, point);
        }
        private PathSegmentPointDistance? GetSegmentNearestPoint(Point3DCollection path, Point3D point)
        {
            PathSegmentPointDistance? result = null;
            foreach (var p in EnumerateNearestPoints(path, point))
            {
                if (!result.HasValue || p.DistanceSquared < result.Value.DistanceSquared)
                    result = p;
            }
            return result;
        }
        private IEnumerable<PathSegmentPointDistance> EnumerateNearestPoints(Point3DCollection path, Point3D point)
        {
            foreach (var seg in EnumerateSegments(path))
            {
                if (seg.Start != seg.End)
                {
                    var nearestPoint = GetNearestPointInSegment(seg, point);
                    yield return new PathSegmentPointDistance()
                    {
                        Segment = seg,
                        NearestPoint = nearestPoint,
                        DistanceSquared = (nearestPoint - point).LengthSquared
                    };
                }
            }
        }
        private IEnumerable<PathSegment> EnumerateSegments(Point3DCollection path)
        {
            for (int i = 0; i < path.Count; i += 2)
                yield return new PathSegment() { Start = path[i], End = path[i + 1] };
        }
        private Point3D GetNearestPointInSegment(PathSegment segment, Point3D point)
        {
            Vector3D x10 = segment.Start - point;
            Vector3D x21 = segment.End - segment.Start;
            // http://mathworld.wolfram.com/Point-LineDistance3-Dimensional.html
            double t = -Vector3D.DotProduct(x10, x21) / x21.LengthSquared;
            t = Math.Max(0, Math.Min(1, t));
            return segment.Start + x21 * t;
        }
        private struct PathSegment
        {
            public Point3D Start;
            public Point3D End;
            public Vector3D Vector => End - Start;
        }
        private struct PathSegmentPointDistance
        {
            public PathSegment Segment;
            public Point3D NearestPoint;
            public double DistanceSquared;
        }

        private void PanAlongPlane(Vector3D delta)
        {
            if (delta.Length == 0)
                return;

            var plane = new Plane3D(CameraPlanePoint, CameraPlaneNormal);
            var startPosition = this.CameraPosition;
            var cameraLookDir = this.CameraLookDirection;
            var moveOrigin = plane.LineIntersection(startPosition, startPosition + cameraLookDir);
            if (!moveOrigin.HasValue)
                return;

            var endPosition = this.CameraPosition + delta;
            var moveFinal = plane.LineIntersection(endPosition, endPosition + cameraLookDir);
            if (!moveFinal.HasValue)
                return;

            var moveLineDir = moveFinal.Value - moveOrigin.Value;
            PanAlongLine(delta, moveOrigin.Value, moveLineDir);
        }
        private void PanAlongLine(Vector3D delta, Point3D pointOnLine, Vector3D lineDirection)
        {
            var start = this.CameraTarget + CameraPathOffset;
            var end = start + delta;

            var mouseSegment = GetNearestPointOnLine(end, pointOnLine, lineDirection);

            // The new delta has the same distance, but a different direction, without going outside of segment bounds.
            var newDelta = mouseSegment.NearestPoint - start;
            if (newDelta.Length > 0)
            {
                newDelta.Normalize();
                newDelta *= delta.Length;
                this.CameraPosition += newDelta;
            }
        }
        private PathSegmentPointDistance GetNearestPointOnLine(Point3D point, Point3D pointOnLine, Vector3D lineDirection)
        {
            var seg = new PathSegment()
            {
                Start = pointOnLine,
                End = pointOnLine + lineDirection
            };
            var nearestPoint = GetNearestPointInSegment(seg, point);
            return new PathSegmentPointDistance()
            {
                Segment = seg,
                NearestPoint = nearestPoint,
                DistanceSquared = (nearestPoint - point).LengthSquared
            };
        }

        /// <summary>
        /// Pans the camera by the specified 2D vector (screen coordinates).
        /// </summary>
        /// <param name="delta">
        /// The delta.
        /// </param>
        public void Pan(Vector delta)
        {
            var mousePoint = this.LastPoint + delta;

            var thisPoint3D = this.UnProject(mousePoint, this.panPoint3D, this.Controller.CameraLookDirection);

            if (this.LastPoint3D == null || thisPoint3D == null)
            {
                return;
            }

            Vector3D delta3D = this.LastPoint3D.Value - thisPoint3D.Value;
            this.Pan(delta3D);

            this.LastPoint3D = this.UnProject(mousePoint, this.panPoint3D, this.Controller.CameraLookDirection);

            this.LastPoint = mousePoint;
        }

        /// <summary>
        /// Occurs when the manipulation is started.
        /// </summary>
        /// <param name="e">The <see cref="ManipulationEventArgs"/> instance containing the event data.</param>
        public override void Started(ManipulationEventArgs e)
        {
            base.Started(e);
            this.panPoint3D = this.Controller.CameraTarget;
            if (this.MouseDownNearestPoint3D != null)
            {
                this.panPoint3D = this.MouseDownNearestPoint3D.Value;
            }

            this.LastPoint3D = this.UnProject(this.MouseDownPoint, this.panPoint3D, this.Controller.CameraLookDirection);
        }

        /// <summary>
        /// Occurs when the command associated with this handler initiates a check to determine whether the command can be executed on the command target.
        /// </summary>
        /// <returns>
        /// True if the execution can continue.
        /// </returns>
        protected override bool CanExecute()
        {
            return this.Controller.IsPanEnabled && this.Controller.CameraMode != CameraMode.FixedPosition;
        }

        /// <summary>
        /// Gets the cursor for the gesture.
        /// </summary>
        /// <returns>
        /// A cursor.
        /// </returns>
        protected override Cursor GetCursor()
        {
            return this.Controller.PanCursor;
        }

        /// <summary>
        /// Called when inertia is starting.
        /// </summary>
        /// <param name="elapsedTime">
        /// The elapsed time (milliseconds).
        /// </param>
        protected override void OnInertiaStarting(int elapsedTime)
        {
            var speed = (this.LastPoint - this.MouseDownPoint) * (40.0 / elapsedTime);
            this.Controller.AddPanForce(speed.X, speed.Y);
        }

        /// <summary>
        /// Occurs when the manipulation is completed.
        /// </summary>
        /// <param name="e">
        /// The <see cref="ManipulationEventArgs"/> instance containing the event data.
        /// </param>
        public override void Completed(ManipulationEventArgs e)
        {
            base.Completed(e);
            if (this.CameraMode == CameraMode.InspectPath)
            {
                Controller.HideTargetAdorner();
            }
        }

    }
}