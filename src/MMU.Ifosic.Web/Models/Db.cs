using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace MMU.Ifosic.Models;

public class Db : DbContext
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Node> Nodes => Set<Node>();

    public Db(DbContextOptions o) : base(o)
    {
    }

    protected override void OnModelCreating(ModelBuilder b)
    {
        // set string to varchar 255
        var f = b.Model.GetEntityTypes().SelectMany(s => s.GetProperties()).Where(w => w.ClrType == typeof(string));
        foreach (var p in f)
        {
            if (p.GetMaxLength() == null)
                p.SetMaxLength(256);
        }

        b.Entity<User>().HasIndex(b => b.Email).IsUnique();
        b.Entity<User>().HasIndex(b => b.Phone).IsUnique();
        // init roles
        var roles = new string[] {
            "Admin",
            "Researcher",
        }.Select((v, i) => new Role { Id = i + 1, Name = v }).ToList();
        b.Entity<Role>().HasData(roles);
        // init user
        var (pass, salt) = "password".Encrypt();
        b.Entity<User>().HasData(new User { Id = 1, Name = "Ir. Prof. Dr. Mohd Ridzuan Bin Mokhtar", Email = "ridz@mmu.edu.my",
            Password = pass, Salt = salt, IsActive = true, CreatedById = 1, UpdatedById = 1 });
        b.Entity<User>().HasMany(s => s.Roles).WithMany(s => s.Users).UsingEntity(j => j.HasData(new { RolesId = 1, UsersId = 1 }));
        // init node
        b.Entity<Node>().HasData(new Node { Id = 1, Title = "Term and Condition", Body = "Term and Condition", IsPublished = true, UserId = 1 });
    }
}