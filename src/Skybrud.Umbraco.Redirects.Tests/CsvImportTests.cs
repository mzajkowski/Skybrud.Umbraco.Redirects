using System.IO;
using Moq;
using Skybrud.Umbraco.Redirects.Import.Csv;
using Skybrud.Umbraco.Redirects.Models;
using Skybrud.Umbraco.Redirects.Models.Import;
using Umbraco.Core.Models;
using Umbraco.Web.PublishedCache;
using Xunit;

namespace Skybrud.Umbraco.Redirects.Tests
{
    public class CsvImportTests
    {
        [Fact]
        public void Import_ValidRedirects_ShouldSucceed()
        {
            var publishedContentMock = new Mock<IPublishedContent>();

            publishedContentMock.Setup(mock => mock.Url).Returns("/source1");

            var contentFinderMock = new Mock<IRedirectPublishedContentFinder>();

            contentFinderMock.Setup(mock => mock.Find("/source1")).Returns(publishedContentMock.Object);

            var importer = new RedirectsImporter(contentFinderMock.Object);

            var response = importer.Import(string.Concat(Directory.GetCurrentDirectory(), "\\Files\\Csv\\redirects.csv"), CsvSeparator.Comma);    
            
            Assert.NotEmpty(response.ImportedItems);
        }

        [Fact]
        public void Import_BlankLines_ShouldSucceed()
        {
            var publishedContentMock = new Mock<IPublishedContent>();

            publishedContentMock.Setup(mock => mock.Url).Returns("/source1");

            var contentFinderMock = new Mock<IRedirectPublishedContentFinder>();

            contentFinderMock.Setup(mock => mock.Find("/source1")).Returns(publishedContentMock.Object);

            var importer = new RedirectsImporter(contentFinderMock.Object);

            var response = importer.Import(string.Concat(Directory.GetCurrentDirectory(), "\\Files\\Csv\\redirects-blanklines.csv"), CsvSeparator.Comma);

            Assert.Equal("Error", response.File.Rows[4].Cells[3].Value);
            Assert.Equal("No destination URL was provided or is in the wrong format", response.File.Rows[4].Cells[4].Value);
        }

        [Fact]
        public void Import_RedirectChain_ShouldSucceed()
        {
            var publishedContentMock = new Mock<IPublishedContent>();

            publishedContentMock.Setup(mock => mock.Url).Returns("/source1");

            var contentFinderMock = new Mock<IRedirectPublishedContentFinder>();

            contentFinderMock.Setup(mock => mock.Find("/source1")).Returns(publishedContentMock.Object);

            var importer = new RedirectsImporter(contentFinderMock.Object);

            var response = importer.Import(string.Concat(Directory.GetCurrentDirectory(), "\\Files\\Csv\\redirects-redirectchain.csv"), CsvSeparator.Comma);
            
            Assert.Equal("Warning", response.File.Rows[0].Cells[3].Value);
            Assert.Equal("This redirect links to the URL (/destination1) in the file. This will result in a redirect chain", response.File.Rows[0].Cells[4].Value);
        }

        [Fact]
        public void Import_RedirectLoop_ShouldSucceed()
        {
            var publishedContentMock = new Mock<IPublishedContent>();

            publishedContentMock.Setup(mock => mock.Url).Returns("/source1");

            var contentFinderMock = new Mock<IRedirectPublishedContentFinder>();

            contentFinderMock.Setup(mock => mock.Find("/source1")).Returns(publishedContentMock.Object);

            var importer = new RedirectsImporter(contentFinderMock.Object);

            var response = importer.Import(string.Concat(Directory.GetCurrentDirectory(), "\\Files\\Csv\\redirects-redirectloop.csv"), CsvSeparator.Comma);

            Assert.Equal("Error", response.File.Rows[0].Cells[3].Value);
            Assert.Equal("This redirect would create a redirect loop as another redirect exists with the URL (/destination1) in the file. It has not been imported.", response.File.Rows[0].Cells[4].Value);
        }
    }
}