using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ServiceTemplate.Infrastructure.Persistence.Entities;

namespace ServiceTemplate.Infrastructure.Persistence.Configurations;

public class OrderEmbeddingConfiguration : IEntityTypeConfiguration<OrderEmbedding>
{
    public void Configure(EntityTypeBuilder<OrderEmbedding> builder)
    {
        builder.ToTable("order_embeddings");
        builder.HasKey(e => e.OrderId);

        builder.Property(e => e.IndexedContent)
            .IsRequired();

        builder.Property(e => e.Embedding)
            .HasColumnType("vector(1536)")
            .IsRequired();

        builder.Property(e => e.GeneratedAt)
            .IsRequired();

        builder.Property(e => e.ModelVersion)
            .IsRequired()
            .HasMaxLength(100);

        // HNSW index with cosine distance operators
        builder.HasIndex(e => e.Embedding)
            .HasMethod("hnsw")
            .HasOperators("vector_cosine_ops")
            .HasStorageParameter("m", 16)
            .HasStorageParameter("ef_construction", 64);
    }
}
