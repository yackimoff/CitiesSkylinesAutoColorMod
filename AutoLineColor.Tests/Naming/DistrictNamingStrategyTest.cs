using System.Collections.Generic;
using AutoLineColor.Naming;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AutoLineColor.Tests.Naming
{
    [TestClass]
    public sealed class DistrictNamingStrategyTest : NamingTestBase
    {
        private protected override INamingStrategy CreateStrategy()
        {
            return new DistrictNamingStrategy();
        }

        protected override IEnumerable<object> GetTestCases()
        {
            throw new System.NotImplementedException();
        }
    }
}
