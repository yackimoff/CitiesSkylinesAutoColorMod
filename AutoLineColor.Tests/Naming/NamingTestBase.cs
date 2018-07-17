using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using AutoLineColor;
using AutoLineColor.Naming;

namespace AutoLineColor.Tests.Naming
{
    [TestClass]
    [TestCategory("Naming Strategies")]
    public abstract class NamingTestBase
    {
        private protected abstract INamingStrategy CreateStrategy();

        protected abstract IEnumerable<object> GetTestCases(); //XXX

        [DataTestMethod]
        [DynamicData("GetTestCases", DynamicDataSourceType.Method)]
        public void TestNaming()
        {
            var strategy = CreateStrategy();

            //XXX
            Assert.Inconclusive();
        }
    }
}
