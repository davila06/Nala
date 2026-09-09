namespace PawTrack.Application.Common.Interfaces;

public interface IDataProtectionService
{
    string Protect(string value);
    string Unprotect(string value);
}