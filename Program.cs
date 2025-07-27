using Complaint_Analyzer_using_ES.Cache;
using Complaint_Analyzer_using_ES.ICache;
using Complaint_Analyzer_using_ES.IServices;
using Complaint_Analyzer_using_ES.Services;
using ComplaintAnalyzer;
using Nest;
using StackExchange.Redis;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp", builder =>
    {
        builder.WithOrigins("http://localhost:3000")
               .AllowAnyHeader()
               .AllowAnyMethod();
    });
});

builder.Services.AddSwaggerGen(c =>
{
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    c.IncludeXmlComments(xmlPath); 
});
// Elasticsearch client setup
builder.Services.AddSingleton<IElasticClient>(sp =>
{
    var settings = new ConnectionSettings(new Uri("http://localhost:9800"))
        .DefaultIndex("complaints");
    return new ElasticClient(settings);
});
builder.Services.AddSingleton<IConnectionMultiplexer>(
    ConnectionMultiplexer.Connect(builder.Configuration.GetConnectionString("RedisConnection")));

// Register ES service with interface
builder.Services.AddScoped<IElasticSearchService, ElasticSearchService>();
builder.Services.AddScoped<IRedisCacheService, RedisCacheService>();
builder.Services.AddScoped<IElasticSearchCache, ElasticSearchCache>();


// Add controllers
builder.Services.AddControllers();

var app = builder.Build();
app.UseCors("AllowReactApp");

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
