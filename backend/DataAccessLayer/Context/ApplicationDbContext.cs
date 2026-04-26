using DataAccessLayer.Models;
using Microsoft.EntityFrameworkCore;

namespace DataAccessLayer.Context;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }
    public DbSet<Address> Addresses { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<Report> Reports { get; set; }
    public DbSet<Restaurant> Restaurants { get; set; }
    public DbSet<Review> Reviews { get; set; }
    public DbSet<RestaurantPhoto> RestaurantPhotos { get; set; }
    public DbSet<ReviewPhoto> ReviewPhotos { get; set; }
    public DbSet<Category>  Categories { get; set; }
    public DbSet<Order> Orders { get; set; }
    public DbSet<QRInformation> QRInformations { get; set; }
    public DbSet<UserFavorite> UserFavorites { get; set; }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        modelBuilder.Entity<Report>()
            .HasOne(r => r.User)
            .WithMany(u => u.Reports)
            .HasForeignKey(r => r.UserId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Report>()
            .HasOne(r => r.Review)
            .WithMany(v => v.Reports)
            .HasForeignKey(r => r.ReviewId);

        modelBuilder.Entity<Review>()
            .HasOne(r => r.User)
            .WithMany(u => u.Reviews)
            .HasForeignKey(r => r.UserId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Review>()
            .HasOne(r => r.Restaurant)
            .WithMany(rs => rs.Reviews)
            .HasForeignKey(r => r.RestaurantId);
        modelBuilder.Entity<RestaurantPhoto>()
            .HasOne(rp => rp.Restaurant)
            .WithMany(r => r.RestaurantPhotos)
            .HasForeignKey(rp => rp.RestaurantId);

        modelBuilder.Entity<ReviewPhoto>()
            .HasOne(rp => rp.Review)
            .WithMany(r => r.Photos)
            .HasForeignKey(rp => rp.ReviewId);
        modelBuilder.Entity<User>()
            .HasOne(u => u.Address)
            .WithOne()
            .HasForeignKey<User>(u => u.AddressId)
            .OnDelete(DeleteBehavior.Cascade); 
        modelBuilder.Entity<Restaurant>()
            .HasOne(u => u.Address)
            .WithOne()
            .HasForeignKey<Restaurant>(u => u.AddressId)
            .OnDelete(DeleteBehavior.Cascade); 
        modelBuilder.Entity<Restaurant>()
            .HasOne(r => r.User)
            .WithMany(u => u.Restaurants)
            .HasForeignKey(r => r.UserId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Restaurant>()
            .HasOne(r => r.Category)
            .WithMany(u => u.Restaurants)
            .HasForeignKey(r => r.CateId);
        modelBuilder.Entity<Order>()
            .HasOne(o => o.User)
            .WithMany(u => u.Orders)
            .HasForeignKey(o => o.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Order>()
            .HasOne(o => o.Restaurant)
            .WithMany(r => r.Orders)
            .HasForeignKey(o => o.RestaurantId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<QRInformation>()
            .HasOne(qr => qr.User)
            .WithMany()
            .HasForeignKey(qr => qr.UserId)
            .OnDelete(DeleteBehavior.Restrict); 

        modelBuilder.Entity<QRInformation>()
            .HasOne(qr => qr.Restaurant)
            .WithMany()
            .HasForeignKey(qr => qr.RestaurantId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<UserFavorite>()
            .HasOne(uf => uf.User)
            .WithMany()
            .HasForeignKey(uf => uf.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<UserFavorite>()
            .HasOne(uf => uf.Restaurant)
            .WithMany()
            .HasForeignKey(uf => uf.RestaurantId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}