namespace Hospital.BLL.ModelVM;

/// <summary>Directory data approved for display on public pages and interactive carousels.</summary>
public sealed class PublicDoctorVm
{
    public string Id { get; init; } = string.Empty;
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string? SpecializationName { get; init; }
    public string? DepartmentName { get; init; }
    public string? Title { get; init; }
    public string? Credentials { get; init; }
    public string? Bio { get; init; }
    public string? Image { get; init; }
    public string? ClinicRoom { get; init; }
    public decimal SessionPrice { get; init; }
    public decimal FollowUpFee { get; init; }
    public int ExperienceYears { get; init; }
    public decimal Rating { get; init; }
    public int ReviewCount { get; init; }
    public bool IsTopRated { get; init; }
    public string? SealEmblem { get; init; }
}
