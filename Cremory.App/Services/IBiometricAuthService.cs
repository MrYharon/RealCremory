namespace Cremory.App.Services
{
    public interface IBiometricAuthService
    {
        Task<bool> AuthenticateAsync(string reason);
    }
}
