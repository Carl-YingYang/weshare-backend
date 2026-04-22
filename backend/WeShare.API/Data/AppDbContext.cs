using Microsoft.EntityFrameworkCore;
using WeShare.API.Models;

namespace WeShare.API.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users { get; set; }
    public DbSet<Post> Posts { get; set; }
    public DbSet<Friendship> Friendships { get; set; }

    // MGA BAGONG TABLES
    public DbSet<PostLike> PostLikes { get; set; }
    public DbSet<Comment> Comments { get; set; }
    public DbSet<Notification> Notifications { get; set; }
    public DbSet<Message> Messages { get; set; }



    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // 1. FRIENDSHIPS
        modelBuilder.Entity<Friendship>()
            .HasOne(f => f.Requester).WithMany().HasForeignKey(f => f.RequesterId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Friendship>()
            .HasOne(f => f.Receiver).WithMany().HasForeignKey(f => f.ReceiverId).OnDelete(DeleteBehavior.Restrict);

        // 2. POSTLIKES (Composite Key + Restrict User path)
        modelBuilder.Entity<PostLike>()
            .HasKey(pl => new { pl.PostId, pl.UserId });

        modelBuilder.Entity<PostLike>()
            .HasOne(pl => pl.User).WithMany().HasForeignKey(pl => pl.UserId).OnDelete(DeleteBehavior.Restrict);

        // 3. MESSAGES
        modelBuilder.Entity<Message>()
            .HasOne(m => m.Sender).WithMany().HasForeignKey(m => m.SenderId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Message>()
            .HasOne(m => m.Receiver).WithMany().HasForeignKey(m => m.ReceiverId).OnDelete(DeleteBehavior.Restrict);

        // 4. COMMENTS (Crucial Fix for your error)
        modelBuilder.Entity<Comment>()
            .HasOne(c => c.User).WithMany().HasForeignKey(c => c.UserId).OnDelete(DeleteBehavior.Restrict); // 🚨 FIX HERE

        modelBuilder.Entity<Comment>()
            .HasOne(c => c.ParentComment).WithMany(c => c.Replies).HasForeignKey(c => c.ParentCommentId).OnDelete(DeleteBehavior.Restrict);
    }
}