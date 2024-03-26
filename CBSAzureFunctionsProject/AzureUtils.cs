using System.Net;
using System.Net.Http;
using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace CBS
{
    public static class AzureUtils
    {
        public static string TABLE_NOT_FOUND_CODE = "TableNotFound";

        public static string GetTableQueryURL(string storageKey, string secretKey, string tableID, string rowKey, string partitionKey, int? nTop)
        {
            var nTopQuery = string.Empty;
            var keyQuery = string.Empty;

            var validKeys = !string.IsNullOrEmpty(rowKey) || !string.IsNullOrEmpty(partitionKey);

            if (validKeys)
            {
                rowKey = string.IsNullOrEmpty(rowKey) ? string.Empty : rowKey;
                partitionKey = string.IsNullOrEmpty(partitionKey) ? string.Empty : partitionKey;
                keyQuery = string.Format("(PartitionKey='{0}', RowKey='{1}')", partitionKey, rowKey);
            }
            else if (nTop != null && nTop != 0)
            {
                nTopQuery = string.Format("$top={0}&", nTop);
                secretKey = addStr(secretKey, 1, nTopQuery);
            }          

            var url = string.Format("https://{0}.table.core.windows.net/{1}{2}/{3}", storageKey, tableID, keyQuery, secretKey);
            return url;
        }

        public static string GetUpdateQueryURL(string storageKey, string secretKey, string tableID, string rowKey, string partitionKey)
        {
            var keyQuery = string.Format("(PartitionKey='{0}', RowKey='{1}')", partitionKey, rowKey);

            var url = string.Format("https://{0}.table.core.windows.net/{1}{2}/{3}", storageKey, tableID, keyQuery, secretKey);
            return url;
        }

        public static string GetDeleteQueryURL(string storageKey, string secretKey, string tableID, string rowKey, string partitionKey)
        {
            var keyQuery = string.Format("(PartitionKey='{0}', RowKey='{1}')", partitionKey, rowKey);

            var url = string.Format("https://{0}.table.core.windows.net/{1}{2}/{3}", storageKey, tableID, keyQuery, secretKey);
            return url;
        }

        public static string GetTableInsertURL(string storageKey, string secretKey, string tableID)
        {
            var url = string.Format("https://{0}.table.core.windows.net/{1}/{2}", storageKey, tableID, secretKey);
            return url;
        }

        public static string GetTableSimpleURL(string storageKey, string secretKey)
        {
            var url = string.Format("https://{0}.table.core.windows.net/Tables/{1}", storageKey, secretKey);
            return url;
        }

        public static HttpRequestMessage GetHeader(AzureRequestType requestType, string url)
        {
            switch(requestType)
            {
                case AzureRequestType.GET_ALL_TABLES:
                case AzureRequestType.GET_TABLE_DATA:
                return new HttpRequestMessage
                {
                    Method = HttpMethod.Get,
                    RequestUri = new Uri(url),
                    Headers = { 
                        { HttpRequestHeader.Accept.ToString(), "application/json;odata=nometadata" }
                    }
                };
                case AzureRequestType.CREATE_TABLE:
                case AzureRequestType.INSERT_DATA:
                return new HttpRequestMessage
                {
                    Method = HttpMethod.Post,
                    RequestUri = new Uri(url),
                    Headers = { 
                        { HttpRequestHeader.Accept.ToString(), "application/json;odata=nometadata" }
                    }
                };
                case AzureRequestType.UPDATE_TABLE:
                return new HttpRequestMessage
                {
                    Method = HttpMethod.Put,
                    RequestUri = new Uri(url),
                    Headers = { 
                        { HttpRequestHeader.Accept.ToString(), "application/json;odata=nometadata" }
                    }
                };
                case AzureRequestType.DELETE_TABLE:
                return new HttpRequestMessage
                {
                    Method = HttpMethod.Delete,
                    RequestUri = new Uri(url),
                    Headers = { 
                        { HttpRequestHeader.Accept.ToString(), "application/json;odata=nometadata" },
                        {"If-Match", "*"}
                    }
                };
                case AzureRequestType.PATCH_TABLES:
                return new HttpRequestMessage
                {
                    Method = HttpMethod.Patch,
                    RequestUri = new Uri(url),
                    Headers = { 
                        { HttpRequestHeader.Accept.ToString(), "application/json;odata=nometadata" }
                    }
                };
                default:
                return null;
            }
        }

        private static string addStr(string str, int index, string stringToAdd)
        {
            return str.Insert(index, stringToAdd);
        }
    }

    public enum AzureRequestType
    {
        GET_TABLE_DATA = 0,
        INSERT_DATA = 1,
        CREATE_TABLE = 2,
        UPDATE_TABLE = 3,
        DELETE_TABLE = 4,
        GET_ALL_TABLES = 5,
        PATCH_TABLES = 6,
    }

    public class AzureTableRequestResult
    {
        public string RawResult {get; private set;}
        public AzureError Error { get; private set;}

        public AzureTableRequestResult(dynamic result)
        {
            if (result["odata.error"] != null)
            {
                Error = JsonConvert.DeserializeObject<AzureError>(result["odata.error"].ToString());
            }
            else
            {
                RawResult = result.ToString();
            }
        }

        public dynamic EmptyValue()
        {
            return new {
                value = new object[]{}
            };
        }
    }

    public class AzureError{
        public string code;
        public AzureErrorMessage message;
    }

    public class AzureErrorMessage
    {
        public string lang;
        public string value;
    }
}