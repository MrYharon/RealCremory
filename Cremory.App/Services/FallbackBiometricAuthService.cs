namespace Cremory.App.Services
{
    public class FallbackBiometricAuthService : IBiometricAuthService
    {
        public Task<bool> AuthenticateAsync(string reason) => Task.FromResult(false);
    }
}
