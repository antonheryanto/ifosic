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
        //b.Entity<User>().HasIndex(b => b.Phone).IsUnique();
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
        b.Entity<User>().HasMany(s => s.Roles).WithMany(s => s.Users).UsingEntity(j => j.HasData(
            new { RolesId = 1, UsersId = 1 },
			new { RolesId = 1, UsersId = 2 },
			new { RolesId = 1, UsersId = 3 }
		));
        // init node
        b.Entity<Node>().HasData(new Node { Id = 1, Title = "Term and Condition", Body = "Term and Condition", IsPublished = true, UserId = 1 });
        var projects = new List<Project>
        {
            new Project { Id = 1, Name = "Set 1", CreatedById = 1, UpdatedById = 1, NumberOfFiber = 5 },
            new Project { Id = 2, Name = "Set 2", CreatedById = 1, UpdatedById = 1, NumberOfFiber = 6 },
            new Project { Id = 3, Name = "Set 3", CreatedById = 1, UpdatedById = 1, NumberOfFiber = 6 }
        };
        b.Entity<Project>().HasData(projects);
        var fibers = new List<Fiber>();
		for (int j = 0, k = 1; j < projects.Count; j++)
		{
            for (int i = 0; i < projects[j].NumberOfFiber; i++, k++)
                fibers.Add(new Fiber { Id = k, Name = $"Fiber {i}", ProjectId = projects[j].Id });
        }

        b.Entity<Fiber>().HasData(fibers);
    }
}