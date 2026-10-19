namespace Tahdeera.Domain.Enums;

public enum AttendanceChangeKind
{
    InitialSystemAbsence = 1,
    InitialUserEntry = 2,
    ManualCorrection = 3,
    SystemReevaluation = 4,
    VoidedOnCancellation = 5,
    RestoredFromCancellation = 6,
    Other = 7
}
