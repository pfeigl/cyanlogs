using Cyan.Logs.Server.Hubs;
using Cyan.Logs.Server.Services;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Util;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddGrpc();
builder.Services.AddSignalR(options =>
{
    options.DisableImplicitFromServicesParameters = true;
});

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(corsBuilder =>
    {
        corsBuilder.SetIsOriginAllowed(origin => new Uri(origin).IsLoopback)
            .AllowCredentials()
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services.AddSingleton<Lucene.Net.Store.Directory>(FSDirectory.Open("index"));
builder.Services.AddSingleton(sc =>
{
    var directory = sc.GetRequiredService<Lucene.Net.Store.Directory>();
    
    const LuceneVersion appLuceneVersion = LuceneVersion.LUCENE_48;
    var analyzer = new StandardAnalyzer(appLuceneVersion);
    var indexConfig = new IndexWriterConfig(appLuceneVersion, analyzer);
    return new IndexWriter(directory, indexConfig);
});
builder.Services.AddSingleton(sc =>
{
    var indexWriter = sc.GetRequiredService<IndexWriter>();
    return new SearcherManager(indexWriter, true, null);

});

var app = builder.Build();

app.UseCors();

// Configure the HTTP request pipeline.
app.MapGrpcService<LogsService>();
app.MapGet("/", () => "Communication with gRPC endpoints must be made through a gRPC client");
app.MapHub<LogsHub>("/logs");

app.Run();