using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PawTrack.Domain.Certificates;

namespace PawTrack.Infrastructure.Persistence.Configurations;

public sealed class VaccinePassportConfiguration : IEntityTypeConfiguration<VaccinePassport>
{
    public void Configure(EntityTypeBuilder<VaccinePassport> builder)
    {
        builder.ToTable("VaccinePassports");
        builder.HasKey(passport => passport.Id);
        builder.Property(passport => passport.Id).ValueGeneratedNever();

        builder.Property(passport => passport.CertificateId).IsRequired();
        builder.Property(passport => passport.PetId).IsRequired();
        builder.Property(passport => passport.IssuingClinicId).IsRequired();
        builder.Property(passport => passport.IssuingVeterinarianId).IsRequired();
        builder.Property(passport => passport.PetNameSnapshot).IsRequired().HasMaxLength(120);
        builder.Property(passport => passport.PetSpeciesSnapshot).IsRequired().HasMaxLength(40);
        builder.Property(passport => passport.PetBreedSnapshot).HasMaxLength(120);
        builder.Property(passport => passport.PetSexSnapshot).HasMaxLength(20);
        builder.Property(passport => passport.PetColorSnapshot).HasMaxLength(80);
        builder.Property(passport => passport.MicrochipSnapshot).HasMaxLength(30);
        builder.Property(passport => passport.OwnerNameSnapshot).HasMaxLength(120);
        builder.Property(passport => passport.ClinicNameSnapshot).IsRequired().HasMaxLength(200);
        builder.Property(passport => passport.ClinicLicenseSnapshot).IsRequired().HasMaxLength(50);
        builder.Property(passport => passport.VetNameSnapshot).IsRequired().HasMaxLength(120);
        builder.Property(passport => passport.VetLicenseSnapshot).IsRequired().HasMaxLength(50);
        builder.Property(passport => passport.IssuedAt).IsRequired();
        builder.Property(passport => passport.ValidUntil).IsRequired();
        builder.Property(passport => passport.VerificationCode).IsRequired().HasMaxLength(8);
        builder.Property(passport => passport.FormatLabel).IsRequired().HasMaxLength(80);
        builder.Property(passport => passport.SchemaVersion).IsRequired().HasMaxLength(20);

        builder.OwnsMany(passport => passport.Vaccines, vaccines =>
        {
            vaccines.ToTable("VaccinePassportVaccines");
            vaccines.WithOwner().HasForeignKey("VaccinePassportId");
            vaccines.Property<Guid>("Id");
            vaccines.HasKey("Id");
            vaccines.Property(vaccine => vaccine.Name).IsRequired().HasMaxLength(120);
            vaccines.Property(vaccine => vaccine.Brand).HasMaxLength(120);
            vaccines.Property(vaccine => vaccine.LotNumber).HasMaxLength(80);
            vaccines.Property(vaccine => vaccine.ApplicationDate).IsRequired();
            vaccines.Property(vaccine => vaccine.ValidUntil);
        });

        builder.OwnsOne(passport => passport.ParasiteControl, parasite =>
        {
            parasite.Property(control => control.ProductName).HasMaxLength(120);
            parasite.Property(control => control.ApplicationDate);
            parasite.Property(control => control.NextDueDate);
        });

        builder.HasIndex(passport => passport.CertificateId).IsUnique();
        builder.HasIndex(passport => new { passport.PetId, passport.IssuedAt });
        builder.HasIndex(passport => new { passport.IssuingClinicId, passport.IssuedAt });
        builder.HasIndex(passport => passport.VerificationCode).IsUnique();
    }
}
