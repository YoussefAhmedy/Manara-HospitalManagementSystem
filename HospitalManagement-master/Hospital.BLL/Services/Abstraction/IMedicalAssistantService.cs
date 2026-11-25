namespace Hospital.BLL.Services.Abstraction;

public sealed record MedicalAssistantReply(
    bool IsMedical,
    bool IsUrgent,
    string Answer,
    string? SuggestedDepartment,
    string? SuggestedDoctorName,
    string Disclaimer,
    bool UsedExternalLlm);

public interface IMedicalAssistantService
{
    Task<MedicalAssistantReply> AskAsync(string prompt, CancellationToken cancellationToken = default);
}
