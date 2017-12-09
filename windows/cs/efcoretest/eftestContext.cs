using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace efcoretest
{
    public partial class eftestContext : DbContext
    {
        public virtual DbSet<TblContents> TblContents { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. See http://go.microsoft.com/fwlink/?LinkId=723263 for guidance on storing connection strings.
                optionsBuilder.UseNpgsql(@"Host=localhost;Database=eftest;Username=postgres;Password=Passw0rd!");
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<TblContents>(entity =>
            {
                entity.HasKey(e => e.ContentsId);

                entity.ToTable("tbl_contents");

                entity.HasIndex(e => e.Tags)
                    .HasName("idx_tbl_contents_contents_id")
                    .ForNpgsqlHasMethod("gin");

                entity.Property(e => e.ContentsId)
                    .HasColumnName("contents_id")
                    .ValueGeneratedNever();

                entity.Property(e => e.Contents).HasColumnName("contents");

                entity.Property(e => e.Tags).HasColumnName("tags");
            });
        }
    }
}
