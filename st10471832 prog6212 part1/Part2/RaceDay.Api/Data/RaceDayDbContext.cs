using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Models;

namespace RaceDay.Api.Data;

public class RaceDayDbContext : DbContext
{
    public RaceDayDbContext(DbContextOptions<RaceDayDbContext> options) : base(options)
    {
    }

    public DbSet<Organiser> Organisers => Set<Organiser>();
    public DbSet<Participant> Participants => Set<Participant>();
    public DbSet<Event> Events => Set<Event>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Enrolment> Enrolments => Set<Enrolment>();
    public DbSet<Result> Results => Set<Result>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Organiser>(e =>
        {
            e.ToTable("Organisers");
            e.HasKey(x => x.OrganiserID);
            e.Property(x => x.FullName).HasMaxLength(100).IsUnicode(false).IsRequired();
            e.Property(x => x.Email).HasMaxLength(150).IsUnicode(false).IsRequired();
            e.HasIndex(x => x.Email).IsUnique();
            e.Property(x => x.PasswordHash).HasMaxLength(255).IsUnicode(false).IsRequired();
            e.Property(x => x.Phone).HasMaxLength(20).IsUnicode(false);
            e.Property(x => x.CreatedAt).HasColumnType("datetime").HasDefaultValueSql("GETDATE()");
        });

        modelBuilder.Entity<Participant>(e =>
        {
            e.ToTable("Participants");
            e.HasKey(x => x.ParticipantID);
            e.Property(x => x.FullName).HasMaxLength(100).IsUnicode(false).IsRequired();
            e.Property(x => x.Email).HasMaxLength(150).IsUnicode(false).IsRequired();
            e.HasIndex(x => x.Email).IsUnique();
            e.Property(x => x.PasswordHash).HasMaxLength(255).IsUnicode(false).IsRequired();
            e.Property(x => x.Phone).HasMaxLength(20).IsUnicode(false);
            e.Property(x => x.DateOfBirth).HasColumnType("date");
            e.Property(x => x.CreatedAt).HasColumnType("datetime").HasDefaultValueSql("GETDATE()");
        });

        modelBuilder.Entity<Event>(e =>
        {
            e.ToTable("Events", t => t.HasCheckConstraint("CK_Events_EventType", "[EventType] IN ('Run','Walk','Cycle')"));
            e.HasKey(x => x.EventID);
            e.Property(x => x.EventName).HasMaxLength(150).IsUnicode(false).IsRequired();
            e.Property(x => x.Description).HasMaxLength(500).IsUnicode(false);
            e.Property(x => x.EventDate).HasColumnType("date");
            e.Property(x => x.Location).HasMaxLength(150).IsUnicode(false).IsRequired();
            e.Property(x => x.DistanceKm).HasPrecision(5, 2);
            e.Property(x => x.EventType).HasMaxLength(10).IsUnicode(false).IsRequired();
            e.Property(x => x.CreatedAt).HasColumnType("datetime").HasDefaultValueSql("GETDATE()");

            e.HasOne(x => x.Organiser)
                .WithMany(o => o.Events)
                .HasForeignKey(x => x.OrganiserID)
                .HasConstraintName("FK_Events_Organisers")
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Category>(e =>
        {
            e.ToTable("Categories");
            e.HasKey(x => x.CategoryID);
            e.Property(x => x.CategoryName).HasMaxLength(100).IsUnicode(false).IsRequired();
            e.Property(x => x.DistanceKm).HasPrecision(5, 2);
            e.Property(x => x.Price).HasPrecision(8, 2);

            e.HasOne(x => x.Event)
                .WithMany(ev => ev.Categories)
                .HasForeignKey(x => x.EventID)
                .HasConstraintName("FK_Categories_Events")
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Enrolment>(e =>
        {
            e.ToTable("Enrolments");
            e.HasKey(x => x.EnrolmentID);
            e.Property(x => x.EnrolmentDate).HasColumnType("datetime").HasDefaultValueSql("GETDATE()");
            e.Property(x => x.Status).HasMaxLength(20).IsUnicode(false).IsRequired().HasDefaultValue("Confirmed");

            e.HasIndex(x => new { x.CategoryID, x.ParticipantID })
                .IsUnique()
                .HasDatabaseName("UQ_Enrolments_Participant_Category");

            e.HasOne(x => x.Category)
                .WithMany(c => c.Enrolments)
                .HasForeignKey(x => x.CategoryID)
                .HasConstraintName("FK_Enrolments_Categories")
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.Participant)
                .WithMany(p => p.Enrolments)
                .HasForeignKey(x => x.ParticipantID)
                .HasConstraintName("FK_Enrolments_Participants")
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Result>(e =>
        {
            e.ToTable("Results");
            e.HasKey(x => x.ResultID);
            e.Property(x => x.FinishTime).HasColumnType("time");
            e.Property(x => x.Status).HasMaxLength(20).IsUnicode(false).IsRequired().HasDefaultValue("Finished");

            e.HasOne(x => x.Enrolment)
                .WithOne(en => en.Result)
                .HasForeignKey<Result>(x => x.EnrolmentID)
                .HasConstraintName("FK_Results_Enrolments")
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}