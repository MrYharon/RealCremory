using AndroidX.Biometric;
using AndroidX.Fragment.App;
using Java.Util.Concurrent;
using Cremory.App.Services;

namespace Cremory.App.Platforms.Android.Services
{
    public class BiometricAuthService : IBiometricAuthService
    {
        public async Task<bool> AuthenticateAsync(string reason)
        {
            var tcs = new TaskCompletionSource<bool>();

            var activity = Platform.CurrentActivity;
            if (activity is not FragmentActivity fragmentActivity)
            {
                tcs.TrySetResult(false);
                return await tcs.Task;
            }

            var biometricManager = BiometricManager.From(activity);
            var canAuth = biometricManager.CanAuthenticate(BiometricManager.Authenticators.BiometricStrong);
            if (canAuth != BiometricManager.BiometricSuccess)
            {
                tcs.TrySetResult(false);
                return await tcs.Task;
            }

            var executor = Executors.NewSingleThreadExecutor()!;
            var prompt = new BiometricPrompt(fragmentActivity, executor,
                new AuthCallback(tcs));

            var promptInfo = new BiometricPrompt.PromptInfo.Builder()
                .SetTitle("Cremory")
                .SetSubtitle(reason)
                .SetAllowedAuthenticators(BiometricManager.Authenticators.BiometricStrong)
                .Build();

            prompt.Authenticate(promptInfo);

            return await tcs.Task;
        }

        private class AuthCallback : BiometricPrompt.AuthenticationCallback
        {
            private readonly TaskCompletionSource<bool> _tcs;

            public AuthCallback(TaskCompletionSource<bool> tcs)
            {
                _tcs = tcs;
            }

            public override void OnAuthenticationSucceeded(BiometricPrompt.AuthenticationResult? result)
            {
                base.OnAuthenticationSucceeded(result);
                _tcs.TrySetResult(true);
            }

            public override void OnAuthenticationError(int errorCode, Java.Lang.ICharSequence? errString)
            {
                base.OnAuthenticationError(errorCode, errString);
                _tcs.TrySetResult(false);
            }
        }
    }
}
