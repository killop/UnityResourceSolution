using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.Build.Pipeline.Interfaces;
using UnityEngine;

namespace UnityEditor.Build.Pipeline.Tests
{
    class MyContextObjectClass : IContextObject
    {
    }

    interface ITestInterfaceWithContextDerivation : IContextObject {}
    class TestITestInterfaceWithContextDerivationImplementation : ITestInterfaceWithContextDerivation
    {}

    public class BuildContextTests
    {
        BuildContext m_Ctx;
        [SetUp]
        public void Setup()
        {
            m_Ctx = new BuildContext();
        }

        [Test]
        public void SetContextObject_WhenTypeDoesNotExist_AddsContextObject()
        {
            m_Ctx.SetContextObject(new MyContextObjectClass());
            Assert.NotNull(m_Ctx.GetContextObject<MyContextObjectClass>());
        }

        [Test]
        public void SetContextObject_WhenTypeHasInterfaceAssignableToContextObject_InterfaceAndObjectTypeUsedAsKey()
        {
            m_Ctx.SetContextObject(new TestITestInterfaceWithContextDerivationImplementation());
            Assert.NotNull(m_Ctx.GetContextObject<ITestInterfaceWithContextDerivation>());
            Assert.NotNull(m_Ctx.GetContextObject<TestITestInterfaceWithContextDerivationImplementation>());
        }

        [Test]
        public void GetContextObject_WhenTypeDoesNotExist_Throws()
        {
            Assert.Throws(typeof(Exception), () => m_Ctx.GetContextObject<ITestInterfaceWithContextDerivation>());
        }
    }
}
