using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace MMU.Ifosic.Models;

public class Db : DbContext
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Node> Nodes => Set<Node>();
    public DbSet<Fiber> Fibers => Set<Fiber>();
    public DbSet<Project> Projects => Set<Project>();

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
        b.Entity<User>().HasData(
            new User { Id = 1, Email = "anton@horizon3.my", Name = "Dr. Anton Heryanto Hasan", Password = pass, Salt = salt, IsActive = true, CreatedById = 1, UpdatedById = 1 },
            new User { Id = 2, Email = "ridz@mmu.edu.my", Name = "Ir. Prof. Dr. Mohd Ridzuan Bin Mokhtar", Password = pass, Salt = salt, IsActive = true, CreatedById = 1, UpdatedById = 1 },
            new User { Id = 3, Email = "khazaimatol@dkss.com.my", Name = "Dr Khazaimatol Shima Subari", Password = pass, Salt = salt, IsActive = true, CreatedById = 1, UpdatedById = 1 }
        );
        b.Entity<User>().HasMany(s => s.Roles).WithMany(s => s.Users).UsingEntity(j => j.HasData(new { RolesId = 1, UsersId = 1 }));
        // init node
        b.Entity<Node>().HasData(new Node { Id = 1, Title = "Term and Condition", Body = "Term and Condition", IsPublished = true, UserId = 1 });

        b.Entity<Fiber>().HasData(
            new Fiber { Id = 1, Coefficient = 0.1000, Name = "Fiber 1" },
            new Fiber { Id = 2, Coefficient = 0.6340, Name = "Fiber 2" },
            new Fiber { Id = 3, Coefficient = 0.5477, Name = "Fiber 3" },
            new Fiber { Id = 4, Coefficient = 0.6602, Name = "Fiber 4" },
            new Fiber { Id = 5, Coefficient = 0.5682, Name = "Fiber 5" }
        );
        b.Entity<Project>().HasData(new Project { Id = 1, Name = "Set 1", CreatedById = 1, UpdatedById = 1 });
        b.Entity<Project>().HasMany(s => s.Fibers).WithMany(s => s.Projects).UsingEntity(j => j.HasData(
            new { FibersId = 1, ProjectsId = 1 },
            new { FibersId = 2, ProjectsId = 1 },
            new { FibersId = 3, ProjectsId = 1 },
            new { FibersId = 4, ProjectsId = 1 },
            new { FibersId = 5, ProjectsId = 1 }
        ));

    }
}