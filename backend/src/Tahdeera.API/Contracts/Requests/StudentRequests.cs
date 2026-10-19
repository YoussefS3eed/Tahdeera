namespace Tahdeera.API.Contracts.Requests;

public record RegisterStudentRequest(
    string FullName,
    short GradeId,
    string? GuardianName,
    string GuardianPhone,
    string? StudentPhone,
    bool WhatsappOptIn,
    string? SeatNumber,
    string? LocalNotes,
    string? CardToken,
    string? CardUidDisplay,
    bool AllowSiblingPhone = false
);

public record LinkStudentByCardRequest(
    string ScannedToken,
    short? GradeId,
    string? GuardianName,
    string GuardianPhone,
    string? StudentPhone,
    bool WhatsappOptIn,
    string? SeatNumber,
    string? LocalNotes,
    bool AllowSiblingPhone = false
);

public record LinkStudentByCodeRequest(
    string StudentCode,
    string RegisteredGuardianPhone,
    short? GradeId,
    string? GuardianName,
    string? GuardianPhone,
    string? StudentPhone,
    bool WhatsappOptIn,
    string? SeatNumber,
    string? LocalNotes,
    bool AllowSiblingPhone = false
);

public record ChangeStudentStatusRequest(
    string Status,
    string? Reason = null
);

public record UpdateStudentContactRequest(
    string? FullName,
    string? GuardianName,
    string? GuardianPhone,
    string? StudentPhone,
    bool WhatsappOptIn,
    string? SeatNumber,
    string? LocalNotes,
    bool AllowSiblingPhone = false
);
