using System;
using System.Collections.Generic;
using AutoLineColor.Naming;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AutoLineColor.Tests.Naming
{
    [TestClass]
    public class NoNamingStrategyTest : NamingTestBase
    {
        private protected override INamingStrategy CreateStrategy()
        {
            return new NoNamingStrategy();
        }

        protected override IEnumerable<object> GetTestCases()
        {
            throw new NotImplementedException();
        }
    }
}
