﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

using NCheck.Checking;

namespace NCheck
{
    /// <summary>
    /// Comparator used to verify that two instances of <see typeparamref="T" /> are 
    /// the same on a per property basis.
    /// </summary>
    /// <typeparam name="T">Type whose instances we will check</typeparam>
    public class Checker<T> : Checker, IChecker<T>, IChecker, ICheckerCompare
    {
// ReSharper disable StaticFieldInGenericType
        private static readonly MethodInfo CheckClassMi = typeof(Checker<T>).GetMethod("CheckClass", BindingFlags.Static | BindingFlags.NonPublic);
        private static readonly MethodInfo CheckParentClassMi = typeof(Checker<T>).GetMethod("CheckParentClass", BindingFlags.Static | BindingFlags.NonPublic);
// ReSharper restore StaticFieldInGenericType
        private readonly IList<PropertyCheck> properties;
        private readonly MethodInfo parentChecker;
        private readonly Type parentType;
        private CheckerConventions conventions;

        /// <summary>
        /// Creates a new instance of the <see cref="Checker{T}" /> class.
        /// </summary>
        public Checker()
        {
            properties = new List<PropertyCheck>();
            parentType = typeof(T).BaseType;
            if (parentType != null && parentType != typeof(object) && parentType != typeof(ValueType))
            {
                // Get a checker for the parent
                parentChecker = CheckParentClassMi.MakeGenericMethod(parentType);
            }
        }

        /// <summary>
        /// Gets or sets the <see cref="CheckerConventions"/>.
        /// </summary>
        public CheckerConventions Conventions
        {
            get { return conventions ?? (conventions = ConventionsFactory.Conventions); }
            set { conventions = value; }
        }

        /// <summary>
        /// Allows access to the <see cref="PropertyCheck" />s by name
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public PropertyCheck this[string name]
        {
            get { return Properties.FirstOrDefault(x => x.Info.Name == name); }
        }

        /// <copydocfrom cref="ICheckerCompare.Properties" />
        protected IList<PropertyCheck> Properties
        {
            get { return properties; }
        }

        /// <copydocfrom cref="ICheckerCompare.Properties" />
        ICollection<PropertyCheck> ICheckerCompare.Properties
        {
            get { return Properties; }
        }

        /// <summary>
        /// Gets a list of descendant types for the checker.
        /// </summary>
        protected virtual IEnumerable<Type> Descendants
        {
            get { return new List<Type>(); }
        }

        /// <summary>
        /// Check that the properties of two instances of <see typeparamref="T" /> are equal.
        /// </summary>
        /// <param name="expected">Expected object to use</param>
        /// <param name="candidate">Candidate object to use</param>
        /// <param name="objectName">Name to use, displayed in error messages to disambiguate</param>
        public void Check(T expected, T candidate, string objectName = "")
        {
            if (string.IsNullOrEmpty(objectName))
            {
                objectName = typeof(T).Name;
            }

            if (!CheckDescendants(expected, candidate, objectName))
            {
                CheckBase(expected, candidate, objectName);
            }
        }

        /// <summary>
        /// Check the base properties of <see typeparameref="T" />, which are the parent properties and those directly declared in T.
        /// </summary>
        /// <param name="expected">Expected object to use</param>
        /// <param name="candidate">Candidate object to use</param>
        /// <param name="objectName">Name to use, displayed in error messages to disambiguate</param>
        public void CheckBase(T expected, T candidate, string objectName = "")
        {
            if (string.IsNullOrEmpty(objectName))
            {
                objectName = typeof(T).Name;
            }

            // First check the parent
            CheckParent(expected, candidate, objectName);

            // Now our explicit ones.
            CheckComparisons(expected, candidate, objectName);
        }

        /// <copydocfrom cref="IChecker.Check" />
        void IChecker.Check(object expected, object candidate, string objectName)
        {
            Check(ToValue<T>(expected, "expected", objectName), ToValue<T>(candidate, "candidate", objectName), objectName);
        }

