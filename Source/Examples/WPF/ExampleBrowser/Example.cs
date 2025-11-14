// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Example.cs" company="Helix Toolkit">
//   Copyright (c) 2014 Helix Toolkit contributors
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace ExampleBrowser
{
    using FractalDemo;
    using System;
    using System.Diagnostics;
    using System.Linq.Expressions;
    using System.Reflection;
    using System.Windows;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;

    public class Example
    {
        public string Title { get; private set; }
        public string Description { get; set; }
        private Type MainWindowType { get; set; }
        public ImageSource Thumbnail { get; set; }
        public string ThumbnailFileName
        {
            get
            {
                return this.MainWindowType.Namespace + "_small.png";
            }
        }

        public Example(Type mainWindowType, string title = null, string description = null)
        {
            this.MainWindowType = mainWindowType;
            this.Title = title ?? mainWindowType.Namespace;
            this.Description = description;
            try
            {
                this.Thumbnail =
                    new BitmapImage(new Uri("pack://application:,,,/Images/" + this.ThumbnailFileName));
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
            }
        }

        public override string ToString()
        {
            return this.Title;
        }

        public Window Create()
        {
            return Activator.CreateInstance(this.MainWindowType) as Window;
        }
    }

    public static class ExampleExtensions
    {
        /// <summary>
        /// Converts an expression into a <see cref="MemberInfo"/>.
        /// </summary>
        /// <param name="expression">The expression to convert.</param>
        /// <returns>The member info.</returns>
        public static MemberInfo GetMemberInfo(this System.Linq.Expressions.Expression expression)
        {
            var lambda = (LambdaExpression)expression;

            MemberExpression memberExpression;
            if (lambda.Body is UnaryExpression)
            {
                var unaryExpression = (UnaryExpression)lambda.Body;
                memberExpression = (MemberExpression)unaryExpression.Operand;
            }
            else
            {
                memberExpression = (MemberExpression)lambda.Body;
            }

            return memberExpression.Member;
        }

        //public static void SetValue<T>(this PropertyTools.Observable observable, ref T thing, T value, Expression<Func<T>> property)
        //{
        //    var propertyName = property.GetMemberInfo().Name;
        //    observable.SetValue(ref thing, value, propertyName);
        //}
    }
}