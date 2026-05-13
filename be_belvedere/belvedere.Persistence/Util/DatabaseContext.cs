using belvedere.Persistence.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;

namespace belvedere.Persistence.Util;

public sealed class DatabaseContext(DbContextOptions<DatabaseContext> options) : DbContext(options)
{
    public const string SchemaName = "belvedere";
    
    public DbSet<User> Users => Set<User>();
    public DbSet<Photo> Photos => Set<Photo>();
    public DbSet<Album> Albums => Set<Album>();
    public DbSet<ShareKey> ShareKeys => Set<ShareKey>();
    public DbSet<PhotoReaction> PhotoReactions => Set<PhotoReaction>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasDefaultSchema(SchemaName);

        ConfigureUser(modelBuilder);
        ConfigurePhoto(modelBuilder);
        ConfigureAlbum(modelBuilder);
        ConfigureShareKey(modelBuilder);
        ConfigurePhotoReaction(modelBuilder);
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        base.ConfigureConventions(configurationBuilder);

        configurationBuilder.Conventions.Remove<TableNameFromDbSetConvention>();
    }

    private static void ConfigureUser(ModelBuilder modelBuilder)
    {
        EntityTypeBuilder<User> user = modelBuilder.Entity<User>();
        user.HasKey(u => u.Id);
        user.Property(u => u.Id).ValueGeneratedOnAdd();
        user.Property(u => u.ExternalSub).HasMaxLength(255);
        user.Property(u => u.Email).HasMaxLength(320);

        user.HasMany(u => u.Photos)
            .WithOne(p => p.User)
            .HasForeignKey(p => p.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        user.HasMany(u => u.Albums)
            .WithOne(a => a.User)
            .HasForeignKey(a => a.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        user.HasMany(u => u.Reactions)
            .WithOne(r => r.User)
            .HasForeignKey(r => r.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }

    private static void ConfigurePhoto(ModelBuilder modelBuilder)
    {
        EntityTypeBuilder<Photo> photo = modelBuilder.Entity<Photo>();
        photo.HasKey(p => p.Id);
        photo.Property(p => p.Id).ValueGeneratedOnAdd();
        photo.Property(p => p.Title).HasMaxLength(255);
        photo.Property(p => p.Description).HasMaxLength(2000);
        photo.Property(p => p.StorageKey).HasMaxLength(1024);
        photo.Property(p => p.ThumbKey).HasMaxLength(1024);
        photo.Property(p => p.MimeType).HasMaxLength(255);
        photo.Property(p => p.Make).HasMaxLength(255);
        photo.Property(p => p.Model).HasMaxLength(255);
        photo.Property(p => p.CountryCode).HasMaxLength(3);
        photo.Property(p => p.City).HasMaxLength(255);

        photo.HasMany(p => p.Reactions)
             .WithOne(r => r.Photo)
             .HasForeignKey(r => r.PhotoId)
             .OnDelete(DeleteBehavior.Cascade);
    }

    private static void ConfigureAlbum(ModelBuilder modelBuilder)
    {
        EntityTypeBuilder<Album> album = modelBuilder.Entity<Album>();
        album.HasKey(a => a.Id);
        album.Property(a => a.Id).ValueGeneratedOnAdd();
        album.Property(a => a.Title).HasMaxLength(255);
        album.Property(a => a.Description).HasMaxLength(2000);

        album.HasOne(a => a.CoverPhoto)
             .WithMany()
             .HasForeignKey(a => a.CoverPhotoId)
             .OnDelete(DeleteBehavior.SetNull);

        album.HasMany(a => a.Photos)
             .WithMany(p => p.Albums)
             .UsingEntity<Dictionary<string, object>>(
                 "AlbumPhoto",
                 right => right.HasOne<Photo>()
                               .WithMany()
                               .HasForeignKey("PhotoId")
                               .OnDelete(DeleteBehavior.Cascade),
                 left => left.HasOne<Album>()
                             .WithMany()
                             .HasForeignKey("AlbumId")
                             .OnDelete(DeleteBehavior.Cascade),
                 join =>
                 {
                     join.ToTable("AlbumPhoto");
                     join.HasKey("AlbumId", "PhotoId");
                 });
    }

    private static void ConfigureShareKey(ModelBuilder modelBuilder)
    {
        EntityTypeBuilder<ShareKey> shareKey = modelBuilder.Entity<ShareKey>();
        shareKey.HasKey(s => s.Id);
        shareKey.Property(s => s.Id).ValueGeneratedOnAdd();

        shareKey.Property(s => s.Key).HasMaxLength(128);
        shareKey.Property(s => s.PasswordHash).HasMaxLength(512);
        shareKey.HasIndex(s => s.Key).IsUnique();
        shareKey.HasIndex(s => s.AlbumId);
        shareKey.HasIndex(s => s.PhotoId);

        shareKey.HasOne(s => s.Album)
                .WithMany(a => a.Shares)
                .HasForeignKey(s => s.AlbumId)
                .OnDelete(DeleteBehavior.Cascade);

        shareKey.HasOne(s => s.Photo)
                .WithMany(p => p.Shares)
                .HasForeignKey(s => s.PhotoId)
                .OnDelete(DeleteBehavior.Cascade);

        shareKey.ToTable(t => t.HasCheckConstraint(
            "CK_ShareKey_ExactlyOneTarget",
            "(\"AlbumId\" IS NOT NULL AND \"PhotoId\" IS NULL) OR (\"AlbumId\" IS NULL AND \"PhotoId\" IS NOT NULL)"));
    }

    private static void ConfigurePhotoReaction(ModelBuilder modelBuilder)
    {
        EntityTypeBuilder<PhotoReaction> photoReaction = modelBuilder.Entity<PhotoReaction>();
        photoReaction.HasKey(r => r.Id);
        photoReaction.Property(r => r.Id).ValueGeneratedOnAdd();
    }
}
