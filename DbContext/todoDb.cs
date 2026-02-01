using new_user_app.Models;

using Microsoft.EntityFrameworkCore;

namespace new_user_app.DbContexts
{
    public class TodoDb : DbContext
    {
        public TodoDb(DbContextOptions<TodoDb> options)
            : base(options)
        {
        }

        public DbSet<Todo> Todos { get; set; }
        public DbSet<User> Users { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Option 1: Apply snake_case naming convention globally
            NamingConventions.ApplySnakeCaseNaming(modelBuilder);

            // Option 2: Apply lowercase naming convention (uncomment to use)
            // NamingConventions.ApplyLowercaseNaming(modelBuilder);

            // Option 3: Use Fluent API for custom per-entity configuration
            // Uncomment the examples below to use custom names per entity:

            // modelBuilder.Entity<Todo>(entity =>
            // {
            //     entity.ToTable("todos"); // Custom table name
            //     entity.Property(e => e.Id).HasColumnName("id");
            //     entity.Property(e => e.Name).HasColumnName("name").HasMaxLength(255);
            //     entity.Property(e => e.IsComplete).HasColumnName("is_complete");
            //     
            //     // Configure primary key
            //     entity.HasKey(e => e.Id).HasName("pk_todos");
            //     
            //     // Add index
            //     entity.HasIndex(e => e.Name).HasDatabaseName("ix_todos_name");
            // });

            // modelBuilder.Entity<User>(entity =>
            // {
            //     entity.ToTable("users");
            //     entity.Property(e => e.Id).HasColumnName("id");
            //     entity.Property(e => e.Username)
            //         .HasColumnName("username")
            //         .HasMaxLength(100)
            //         .IsRequired();
            //     entity.Property(e => e.Email)
            //         .HasColumnName("email")
            //         .HasMaxLength(255)
            //         .IsRequired();
            //     entity.Property(e => e.PasswordHash)
            //         .HasColumnName("password_hash")
            //         .HasMaxLength(255)
            //         .IsRequired();
            //     
            //     // Add unique constraint
            //     entity.HasIndex(e => e.Username).IsUnique().HasDatabaseName("ix_users_username_unique");
            //     entity.HasIndex(e => e.Email).IsUnique().HasDatabaseName("ix_users_email_unique");
            // });
        }
    }
}