        /// <summary>
        /// Checks an entity to see if it satisfies a class constraint
        /// </summary>
        /// <typeparam name="TClass">Type of class to check.</typeparam>
        /// <param name="expected">Expected object to use</param>
        /// <param name="candidate">Candidate object to use</param>
        /// <param name="objectName">Name to use, displayed in error messages to disambiguate</param>
        /// <returns>true if the instance pass, otherwise false.</returns>
        protected static bool CheckClass<TClass>(object expected, object candidate, string objectName)
            where TClass : class
        {
            if (expected is TClass)
            {
                CheckerFactory.Check(expected as TClass, candidate as TClass, objectName);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Check the parent class of an entity
        /// </summary>
        /// <typeparam name="TClass">Type to cast the objects to.</typeparam>
        /// <param name="expected">Expected object to use</param>
        /// <param name="candidate">Candidate object to use</param>
        /// <param name="objectName">Name to use, displayed in error messages to disambiguate</param>
        protected static void CheckParentClass<TClass>(object expected, object candidate, string objectName)
            where TClass : class
        {
            CheckerFactory.CheckParent(expected as TClass, candidate as TClass, objectName);
        }

        /// <summary>
        /// Checks the parent properties of T.
        /// </summary>
        /// <param name="expected">Expected object to use</param>
        /// <param name="candidate">Candidate object to use</param>
        /// <param name="objectName">Name to use, displayed in error messages to disambiguate</param>
        protected virtual void CheckParent(T expected, T candidate, string objectName)
        {
            if (parentChecker == null)
            {
                return;
            }

            parentChecker.Invoke(null, new object[] { expected, candidate, objectName });
        }

        /// <summary>
        /// Automatically initializes the comparisons based on the public properties.
        /// </summary>
        protected void Initialize()
        {
            this.AutoCheck(typeof(T));
        }

        /// <summary>
        /// Check all immediate descendants of the class.
        /// </summary>
        /// <param name="expected">Expected object to use</param>
        /// <param name="candidate">Candidate object to use</param>
        /// <param name="objectName">Name to use, displayed in error messages to disambiguate</param>
        /// <returns>true if we are a descendant, false otherwise</returns>
        protected virtual bool CheckDescendants(object expected, object candidate, string objectName)
        {
            return Descendants
                        .Select(type => CheckClassMi.MakeGenericMethod(type))
                        .Any(castMethod => (bool)castMethod.Invoke(null, new[] { expected, candidate, objectName }));
        }

        /// <summary>
        /// Check for equality between two objects.
        /// </summary>
        /// <param name="expected">Expected object to use</param>
        /// <param name="candidate">Candidate object to use</param>
        /// <param name="objectName">Name to use, displayed in error messages to disambiguate</param>
        protected virtual void CheckComparisons(T expected, T candidate, string objectName)
        {
            foreach (var prop in Properties)
            {
                prop.Check(CheckerFactory, expected, candidate, objectName);
            }
        }

        /// <summary>
        /// Add comparison to an entity checker.
        /// </summary>
        /// <param name="propertyExpression"></param>
        /// <returns></returns>
        protected PropertyCheckExpression Compare(Expression<Func<T, object>> propertyExpression)
        {
            return Compare(propertyExpression.GetPropertyInfo());
        }

        /// <summary>
        /// Add comparison to an entity checker.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="flags"></param>
        /// <returns></returns>
        protected PropertyCheckExpression Compare(string name, BindingFlags flags)
        {
            var propertyInfo = typeof(T).GetProperty(name, flags);
            if (propertyInfo == null)
            {
                throw new NotSupportedException("Could not find property: " + name);
            }

            return Compare(propertyInfo);
        }

        /// <copydocfrom cref="ICheckerCompare.Compare" />
        PropertyCheckExpression ICheckerCompare.Compare(PropertyInfo propertyInfo)
        {
            return Compare(propertyInfo);
        }

        /// <copydocfrom cref="ICheckerCompare.Compare" />
        protected PropertyCheckExpression Compare(PropertyInfo propertyInfo)
        {
            if (propertyInfo == null)
            {
                throw new ArgumentNullException(nameof(propertyInfo));
            }

            return Compare(propertyInfo, Conventions.CompareTarget(propertyInfo));
        }

        /// <summary>
        /// Include the property in the comparison test.
        /// </summary>
        /// <param name="propertyInfo">PropertyInfo to use</param>
        /// <param name="compareTarget"></param>        
        /// <returns>A new <see cref="PropertyCheckExpression" /> created from the <see cref="PropertyInfo" /></returns>
        protected PropertyCheckExpression Compare(PropertyInfo propertyInfo, CompareTarget compareTarget)
        {
            var pc = this[propertyInfo.Name];
            if (pc == null)
            {
                // Add the new check
                pc = new PropertyCheck(propertyInfo, compareTarget);
                Properties.Add(pc);
            }
            else
            {
                // Update to the supplied target
                pc.CompareTarget = compareTarget;
            }

            return new PropertyCheckExpression(pc);
        }

        /// <summary>
        /// Try to cast an object to the required type
        /// </summary>
        /// <param name="value"></param>
        /// <param name="x"></param>
        /// <param name="objectName"></param>
        /// <typeparam name="TEntity"></typeparam>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        protected TEntity ToValue<TEntity>(object value, string x, string objectName)
        {
            try
            {
                return (TEntity)value;
            }
            catch (InvalidCastException)
            {
                throw new Exception(string.Format("{0}: Could not cast {1} value {2} ({3}) to {4}", objectName, x, value, value == null ? "Unknown" : value.GetType().Name, typeof(TEntity).Name));
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("{0}: Could not cast {1} value {2} ({3}) to {4}: {5}", objectName, x, value, value == null ? "Unknown" : value.GetType().Name, typeof(TEntity).Name, ex.Message));
            }
        }
    }
}