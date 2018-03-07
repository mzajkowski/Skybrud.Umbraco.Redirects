using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using Skybrud.Umbraco.Redirects.Extensions;
using Skybrud.Umbraco.Redirects.Import.Csv;
using Skybrud.Umbraco.Redirects.Models.Import.Validators;

namespace Skybrud.Umbraco.Redirects.Models.Import
{
    public class RedirectsImporter
    {
        private readonly IRedirectPublishedContentFinder contentFinder;

        public RedirectsImporter(IRedirectPublishedContentFinder contentFinder)
        {
            this.contentFinder = contentFinder;
        }

        public virtual ImporterResponse Import(string filename, CsvSeparator seperator)
        {
            var response = new ImporterResponse();

            var csv = CsvFile.Load(filename, seperator);

            csv.Columns.AddColumn("Status");
            csv.Columns.AddColumn("ErrorMessage");

            var redirects = csv.Rows.Select(Parse).ToList();

            var validatedItems = redirects.Select((redirect, index) => RedirectItemValidationContext.Validate(index, redirect, redirects)).ToList();

            response.ImportedItems = validatedItems;

            var dataTable = validatedItems
                .Where(validatedItem => validatedItem.IsValid)
                .Select(validatedItem => validatedItem.Item.Row)
                .ToList()
                .ToDataTable();

            dataTable.Columns["UniqueId"].ColumnName = "RedirectUniqueId";
            dataTable.Columns["Id"].ColumnName = "RedirectId";

            using (var connection = new SqlConnection(ConfigurationManager.ConnectionStrings["umbracoDbDSN"].ConnectionString))
            {
                connection.Open();

                var truncate = new SqlCommand
                {
                    CommandText = @"TRUNCATE TABLE SkybrudRedirectsImport",
                    CommandType = CommandType.Text,
                    Connection = connection
                };

                truncate.ExecuteScalar();

                using (var bulkCopy = new SqlBulkCopy
                (
                    connection,
                    SqlBulkCopyOptions.TableLock |
                    SqlBulkCopyOptions.FireTriggers |
                    SqlBulkCopyOptions.UseInternalTransaction,
                    null
                ))
                {
                    bulkCopy.DestinationTableName = "SkybrudRedirectsImport";

                    bulkCopy.BatchSize = 0;

                    foreach (DataColumn col in dataTable.Columns)
                    {
                        bulkCopy.ColumnMappings.Add(col.ColumnName, col.ColumnName);
                    }

                    bulkCopy.WriteToServer(dataTable);
                    bulkCopy.Close();
                }
                connection.Close();
                connection.Open();

                var cmd = new SqlCommand();

                cmd.CommandText = @"
                    MERGE INTO SkybrudRedirects WITH (HOLDLOCK) AS target
                    USING SkybrudRedirectsImport AS source
                        ON target.LinkUrl = source.LinkUrl
                        AND target.Url = source.Url
                    WHEN MATCHED THEN UPDATE SET 
                        target.QueryString = source.QueryString,
                        target.LinkMode = source.LinkMode,
                        target.LinkId = source.LinkId,
                        target.Created = source.Created,
                        target.Updated = source.Updated,
                        target.IsPermanent = source.IsPermanent,
                        target.IsRegex = source.IsRegex,
                        target.ForwardQueryString = source.ForwardQueryString

                    WHEN NOT MATCHED BY TARGET THEN

                    INSERT(
                        Url,
                        QueryString,
                        RedirectUniqueId,
                        RootNodeId,
                        LinkMode,
                        LinkId,
                        LinkUrl,
                        LinkName,
                        Created,
                        Updated,
                        IsPermanent,
                        IsRegEx,
                        ForwardQueryString)
                    VALUES(
                        source.Url, 
                        source.QueryString,
                        source.RedirectUniqueId,
                        source.RootNodeId,
                        source.LinkMode,
                        source.LinkId,
                        source.LinkUrl,
                        source.LinkName,
                        source.Created,
                        source.Updated,
                        source.IsPermanent,
                        source.IsRegEx,
                        source.ForwardQueryString);";

                cmd.CommandType = CommandType.Text;
                cmd.Connection = connection;

                cmd.ExecuteScalar();

                connection.Close();
            }

            dataTable.Clear();

            foreach (var item in validatedItems)
            {
                csv.Rows[item.Index].AddCell(item.Status.ToString());
                csv.Rows[item.Index].AddCell(string.Join(",", item.ValidationResults.Select(a => a.ErrorMessage)));
            }

            response.File = csv;

            return response;
        }

        private RedirectItem Parse(CsvRow row)
        {
            var redirectItemRow = new RedirectItemRow();

            string sourceUrlRaw = row.Cells[0] == null ? null : row.Cells[0].Value.Trim();

            Uri sourceUrl;

            if (Uri.TryCreate(sourceUrlRaw, UriKind.Absolute, out sourceUrl))
            {
                redirectItemRow.Url = sourceUrl.AbsolutePath;
            }

            string destinationUrlRaw = row.Cells[1] == null ? null : row.Cells[1].Value.Trim();

            Uri destinationUrl;

            if (Uri.TryCreate(destinationUrlRaw, UriKind.Absolute, out destinationUrl))
            {
                redirectItemRow.LinkUrl = destinationUrl.AbsolutePath;
            }

            redirectItemRow.UniqueId = Guid.NewGuid().ToString();

            RedirectLinkMode linkMode;

            var linkModeRaw = row.Cells[2].Value == null ? RedirectLinkMode.Url.ToString() : row.Cells[2].Value.Trim();

            Enum.TryParse(linkModeRaw, out linkMode);

            redirectItemRow.LinkMode = linkMode.ToString().ToLower();

            if (destinationUrl != null)
            {
                var urlContent = contentFinder.Find(destinationUrl.AbsolutePath);

                if (urlContent != null)
                {
                    redirectItemRow.LinkMode = RedirectLinkMode.Content.ToString().ToLower();
                    redirectItemRow.LinkId = urlContent.Id;
                    redirectItemRow.LinkName = urlContent.Name;
                    redirectItemRow.LinkUrl = urlContent.Url;
                }
                else
                {
                    redirectItemRow.LinkUrl = destinationUrl.AbsolutePath;
                }
            }            

            redirectItemRow.RootNodeId = 0;
            redirectItemRow.IsPermanent = true;
            redirectItemRow.IsRegex = false;
            redirectItemRow.ForwardQueryString = true;

            var redirectItem = new RedirectItem(redirectItemRow);

            return redirectItem;
        }
    }
}