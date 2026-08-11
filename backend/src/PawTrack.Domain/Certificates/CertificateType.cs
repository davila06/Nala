namespace PawTrack.Domain.Certificates;

public enum CertificateType
{
    Vaccination          = 0,
    GeneralExam          = 1,
    Deworming            = 2,
    Neutering            = 3,
    HealthClearance      = 4,
    MicrochipRegistration = 5,
    VaccinePassport      = 6, // OIRSA-format travel passport
}
