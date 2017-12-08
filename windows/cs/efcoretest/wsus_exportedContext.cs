using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Logging;

namespace efcoretest
{
    public partial class wsus_exportedContext : DbContext
    {
        public virtual DbSet<TblCategories> TblCategories { get; set; }
        public virtual DbSet<TblClassifications> TblClassifications { get; set; }
        public virtual DbSet<TblFileDigests> TblFileDigests { get; set; }
        public virtual DbSet<TblFiles> TblFiles { get; set; }
        public virtual DbSet<TblLanguages> TblLanguages { get; set; }
        public virtual DbSet<TblUpdates> TblUpdates { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
			//LoggerFactory loggerFactory = new LoggerFactory();
			//loggerFactory.AddProvider(new TraceLoggerProvider());
			//optionsBuilder.UseLoggerFactory(loggerFactory);

			if (!optionsBuilder.IsConfigured)
            {
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. See http://go.microsoft.com/fwlink/?LinkId=723263 for guidance on storing connection strings.
                optionsBuilder.UseNpgsql(@"Host=localhost;Database=wsus_exported;Username=postgres;Password=wertyu89?");
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<TblCategories>(entity =>
            {
                entity.HasKey(e => new { e.UpdateId, e.CategoryId });

                entity.ToTable("tbl_categories");

                entity.HasIndex(e => e.CategoryId)
                    .HasName("index_tbl_categories_category_id");

                entity.Property(e => e.UpdateId).HasColumnName("update_id");

                entity.Property(e => e.CategoryId).HasColumnName("category_id");
            });

            modelBuilder.Entity<TblClassifications>(entity =>
            {
                entity.HasKey(e => new { e.UpdateId, e.ClassificationId });

                entity.ToTable("tbl_classifications");

                entity.HasIndex(e => e.ClassificationId)
                    .HasName("index_tbl_classifications_classification_id");

                entity.Property(e => e.UpdateId).HasColumnName("update_id");

                entity.Property(e => e.ClassificationId).HasColumnName("classification_id");
            });

            modelBuilder.Entity<TblFileDigests>(entity =>
            {
                entity.HasKey(e => new { e.UpdateId, e.FileDigest });

                entity.ToTable("tbl_file_digests");

                entity.HasIndex(e => e.FileDigest)
                    .HasName("index_tbl_file_digests_file_digest");

                entity.Property(e => e.UpdateId).HasColumnName("update_id");

                entity.Property(e => e.FileDigest).HasColumnName("file_digest");
            });

            modelBuilder.Entity<TblFiles>(entity =>
            {
                entity.HasKey(e => e.Digest);

                entity.ToTable("tbl_files");

                entity.Property(e => e.Digest)
                    .HasColumnName("digest")
                    .ValueGeneratedNever();

                entity.Property(e => e.ContentPath).HasColumnName("content_path");

                entity.Property(e => e.DecryptionKey).HasColumnName("decryption_key");

                entity.Property(e => e.Muurl).HasColumnName("muurl");

                entity.Property(e => e.Name).HasColumnName("name");
            });

            modelBuilder.Entity<TblLanguages>(entity =>
            {
                entity.HasKey(e => e.LanguageId);

                entity.ToTable("tbl_languages");

                entity.Property(e => e.LanguageId)
                    .HasColumnName("language_id")
                    .ValueGeneratedNever();

                entity.Property(e => e.Enabled).HasColumnName("enabled");

                entity.Property(e => e.LongLanguage).HasColumnName("long_language");

                entity.Property(e => e.ShortLanguage).HasColumnName("short_language");
            });

            modelBuilder.Entity<TblUpdates>(entity =>
            {
                entity.HasKey(e => new { e.UpdateId, e.RevisionNumber });

                entity.ToTable("tbl_updates");

                entity.HasIndex(e => e.Categories)
                    .HasName("categories_tag_idx")
                    .ForNpgsqlHasMethod("gin");

                entity.HasIndex(e => e.Classifications)
                    .HasName("classification_tag_idx")
                    .ForNpgsqlHasMethod("gin");

                entity.Property(e => e.UpdateId).HasColumnName("update_id");

                entity.Property(e => e.RevisionNumber).HasColumnName("revision_number");

                entity.Property(e => e.Categories).HasColumnName("categories");

                entity.Property(e => e.Classifications).HasColumnName("classifications");

                entity.Property(e => e.FileDigests).HasColumnName("file_digests");

                entity.Property(e => e.RevisionId).HasColumnName("revision_id");

                entity.Property(e => e.Title).HasColumnName("title");

                entity.Property(e => e.Xml)
                    .HasColumnName("xml")
                    .HasColumnType("xml");
            });
        }
    }
}
