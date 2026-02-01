using Microsoft.EntityFrameworkCore;
using new_user_app.Models;
using new_user_app.DTO;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using new_user_app.DbContexts;
using NSwag;
using NSwag.Generation.Processors.Security;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddControllers();
builder.Services.AddOpenApi();

// Configure database connection
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<TodoDb>(options =>
    options.UseMySql(connectionString, new MySqlServerVersion(new Version(8, 0, 21))));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApiDocument(config =>
{
    config.DocumentName = "TodoAPI";
    config.Title = "TodoAPI v1";
    config.Version = "v1";
    
    // Add JWT Bearer authentication to Swagger
    config.AddSecurity("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Enter your token below (the 'Bearer ' prefix will be added automatically).",
        Name = "Authorization",
        In = OpenApiSecurityApiKeyLocation.Header,
        Type = OpenApiSecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });
    
    config.OperationProcessors.Add(new AspNetCoreOperationSecurityScopeProcessor());
});

// Configure JWT Authentication
// Use direct configuration access with colon notation (standard ASP.NET Core way)
var jwtKey = builder.Configuration["Jwt:Key"];
var jwtIssuer = builder.Configuration["Jwt:Issuer"];
var jwtAudience = builder.Configuration["Jwt:Audience"];

// Debug: Check what configuration values we're getting
if (string.IsNullOrEmpty(jwtKey))
{
    // Try alternative access methods
    var jwtSection = builder.Configuration.GetSection("Jwt");
    jwtKey = jwtSection["Key"];
    
    if (string.IsNullOrEmpty(jwtKey))
    {
        // Get all configuration keys that start with "Jwt" for debugging
        var allConfigKeys = builder.Configuration.AsEnumerable()
            .Where(kvp => kvp.Key.StartsWith("Jwt", StringComparison.OrdinalIgnoreCase))
            .Select(kvp => $"{kvp.Key}={kvp.Value}")
            .ToList();
        
        var configSources = string.Join(", ", allConfigKeys.Any() ? allConfigKeys : new[] { "none found" });
        
        throw new InvalidOperationException(
            $"JWT Key is not configured. " +
            $"Configuration sources checked: {configSources}. " +
            $"Please ensure 'Jwt:Key' exists in appsettings.json or appsettings.Development.json");
    }
}

if (string.IsNullOrEmpty(jwtIssuer))
{
    jwtIssuer = builder.Configuration.GetSection("Jwt")["Issuer"];
    if (string.IsNullOrEmpty(jwtIssuer))
    {
        throw new InvalidOperationException("JWT Issuer is not configured. Please add 'Jwt:Issuer' to appsettings.json");
    }
}

if (string.IsNullOrEmpty(jwtAudience))
{
    jwtAudience = builder.Configuration.GetSection("Jwt")["Audience"];
    if (string.IsNullOrEmpty(jwtAudience))
    {
        throw new InvalidOperationException("JWT Audience is not configured. Please add 'Jwt:Audience' to appsettings.json");
    }
}

if (string.IsNullOrWhiteSpace(jwtKey) || jwtKey.Length < 32)
{
    throw new InvalidOperationException("JWT Key must be at least 32 characters long. Please update 'Jwt:Key' in appsettings.json");
}

var key = Encoding.UTF8.GetBytes(jwtKey);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization();

var app = builder.Build();

// Ensure database is created
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<TodoDb>();
    db.Database.EnsureCreated();
}

if (app.Environment.IsDevelopment())
{
    app.UseOpenApi();
    app.UseSwaggerUi(config =>
    {
        config.DocumentTitle = "TodoAPI";
        config.Path = "/swagger";
        config.DocumentPath = "/swagger/{documentName}/swagger.json";
        config.DocExpansion = "list";
        config.CustomJavaScriptPath = "/swagger-login.js";
    });
    // Serve static files for the custom JavaScript (if needed)
    app.UseStaticFiles();
}

app.UseAuthentication();
app.UseAuthorization();

// Authentication endpoints
var auth = app.MapGroup("/auth");

auth.MapPost("/register", Register);
auth.MapPost("/login", Login);

var todoItems = app.MapGroup("/todoitems").RequireAuthorization();

todoItems.MapGet("", GetAllTodos);

todoItems.MapGet("{id}", GetTodo);

todoItems.MapPost("", CreateTodo);

todoItems.MapGet("/complete", GetCompleteTodos);

todoItems.MapPut("{id}", UpdateTodo);

todoItems.MapDelete("{id}", DeleteTodo);


static async Task<IResult> GetAllTodos(TodoDb db)
{
    return TypedResults.Ok(await db.Todos.Select(x => new TodoDTO(x)).ToArrayAsync());
}

