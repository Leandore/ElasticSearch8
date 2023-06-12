﻿using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.QueryDsl;
using Elastic.Transport;
using System.Text;
using System.Text.Json;

static ElasticsearchClient CreateEsClient()
{
    var url = Environment.GetEnvironmentVariable("ElasticsearchServiceUrl");
    var username = Environment.GetEnvironmentVariable("ElasticsearchUsername");
    var password = Environment.GetEnvironmentVariable("ElasticsearchPassword");
    var indexName = Environment.GetEnvironmentVariable("ElasticsearchIndexName");

    var settings = new ElasticsearchClientSettings(new Uri(url!))
        .Authentication(new BasicAuthentication(username!, password!))
        .DefaultMappingFor<AssetSearchDo>(deviceMapping => deviceMapping
            .IndexName(indexName!)
            .IdProperty(dev => dev.Id)
        )
        .ServerCertificateValidationCallback((_, _, _, _) => true);

    var client = new ElasticsearchClient(settings);
    return client;
}

static void DemonstrateMissingWildcardQueryValue(SearchRequest<AssetSearchDo> searchRequest)
{
    BoolQuery? boolQuery = null;
    WildcardQuery? wildcardQuery = null;
    searchRequest.Query?.TryGet<BoolQuery?>(out boolQuery);
    boolQuery.Filter?.ElementAt(0).TryGet<WildcardQuery>(out wildcardQuery);

    var missingwildcardQuery = wildcardQuery.Value;

    // now inspect value of missingwildcardQuery, expected "*" but actual is null
    System.Diagnostics.Debugger.Break();
}

var client = CreateEsClient();

// simple WildcardQuery generated by our WebAPI clients
var simpleWildcardQuery = new
{
    Query = new
    {
        @bool = new
        {
            filter = new
            {
                wildcard = new
                {
                    displayName = "*"
                }
            }
        }
    }
};
var searchQuery = JsonSerializer.Serialize(simpleWildcardQuery, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
using var stream = new MemoryStream(Encoding.UTF8.GetBytes(searchQuery));

var searchRequest = client.RequestResponseSerializer.Deserialize<SearchRequest<AssetSearchDo>>(stream);

DemonstrateMissingWildcardQueryValue(searchRequest);

var searchResponse = await client.SearchAsync<AssetSearchDo>(searchRequest).ConfigureAwait(false);
Console.WriteLine(searchResponse.ToString());
Console.ReadLine();

public sealed class AssetSearchDo
{
    public Guid Id { get; set; }
    public required string DisplayName { get; set; }
}
