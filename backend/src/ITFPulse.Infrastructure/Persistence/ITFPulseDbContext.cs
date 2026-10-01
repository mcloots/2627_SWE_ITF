using ITFPulse.Domain.Posts;
using Microsoft.EntityFrameworkCore;
using ITFPulse.Domain.Followers;
using MassTransit;
using System;
using System.Collections.Generic;
using System.Text;

namespace ITFPulse.Infrastructure.Persistence
{
    public sealed class ITFPulseDbContext : DbContext
    {
        public ITFPulseDbContext(DbContextOptions<ITFPulseDbContext> options) : base(options)
        {
        }

        public DbSet<Post> Posts => Set<Post>();
        public DbSet<Follow> Follows => Set<Follow>();
        public DbSet<FeedEntry> FeedEntries => Set<FeedEntry>();

        protected override void OnModelCreating(
            ModelBuilder modelBuilder)
        {
            modelBuilder.HasDefaultSchema("itfpulse");
            // Persist transport deduplication and pending publications alongside application data.
            modelBuilder.AddInboxStateEntity();
            modelBuilder.AddOutboxMessageEntity();
            modelBuilder.AddOutboxStateEntity();

            modelBuilder.Entity<Follow>(b =>
            {
                b.ToTable("follows");
                b.HasKey(x => x.Id);
                b.Property(x => x.Id).HasColumnName("id").UseIdentityAlwaysColumn();
                b.Property(x => x.AuthorId).HasColumnName("author_id");
                b.Property(x => x.FollowerId).HasColumnName("follower_id");
                b.Property(x => x.FollowedAt).HasColumnName("followed_at");
                b.HasIndex(x => new { x.AuthorId, x.FollowerId }).IsUnique();
                b.HasIndex(x => new { x.AuthorId, x.Id });
            });
            modelBuilder.Entity<FeedEntry>(b =>
            {
                b.ToTable("feed_entries");
                // Enforce one feed reference per follower/post, independently of transport deduplication.
                b.HasKey(x => new { x.FollowerId, x.PostId });
                b.Property(x => x.Sequence).HasColumnName("sequence").UseIdentityAlwaysColumn();
                b.Property(x => x.FollowerId).HasColumnName("follower_id");
                b.Property(x => x.PostId).HasColumnName("post_id");
                b.Property(x => x.CreatedAt).HasColumnName("created_at");
                b.HasIndex(x => new { x.FollowerId, x.Sequence });
                b.HasIndex(x => x.PostId);
                b.HasOne<Post>().WithMany().HasForeignKey(x => x.PostId).OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.ApplyConfigurationsFromAssembly(
                typeof(ITFPulseDbContext).Assembly);
        }
    }
}
