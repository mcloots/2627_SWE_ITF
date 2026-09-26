using ITFPulse.Domain.Common;
using ITFPulse.Domain.Posts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Text;

namespace ITFPulse.Infrastructure.Persistence.Configurations
{
    internal sealed class PostConfiguration
    : IEntityTypeConfiguration<Post>
    {
        public void Configure(
            EntityTypeBuilder<Post> builder)
        {
            builder.ToTable("posts");

            builder.HasKey(post => post.Id);

            builder.Property(post => post.Id)
                .HasColumnName("id")
                .ValueGeneratedNever();

            builder.Property(post => post.AuthorId)
                .HasColumnName("author_id")
                .IsRequired();

            builder.Property(post => post.PostContent)
                .HasColumnName("content")
                .HasConversion(
                    postContent => postContent.Value,
                    value => new PostContent(value))
                .HasMaxLength(2000)
                .IsRequired();

            builder.Property(post => post.CreatedAt)
                .HasColumnName("created_at")
                .HasConversion(
                    createdAt => createdAt.Value,
                    value => new CreatedAt(value))
                .HasColumnType("timestamp with time zone")
                .IsRequired();

            // Comments are not part of this vertical slice yet.
            builder.Ignore(post => post.Comments);
        }
    }
}
