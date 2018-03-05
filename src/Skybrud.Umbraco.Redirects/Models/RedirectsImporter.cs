using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using Skybrud.Essentials.Time;
using Skybrud.Umbraco.Redirects.Import.Csv;
using Umbraco.Web;

namespace Skybrud.Umbraco.Redirects.Models
{
    public class RedirectsImporter
    {
        private readonly UmbracoContext umbracoContext;

        public RedirectsImporter(UmbracoContext umbracoContext)
        {
            this.umbracoContext = umbracoContext;
        }

        public virtual ImporterResponse Import(string filename, CsvSeparator seperator)
        {
            var response = new ImporterResponse();

            var csv = CsvFile.Load(filename, seperator);         

            var dataTable = new DataTable();

            dataTable.Columns.Add("RedirectId", typeof(int));
            dataTable.Columns.Add("RedirectUniqueId", typeof(string));
            dataTable.Columns.Add("RootNodeId", typeof(int));
            dataTable.Columns.Add("Url", typeof(string));
            dataTable.Columns.Add("QueryString", typeof(string));
            dataTable.Columns.Add("LinkMode", typeof(string));
            dataTable.Columns.Add("LinkId", typeof(int));
            dataTable.Columns.Add("LinkUrl", typeof(string));
            dataTable.Columns.Add("LinkName", typeof(string));
            dataTable.Columns.Add("Created", typeof(long));
            dataTable.Columns.Add("Updated", typeof(long));
            dataTable.Columns.Add("IsPermanent", typeof(bool));
            dataTable.Columns.Add("IsRegex", typeof(bool));
            dataTable.Columns.Add("ForwardQueryString", typeof(bool));

            foreach (var row in csv.Rows)
            {               
                var sourceUrl = new Uri(row.Cells[0].Value.Trim());

                var destinationUrl = new Uri(row.Cells[1].Value.Trim());

                var dataTableRow = dataTable.NewRow();

                var redirectItem = new RedirectItem();

                redirectItem.Url = sourceUrl.AbsolutePath;
                dataTableRow["Url"] = redirectItem.Url;

                redirectItem.Url = destinationUrl.Query;
                dataTableRow["QueryString"] = redirectItem.Url;

                RedirectLinkMode linkMode;

                Enum.TryParse(row.Cells[2].Value, out linkMode);

                redirectItem.LinkMode = linkMode;
                dataTableRow["LinkMode"] = redirectItem.LinkMode.ToString().ToLower();

                var urlContent = umbracoContext.ContentCache.GetByRoute(false, sourceUrl.AbsolutePath, false);

                if (urlContent != null)
                {
                    redirectItem.LinkMode = RedirectLinkMode.Content;
                    dataTableRow["LinkMode"] = RedirectLinkMode.Content.ToString().ToLower();

                    redirectItem.LinkId = urlContent.Id;
                    dataTableRow["LinkId"] = urlContent.Id;

                    redirectItem.LinkName = urlContent.Name;
                    dataTableRow["LinkName"] = redirectItem.LinkName;

                    redirectItem.LinkUrl = urlContent.Url;
                    dataTableRow["LinkUrl"] = redirectItem.LinkUrl;
                }
                else
                {
                    redirectItem.LinkUrl = destinationUrl.AbsolutePath;
                    dataTableRow["LinkUrl"] = redirectItem.LinkUrl;
                    dataTableRow["LinkId"] = 0;
                }

                redirectItem.RootNodeId = 0;
                dataTableRow["RootNodeId"] = 0;

                //redirectItem.UniqueId = Guid.NewGuid().ToString();
                dataTableRow["RedirectUniqueId"] = Guid.NewGuid().ToString();

                redirectItem.Created = EssentialsDateTime.CurrentUnixTimestamp;
                dataTableRow["Created"] = EssentialsDateTime.CurrentUnixTimestamp;

                redirectItem.Updated = EssentialsDateTime.CurrentUnixTimestamp;
                dataTableRow["Updated"] = EssentialsDateTime.CurrentUnixTimestamp;

                redirectItem.IsPermanent = true;
                dataTableRow["IsPermanent"] = true;

                redirectItem.IsRegex = false;
                dataTableRow["IsRegex"] = false;

                redirectItem.ForwardQueryString = true;
                dataTableRow["ForwardQueryString"] = true;

                dataTable.Rows.Add(dataTableRow);
                response.AddImportedItem(redirectItem);
            }

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

            return response;
        }
    }

    public class ImporterResponse
    {
        public ImporterResponse()
        {
            ImportedItems = new List<RedirectItem>();
        }

        public List<RedirectItem> ImportedItems { get; private set;  }

        public void AddImportedItem(RedirectItem item)
        {
            ImportedItems.Add(item);
        }
    }
}