static async Task<IResult> GetCompleteTodos(TodoDb db) {
    return TypedResults.Ok(await db.Todos.Where(t => t.IsComplete).Select(x => new TodoDTO(x)).ToListAsync());
}

static async Task<IResult> GetTodo(int id, TodoDb db)
{
    return await db.Todos.FindAsync(id)
        is Todo todo
            ? TypedResults.Ok(new TodoDTO(todo))
            : TypedResults.NotFound();
}

static async Task<IResult> CreateTodo(TodoDTO todoItemDTO, TodoDb db)
{
    var todoItem = new Todo
    {
        IsComplete = todoItemDTO.IsComplete,
        Name = todoItemDTO.Name
    };

    db.Todos.Add(todoItem);
    await db.SaveChangesAsync();

    todoItemDTO = new TodoDTO(todoItem);

    return TypedResults.Created($"/todoitems/{todoItem.Id}", todoItemDTO);
}

static async Task<IResult> UpdateTodo(int id, TodoDTO todoItemDTO, TodoDb db)
{
    var todo = await db.Todos.FindAsync(id);

    if (todo is null) return TypedResults.NotFound();

    todo.Name = todoItemDTO.Name;
    todo.IsComplete = todoItemDTO.IsComplete;

    await db.SaveChangesAsync();

    return TypedResults.NoContent();
}

static async Task<IResult> DeleteTodo(int id, TodoDb db)
{
    if (await db.Todos.FindAsync(id) is Todo todo)
    {
        db.Todos.Remove(todo);
        await db.SaveChangesAsync();
        return TypedResults.NoContent();
    }

    return TypedResults.NotFound();
}

// Authentication helper methods
static string HashPassword(string password)
{
    using var sha256 = SHA256.Create();
    var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
    return Convert.ToBase64String(hashedBytes);
}

static bool VerifyPassword(string password, string passwordHash)
{
    var hashOfInput = HashPassword(password);
    return hashOfInput == passwordHash;
}

static string GenerateJwtToken(User user, IConfiguration configuration)
{
    // Use direct configuration access with colon notation
    var jwtKey = configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT Key is not configured");
    var jwtIssuer = configuration["Jwt:Issuer"] ?? throw new InvalidOperationException("JWT Issuer is not configured");
    var jwtAudience = configuration["Jwt:Audience"] ?? throw new InvalidOperationException("JWT Audience is not configured");
    var expirationMinutesStr = configuration["Jwt:ExpirationInMinutes"] ?? "60";
    
    if (!int.TryParse(expirationMinutesStr, out var expirationMinutes))
    {
        expirationMinutes = 60; // Default to 60 minutes if parsing fails
    }

    var key = Encoding.UTF8.GetBytes(jwtKey);

    var claims = new[]
    {
        new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
        new Claim(ClaimTypes.Name, user.Username),
        new Claim(ClaimTypes.Email, user.Email)
    };

    var tokenDescriptor = new SecurityTokenDescriptor
    {
        Subject = new ClaimsIdentity(claims),
        Expires = DateTime.UtcNow.AddMinutes(expirationMinutes),
        Issuer = jwtIssuer,
        Audience = jwtAudience,
        SigningCredentials = new SigningCredentials(
            new SymmetricSecurityKey(key),
            SecurityAlgorithms.HmacSha256Signature)
    };

    var tokenHandler = new JwtSecurityTokenHandler();
    var token = tokenHandler.CreateToken(tokenDescriptor);
    return tokenHandler.WriteToken(token);
}

static async Task<IResult> Register(RegisterDTO registerDTO, TodoDb db, IConfiguration configuration)
{
    // Check if user already exists
    if (await db.Users.AnyAsync(u => u.Username == registerDTO.Username || u.Email == registerDTO.Email))
    {
        return TypedResults.BadRequest(new { message = "Username or email already exists" });
    }

    var user = new User
    {
        Username = registerDTO.Username,
        Email = registerDTO.Email,
        PasswordHash = HashPassword(registerDTO.Password)
    };

    db.Users.Add(user);
    await db.SaveChangesAsync();

    var token = GenerateJwtToken(user, configuration);

    return TypedResults.Ok(new AuthResponseDTO
    {
        Token = token,
        Username = user.Username,
        Email = user.Email
    });
}

static async Task<IResult> Login(LoginDTO loginDTO, TodoDb db, IConfiguration configuration)
{
    var user = await db.Users.FirstOrDefaultAsync(u => u.Username == loginDTO.Username);

    if (user == null || !VerifyPassword(loginDTO.Password, user.PasswordHash))
    {
        return TypedResults.Unauthorized();
    }

    var token = GenerateJwtToken(user, configuration);

    return TypedResults.Ok(new AuthResponseDTO
    {
        Token = token,
        Username = user.Username,
        Email = user.Email
    });
}

app.Run();
