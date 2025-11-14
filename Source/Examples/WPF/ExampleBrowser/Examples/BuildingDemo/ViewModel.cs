// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ViewModel.cs" company="Helix Toolkit">
//   Copyright (c) 2014 Helix Toolkit contributors
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace BuildingDemo
{
    using ExampleBrowser;
    using PropertyTools;
    using System;
    using System.Linq.Expressions;
    using System.Windows.Media.Media3D;

    public class ViewModel : Observable
    {
        private object selectedObject;

        public object SelectedObject
        {
            get
            {
                return this.selectedObject;
            }

            set
            {
                this.SetValue(ref this.selectedObject, value, () => this.SelectedObject);
            }
        }

        public void Select(Visual3D visual)
        {
            this.SelectedObject = visual;
        }

        public void SetValue<T>(ref T thing, T value, Expression<Func<T>> property)
        {
            var propertyName = property.GetMemberInfo().Name;
            this.SetValue(ref thing, value, propertyName);
        }
    }
}