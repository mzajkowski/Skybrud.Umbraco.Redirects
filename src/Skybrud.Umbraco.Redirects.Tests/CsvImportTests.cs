using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.XPath;
using GDev.Umbraco.Test;
using Moq;
using Skybrud.Umbraco.Redirects.Import.Csv;
using Skybrud.Umbraco.Redirects.Models;
using Umbraco.Core.Models;
using Umbraco.Core.Models.PublishedContent;
using Umbraco.Core.PropertyEditors;
using Umbraco.Core.Xml;
using Umbraco.Web.PublishedCache;
using Xunit;
using File = System.IO.File;
using UmbracoCmsWeb = Umbraco.Web;

namespace Skybrud.Umbraco.Redirects.Tests
{
    public class CsvImportTests
    {
        public interface Importer
        {
            
        }

        [Fact]
        public void Import_ValidRedirects_ShouldSucceed()
        {
            var umbracoContext = new ContextMocker().UmbracoContextMock;

            var repositoryMock = new Mock<RedirectsRepository>();

            repositoryMock.Setup(m => m.GetRedirectByUrl(-1, "http://skybrudtest.dev.local/source1"));

            var existingItem = new RedirectItem
            {
                Url = "http://skybrudtest.dev.local/source1",
                Link = new RedirectLinkItem(3, "destination1", "http://skybrudtest.dev.local/destination1,url", RedirectLinkMode.Url)
            };

            repositoryMock.Setup(m => m.SaveRedirect(existingItem));

            var repository = repositoryMock.Object;

            var publishedCacheMock = new Mock<IPublishedContentCache>();

            publishedCacheMock.Setup(m => m.GetByRoute(umbracoContext, false, "/source1", false));

            var importer = new RedirectsImporter(umbracoContext);

            importer.Import(string.Concat(Directory.GetCurrentDirectory(), "\\Files\\Csv\\redirects.csv"), CsvSeparator.Comma);
        }

        [Fact]
        public void Import_BlankLines_ShouldReturnValidationError()
        {
            var umbracoContext = new ContextMocker().UmbracoContextMock;
            
            var repositoryMock = new Mock<RedirectsRepository>();

            repositoryMock.Setup(m => m.GetRedirectByUrl(-1, "http://skybrudtest.dev.local/source1"));

            var existingItem = new RedirectItem
            {
                Url = "http://skybrudtest.dev.local/source1",
                Link = new RedirectLinkItem(3, "destination1", "http://skybrudtest.dev.local/destination1,url", RedirectLinkMode.Url)
            };

            repositoryMock.Setup(m => m.SaveRedirect(existingItem));

            var repository = repositoryMock.Object;

            var publishedCacheMock = new Mock<IPublishedContentCache>();

            publishedCacheMock.Setup(m => m.GetByRoute(umbracoContext, false, "/source1", false));

            var importer = new RedirectsImporter(umbracoContext);

            var response = importer.Import(string.Concat(Directory.GetCurrentDirectory(), "\\Files\\Csv\\redirects.csv"), CsvSeparator.Comma);

            Assert.NotEmpty(response.ImportedItems);

            FileInfo outputFile = new FileInfo(string.Concat(Directory.GetCurrentDirectory(), "\\Files\\Csv\\redirects.xlsx"));

            //NTH
            //using (FastExcel.FastExcel fastExcel = new FastExcel.FastExcel(outputFile))
            //{
            //    var objectList = new List<RedirectItem>();

            //    foreach (var VARIABLE in response.ImportedItems)
            //    {
                    
            //    }

            //    RedirectItem genericObject = new RedirectItem();
            //    genericObject

            //    objectList.Add(genericObject);

            //    fastExcel.Write(objectList, "sheet3", true);
            //}
        }
    }
}