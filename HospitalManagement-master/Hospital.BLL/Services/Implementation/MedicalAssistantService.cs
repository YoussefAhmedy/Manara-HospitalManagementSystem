using Hospital.BLL.Services.Abstraction;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Hospital.BLL.Services.Implementation;

public sealed class MedicalAssistantService(
    IConfiguration configuration,
    ILogger<MedicalAssistantService> logger) : IMedicalAssistantService
{
    private const string StandardDisclaimer =
        "MANARA Medical Guidance provides general health education and department triage only — it is not a medical diagnosis or substitute for an in-person clinical examination.";

    private static readonly HttpClient SharedHttp = new() { Timeout = TimeSpan.FromSeconds(12) };

    private static readonly string[] OffTopicIndicators =
    [
        "python", "javascript", "c#", "sql", "react", "homework", "math", "integral",
        "crypto", "bitcoin", "stock market", "football", "soccer", "movie", "recipe",
        "pasta", "cake", "weather", "politics", "election", "write an essay", "poem",
        "translate", "video game", "netflix", "password crack"
    ];

    private static readonly string[] MedicalIndicators =
    [
        "pain", "ache", "fever", "cough", "headache", "migraine", "heart", "chest", "blood",
        "pressure", "bp", "sugar", "glucose", "diabetes", "thyroid", "tsh", "rash", "skin",
        "itch", "allergy", "breath", "asthma", "stomach", "nausea", "vomit", "dizzy",
        "joint", "knee", "back", "spine", "fracture", "child", "pediatric", "baby", "kid",
        "vaccine", "lab", "cbc", "test", "x-ray", "xray", "mri", "ct", "scan", "ultrasound",
        "radiology", "pacs", "medication", "medicine", "pill", "dose", "prescription", "rx",
        "doctor", "clinic", "department", "appointment", "symptom", "infection", "tired",
        "fatigue", "sleep", "stress", "anxiety", "ear", "throat", "eye", "vision", "health",
        "medical", "manara", "follow up", "follow-up", "consult", "cardiology", "neurology",
        "orthopedic", "dermatology", "internal"
    ];

    public async Task<MedicalAssistantReply> AskAsync(string prompt, CancellationToken cancellationToken = default)
    {
        var trimmed = (prompt ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
        {
            return new MedicalAssistantReply(
                false,
                false,
                "Please describe your symptoms, lab/imaging question, or the type of specialist care you are looking for.",
                null,
                null,
                StandardDisclaimer,
                false);
        }

        var lower = trimmed.ToLowerInvariant();
        var hasMedicalKeyword = MedicalIndicators.Any(lower.Contains);
        var hasOffTopicKeyword = OffTopicIndicators.Any(lower.Contains);

        if (hasOffTopicKeyword && !hasMedicalKeyword)
        {
            return new MedicalAssistantReply(
                false,
                false,
                "I am MANARA's dedicated medical triage & patient guidance assistant. I can only assist with health symptoms, diagnostic preparation (lab tests, MRI/CT/X-ray), medications, and choosing the right MANARA specialist department.",
                null,
                null,
                StandardDisclaimer,
                false);
        }

        if (!hasMedicalKeyword && trimmed.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length <= 5 &&
            (lower.Contains("hello") || lower.Contains("hi") || lower.Contains("hey")))
        {
            return new MedicalAssistantReply(
                true,
                false,
                "Welcome to MANARA Medical Complex. Tell me about any symptoms you are experiencing, how to prepare for a lab or imaging test, or which specialist department you need, and I will guide you to the right team.",
                "Internal Medicine",
                "Dr. Karim Mansour",
                StandardDisclaimer,
                false);
        }

        // Optional external LLM configuration
        var apiKey = configuration["MedicalAssistant:ApiKey"];
        var endpoint = configuration["MedicalAssistant:Endpoint"];
        var model = configuration["MedicalAssistant:Model"] ?? "gpt-4o-mini";

        if (!string.IsNullOrWhiteSpace(apiKey) && !string.IsNullOrWhiteSpace(endpoint))
        {
            var externalReply = await TryExternalLlmAsync(trimmed, endpoint, apiKey, model, cancellationToken);
            if (externalReply is not null)
            {
                return externalReply;
            }
        }

        return EvaluateStructuredMedicalGuidance(lower, hasMedicalKeyword);
    }

    private async Task<MedicalAssistantReply?> TryExternalLlmAsync(
        string prompt,
        string endpoint,
        string apiKey,
        string model,
        CancellationToken cancellationToken)
    {
        try
        {
            using var req = new HttpRequestMessage(HttpMethod.Post, endpoint);
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

            var payload = new
            {
                model,
                temperature = 0.2,
                messages = new[]
                {
                    new
                    {
                        role = "system",
                        content = "You are the MANARA Medical Complex Alexandria triage guidance assistant. Answer strictly medical/healthcare/diagnostic questions concisely, recommend a department (Cardiology, Internal Medicine, Orthopedics, Pediatrics, Radiology & Imaging, Clinical Laboratory, Neurology, Dermatology), highlight urgent red flags if any, and politely refuse non-medical questions."
                    },
                    new { role = "user", content = prompt }
                }
            };

            req.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            using var res = await SharedHttp.SendAsync(req, cancellationToken);
            if (!res.IsSuccessStatusCode)
            {
                return null;
            }

            using var doc = await JsonDocument.ParseAsync(await res.Content.ReadAsStreamAsync(cancellationToken), cancellationToken: cancellationToken);
            var text = doc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString();

            if (string.IsNullOrWhiteSpace(text))
            {
                return null;
            }

            var fallback = EvaluateStructuredMedicalGuidance(prompt.ToLowerInvariant(), true);
            return new MedicalAssistantReply(
                true,
                fallback.IsUrgent,
                text.Trim(),
                fallback.SuggestedDepartment,
                fallback.SuggestedDoctorName,
                StandardDisclaimer,
                true);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "External LLM medical assistant unavailable; falling back to structured MANARA triage.");
            return null;
        }
    }

    private static MedicalAssistantReply EvaluateStructuredMedicalGuidance(string lower, bool hasMedicalKeyword)
    {
        if (!hasMedicalKeyword)
        {
            return new MedicalAssistantReply(
                false,
                false,
                "Please ask a medical, symptom, diagnostic (Lab / MRI / X-Ray), medication, or department-selection question so I can route you accurately within MANARA Medical Complex.",
                null,
                null,
                StandardDisclaimer,
                false);
        }

        // Red flags
        if (ContainsAny(lower, "chest pain", "crushing chest", "difficulty breathing", "shortness of breath", "can't breathe", "unconscious", "fainted", "slurred speech", "face drooping", "severe bleeding", "anaphylaxis"))
        {
            return new MedicalAssistantReply(
                true,
                true,
                "Your description includes potential urgent warning signs (chest pain, breathing difficulty, sudden neurological deficit, or severe bleeding). Do not wait for a routine online reply — please seek immediate emergency evaluation at MANARA Urgent Care or your nearest Emergency Department now.",
                "Cardiology & Emergency Triage",
                "Dr. Hisham El-Masry",
                StandardDisclaimer,
                false);
        }

        if (ContainsAny(lower, "palpitation", "blood pressure", "hypertension", "bp", "heart", "cardiac", "cholesterol", "ecg", "echocardiogram"))
        {
            return new MedicalAssistantReply(
                true,
                false,
                "For blood pressure fluctuations, palpitations, cholesterol management, or preventive cardiac screening, keep a 3-day morning/evening resting blood pressure log, limit sodium intake, avoid sudden energy drinks or excess caffeine, and avoid stopping prescribed cardiac medications abruptly.",
                "Cardiology",
                "Dr. Salma El-Sayed",
                StandardDisclaimer,
                false);
        }

        if (ContainsAny(lower, "migraine", "headache", "dizzy", "vertigo", "numbness", "tingling", "seizure", "nerve", "memory", "eeg"))
        {
            return new MedicalAssistantReply(
                true,
                false,
                "For recurring headaches, migraines, vertigo, or nerve tingling, track triggers (hydration, sleep hours, screen glare, meals) and note duration and visual aura if present. Seek urgent evaluation if headache is sudden/thunderclap or paired with fever, neck stiffness, or weakness.",
                "Neurology",
                "Dr. Sara El-Gamal",
                StandardDisclaimer,
                false);
        }

        if (ContainsAny(lower, "knee", "back pain", "spine", "joint", "bone", "fracture", "sprain", "shoulder", "ankle", "arthritis", "orthopedic"))
        {
            return new MedicalAssistantReply(
                true,
                false,
                "For joint stiffness, sports injuries, or spine discomfort, rest the affected joint, apply 15-minute cold compresses in the first 48 hours of acute swelling, avoid high-impact strain, and bring any prior X-ray or MRI images to your orthopedic visit.",
                "Orthopedics",
                "Dr. Tarek Nabil",
                StandardDisclaimer,
                false);
        }

        if (ContainsAny(lower, "child", "baby", "infant", "kid", "toddler", "pediatric", "vaccination", "immunization", "ear pain"))
        {
            return new MedicalAssistantReply(
                true,
                false,
                "For pediatric fever, respiratory symptoms, developmental questions, or vaccination schedules, monitor hydration (wet diapers / fluid intake), record exact temperature readings before dosing weight-based antipyretics, and consult our Pediatrics Sanctuary team.",
                "Pediatrics",
                "Dr. Nadia Farouk",
                StandardDisclaimer,
                false);
        }

        if (ContainsAny(lower, "rash", "skin", "eczema", "acne", "itch", "mole", "hives", "psoriasis", "dermatolog"))
        {
            return new MedicalAssistantReply(
                true,
                false,
                "For persistent rashes, eczema flare-ups, or changing skin lesions, avoid harsh fragranced cleansers or over-the-counter steroid creams until evaluated, note any new detergents/foods/medications, and book a digital dermoscopy exam.",
                "Dermatology",
                "Dr. Yasmin Zaki",
                StandardDisclaimer,
                false);
        }

        if (ContainsAny(lower, "mri", "ct", "x-ray", "xray", "ultrasound", "scan", "radiology", "pacs", "mammogram"))
        {
            return new MedicalAssistantReply(
                true,
                false,
                "Diagnostic imaging preparation tip: remove metallic jewelry before MRI, bring prior comparison scans, and inform the Radiology Suite beforehand about pacemakers, implants, pregnancy, or kidney history before contrast CT/MRI studies. Verified MANARA PACS reports appear automatically in My Updates.",
                "Radiology & Imaging",
                "Dr. Amr Fouad",
                StandardDisclaimer,
                false);
        }

        if (ContainsAny(lower, "cbc", "blood test", "fasting", "glucose", "a1c", "thyroid", "tsh", "urine", "crp", "lab"))
        {
            return new MedicalAssistantReply(
                true,
                false,
                "Laboratory preparation tip: fasting lipid and glucose panels typically require 8–12 hours with plain water only beforehand. Morning sample collection between 8:00 AM and 11:00 AM is ideal for hormone and TSH panels. Released reports trigger an automatic update in My Updates.",
                "Clinical Laboratory",
                "Dr. Laila Kamel",
                StandardDisclaimer,
                false);
        }

        return new MedicalAssistantReply(
            true,
            false,
            "Based on your medical question, an Internal Medicine evaluation is recommended to review your full history, medications, and vital signs, and coordinate targeted lab or imaging tests if needed.",
            "Internal Medicine",
            "Dr. Karim Mansour",
            StandardDisclaimer,
            false);
    }

    private static bool ContainsAny(string text, params string[] terms) => terms.Any(text.Contains);
}
