using Goatbot.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Goatbot.Data;

public class BirdDbContext : DbContext
{
    private IConfiguration _config;

    public DbSet<Upvotes> Upvotes { get; set; }
    public DbSet<VoidMutes> VoidMutes { get; set; }
    
    public BirdDbContext(IConfiguration config)
    {
        _config = config;
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        => optionsBuilder.UseNpgsql($"Host={_config.GetValue<string>("Database:Host")};Database={_config.GetValue<string>("Database:Database")};Username={_config.GetValue<string>("Database:Username")};Password={_config.GetValue<string>("Database:Password")}; Include Error Detail=true");
}