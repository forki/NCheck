﻿namespace NCheck.Test
{
    using NCheck.Checking;

    using NUnit.Framework;

    [TestFixture]
    public class AutoBuildConventionsFixture : Fixture
    {
        [Test]
        public void ShouldObeyIgnoreConvention()
        {
            CheckProperty("Ignore", CompareTarget.Ignore);
        }

        [Test]
        public void ShouldObeyExplicitIgnore()
        {
            CheckProperty("Another", CompareTarget.Ignore);
        }

        private void CheckProperty(string name, CompareTarget expected)
        {
            var checker = new ParentChecker();
            var property = checker[name];
            Assert.AreEqual(expected, property.CompareTarget);  
        }

        [SetUp]
        public void Setup()
        {
            var x = CheckerFactory;
        }
    }
}