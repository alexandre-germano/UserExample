using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDBContext>(options =>
{
  options.UseInMemoryDatabase("MyInMemoryDataBase");
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
  app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapGet("/users/{userName}", async (string userName, AppDBContext dbContext) =>
{  
  // Violates Single Responsibility Principle
  // The endpoint directly interacts with the database, instead of delegating logic to a service.
  if (string.IsNullOrEmpty(userName))
  {
    return Results.BadRequest("User name is required!");
  }

 /*This API is safe from SQL Injection because it uses Entity Framework Core,which generates parameterized queries under the hood.  
   
  When calling `FirstOrDefaultAsync(x => x.UserName == userName)`, 
  EF Core translates this LINQ expression into a SQL query that uses parameters, like:
  
  SELECT * FROM Users WHERE UserName = @p0  

  The parameter @p0 is automatically bound to the `userName` value safely, preventing malicious inputs from being executed as SQL commands.
 */
    
  var user = 
    await dbContext.Users.FirstOrDefaultAsync(x => x.UserName == userName);

  if (user is null)
  {
    return Results.NotFound();
  }

  return Results.Ok(user);

})
.WithName("GetUser");

app.MapPost("/users", async (User newUser, AppDBContext dbContext) =>
{
  // Violates SRP - The API should not handle business logic directly.  
  if (string.IsNullOrEmpty(newUser.UserName))
  {
    return Results.BadRequest("User name is required!");
  }  
  
  await dbContext.Users.AddAsync(newUser);
  await dbContext.SaveChangesAsync();

  return Results.Created($"/users/{newUser.UserName}", newUser);

})
.WithName("AddUser");

app.Run();

//This class should not be in the same file as the API setup.
//A repository design pattern should be implemented
public class AppDBContext : DbContext
{
  public AppDBContext(DbContextOptions<AppDBContext> options) : base(options)
  {
  }

  public DbSet<User> Users { get; set; }
}


// We are aware that the `User` record is being used simultaneously as a DTO, an EF Core entity, and a domain model.
public record User(Guid Id, string UserName, string Email)
{
}
