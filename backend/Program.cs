using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using WhaleSpotting;
using WhaleSpotting.Models.Data;

var builder = WebApplication.CreateBuilder(args);

// Add this right after the builder is created
//var builder = WebApplication.CreateBuilder(args);

// Debug: Check if any connection string exists
Console.WriteLine($"Connection string exists: {builder.Configuration.GetConnectionString("Postgres") != null}");
if (builder.Configuration.GetConnectionString("Postgres") != null)
{
    Console.WriteLine($"Connection string value: {builder.Configuration.GetConnectionString("Postgres")}");
}

builder.Configuration.AddEnvironmentVariables();

// Debug: Check if environment variables exist
Console.WriteLine($"DB_HOST_KEY exists: {builder.Configuration["DB_HOST_KEY"] != null}");
Console.WriteLine($"DB_USERNAME_KEY exists: {builder.Configuration["DB_USERNAME_KEY"] != null}");
Console.WriteLine($"DB_PASSWORD_KEY exists: {builder.Configuration["DB_PASSWORD_KEY"] != null}");

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(builder.Configuration["Cors:Frontend"]!).AllowAnyMethod().AllowAnyHeader();
    });
});

builder
    .Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    });

builder.Services.AddDbContext<WhaleSpottingContext>(options =>
{
    var connectionTemplate = builder.Configuration.GetConnectionString("Postgres");
    if (string.IsNullOrEmpty(connectionTemplate))
    {
        throw new InvalidOperationException("Connection string 'Postgres' is missing or empty in configuration");
    }

    // Format with values from configuration
    var formattedConnectionString = string.Format(
        System.Globalization.CultureInfo.InvariantCulture,
        connectionTemplate,
        builder.Configuration["DB_HOST_KEY"],
        builder.Configuration["DB_USERNAME_KEY"],
        builder.Configuration["DB_PASSWORD_KEY"]
    );

    Console.WriteLine($"Formatted connection string: {formattedConnectionString}");

    options.UseNpgsql(formattedConnectionString);
});

builder.Services.AddIdentity<User, Role>().AddEntityFrameworkStores<WhaleSpottingContext>();

builder
    .Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.Default.GetBytes(builder.Configuration["Jwt:Secret"]!)
            ),
        };
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition(
        "Bearer",
        new OpenApiSecurityScheme()
        {
            Name = "Authorization",
            BearerFormat = "JWT",
            Scheme = "Bearer",
            Description = "Specify the authorization token",
            In = ParameterLocation.Header,
            Type = SecuritySchemeType.Http,
        }
    );
    options.AddSecurityRequirement(
        new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
                },
                []
            }
        }
    );
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
