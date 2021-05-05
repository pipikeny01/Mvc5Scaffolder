
using System;
using System.CodeDom;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using Happy.Scaffolding.MVC.Utils;
using Microsoft.AspNet.Scaffolding.Core.Metadata;
using Microsoft.CSharp;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Happy.Scaffolding.MVC.Models.Tests
{
    [TestClass()]
    public class MetaColumnInfoTests
    {
        [TestMethod()]
        public void GetColumnTypeTest()
        {
            MetaColumnInfo mcol = new MetaColumnInfo();
            euColumnType expected = euColumnType.stringCT;
            euColumnType actual = mcol.GetColumnType("guid");
            Assert.AreEqual(expected, actual);
        }
        [TestMethod()]
        public void TestToMetadata()
        {
            var metadataHelper = new MetadataHelper(@"D:\Users\pigi0\Source\Repos\SPATemplate\Solustion\DAL\bin\Debug\DAL.dll");
            var metadatas = metadataHelper.ToMetadata("TestScaffold");

            Assert.AreNotEqual(0, metadatas.PrimaryKeys.Length);
            Assert.AreEqual("Id", metadatas.PrimaryKeys[0].PropertyName);

            var dateTimeNullValue = metadatas.Properties.FirstOrDefault(p=>p.PropertyName == "ModTime");
            Assert.AreEqual("DateTime?", dateTimeNullValue.ShortTypeName);
        }


        [TestMethod()]
        public void TestToAliases()
        {
            Assert.AreEqual("int", typeof(Int32).ToAliases());
            Assert.AreEqual("long", typeof(Int64).ToAliases());
            Assert.AreEqual("string", typeof(String).ToAliases());
            Assert.AreEqual("DateTime", typeof(DateTime).ToAliases());
            Assert.AreEqual("int?", typeof(Nullable<int>).ToAliases());
            Assert.AreEqual("DateTime?", typeof(Nullable<DateTime>).ToAliases());
        }


    }
}
