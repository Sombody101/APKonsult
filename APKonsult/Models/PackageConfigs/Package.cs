using System.ComponentModel.DataAnnotations.Schema;

namespace APKonsult.Models.PackageConfigs;

public sealed class Package
{
    [Column("id"), DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Column("package_name")]
    public string PackageName { get; set; } = string.Empty;

    [Column("friendly_name")]
    public string FriendlyName { get; set; } = string.Empty;

    public ICollection<PackageConfiguration> Configurations { get; set; } = [];
}
