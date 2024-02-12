using Microsoft.EntityFrameworkCore;

public class db : DbContext
{

    public db(DbContextOptions<db> options) : base(options)
    {

    }

    public DbSet<ScrapeDetails> ScrapeDetails { get; set; }
}