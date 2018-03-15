using System.IO;
using System.Linq;
using Moq;
using Skybrud.Umbraco.Redirects.Import.Csv;
using Skybrud.Umbraco.Redirects.Models.Import;
using Skybrud.Umbraco.Redirects.Models.Import.File;
using Umbraco.Core.Models;
using Xunit;

namespace Skybrud.Umbraco.Redirects.Tests
{
    public class ExcelImportTests
    {
        public IRedirectPublishedContentFinder CreateMockContentFinder(string url = "/source1")
        {
            var publishedContentMock = new Mock<IPublishedContent>();

            publishedContentMock.Setup(mock => mock.Url).Returns(url);

            var contentFinderMock = new Mock<IRedirectPublishedContentFinder>();

            contentFinderMock.Setup(mock => mock.Find(url)).Returns(publishedContentMock.Object);

            return contentFinderMock.Object;
        }

        [Fact]
        public void Import_TestImportedEvent_ShouldRaiseEvent()
        {
            var importer = new RedirectsImporterService();

            var totalRedirects = 0;

            //Handy event for future integrations
            importer.Imported += (e, r) => { totalRedirects = r.Response.ImportedItems.Count(a => a.IsValid); };

            var redirectsFile = new ExcelRedirectsFile(CreateMockContentFinder());

            redirectsFile.FileName = string.Concat(Directory.GetCurrentDirectory(), "\\Files\\Excel\\redirects.xlsx");

            importer.Import(redirectsFile);

            Assert.True(totalRedirects > 0);
        }

        [Fact]
        public void Import_ValidRedirects_ShouldSucceedWithAllValid()
        {
            var importer = new RedirectsImporterService();

            var redirectsFile = new ExcelRedirectsFile(CreateMockContentFinder());

            redirectsFile.FileName = string.Concat(Directory.GetCurrentDirectory(), "\\Files\\Csv\\redirects.xlsx");

            var response = importer.Import(redirectsFile);    
            
            Assert.True(response.ImportedItems.All(item => item.IsValid));
        }

        [Fact]
        public void Import_BlankLines_ShouldReturnValidationErrors()
        {
            var importer = new RedirectsImporterService();

            var redirectsFile = new CsvRedirectsFile(CreateMockContentFinder());

            redirectsFile.FileName = string.Concat(Directory.GetCurrentDirectory(), "\\Files\\Csv\\redirects-blanklines.csv");
            redirectsFile.Seperator = CsvSeparator.Comma;

            var response = importer.Import(redirectsFile);

            Assert.Equal("Error", ((CsvRedirectsFile)response.File).File.Rows[4].Cells[3].Value);
            Assert.Equal("No source URL was provided or is in the wrong format", ((CsvRedirectsFile)response.File).File.Rows[4].Cells[4].Value);
        }

        [Fact]
        public void Import_RedirectChain_ShouldReturnValidationErrors()
        {
            var importer = new RedirectsImporterService();

            var redirectsFile = new CsvRedirectsFile(CreateMockContentFinder());

            redirectsFile.FileName = string.Concat(Directory.GetCurrentDirectory(), "\\Files\\Csv\\redirects-redirectchain.csv");
            redirectsFile.Seperator = CsvSeparator.Comma;

            var response = importer.Import(redirectsFile);

            Assert.Equal("Warning", ((CsvRedirectsFile)response.File).File.Rows[0].Cells[3].Value);
            Assert.Equal("This redirect links to the URL (/destination1) in the file. This will result in a redirect chain", ((CsvRedirectsFile)response.File).File.Rows[0].Cells[4].Value);
        }

        [Fact]
        public void Import_RedirectLoop_ShouldReturnValidationErrors()
        {
            var importer = new RedirectsImporterService();

            var redirectsFile = new CsvRedirectsFile(CreateMockContentFinder());

            redirectsFile.FileName = string.Concat(Directory.GetCurrentDirectory(), "\\Files\\Csv\\redirects-redirectloop.csv");
            redirectsFile.Seperator = CsvSeparator.Comma;

            var response = importer.Import(redirectsFile);
            
            Assert.Equal("Error", ((CsvRedirectsFile)response.File).File.Rows[0].Cells[3].Value);
            Assert.Equal("This redirect would create a redirect loop as another redirect exists with the URL (/destination1) in the file. It has not been imported.", ((CsvRedirectsFile)response.File).File.Rows[0].Cells[4].Value);
        }
    }
}