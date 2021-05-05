
using System;
using System.CodeDom;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
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
        public void TestDomainLoadAssembly()
        
        {
            AppDomain ad = AppDomain.CreateDomain("Test");
            Loader loader = (Loader)ad.CreateInstanceFromAndUnwrap(
                typeof(Loader).Assembly.EscapedCodeBase,
                typeof(Loader).FullName);

            var file = @"D:\Users\pigi0\Source\Repos\SPATemplate\Solustion\DAL\bin\Debug\DAL.dll";
            var assembly = loader.LoadAssembly(file);
            AppDomain.Unload(ad);

            var types = assembly.GetTypes();

            //var assembly2 = Assembly.LoadFrom(file);
            //var t2 = assembly2.GetTypes();

            var expect =  IsFileLocked(new FileInfo(file));

            Assert.AreEqual(expect, false);


        }
        protected virtual bool IsFileLocked(FileInfo file)
        {
            try
            {
                using (FileStream stream = file.Open(FileMode.Open, FileAccess.Read, FileShare.None))
                {
                    stream.Close();
                }
            }
            catch (IOException)
            {
                //the file is unavailable because it is:
                //still being written to
                //or being processed by another thread
                //or does not exist (has already been processed)
                return true;
            }

            //file is not locked
            return false;
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


        [TestMethod()]
        public void TestClassToAliases()
        {
            Assert.AreEqual("TestType", typeof(TestType).ToAliases());
        }

        [TestMethod()]
        public void TestEnumToAliases()
        {
            Assert.AreEqual("TestEnum", typeof(TestEnum).ToAliases());
        }


        [TestMethod()]
        public void TesGenericTypeToAliases()
        {
            Assert.AreEqual("Collections.Generic.List<string>", typeof(List<string>).ToAliases());
        }


    }

    public class TestEnum
    {
    }

    public class TestType
    {
    }
}
