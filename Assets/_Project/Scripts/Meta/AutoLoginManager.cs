using System;
using System.Threading.Tasks;
using Unity.Services.Core;
using Unity.Services.Authentication;
using UnityEngine;

#if UNITY_ANDROID
using GooglePlayGames;
using GooglePlayGames.BasicApi;
#endif
#if UNITY_IOS
using Apple.GameKit;
#endif

public class AutoLoginManager : MonoBehaviour
{
    public async Task InitializeAndSignInAsync()
    {
        try
        {
            // 1. Wait if another process is currently initializing
            while (UnityServices.State == ServicesInitializationState.Initializing)
            {
                await Task.Delay(100);
            }

            // 2. Initialize using the project default environment
            if (UnityServices.State == ServicesInitializationState.Uninitialized)
            {
                await UnityServices.InitializeAsync();
            }

            // 3. Confirm initialization completed before accessing AuthenticationService
            if (UnityServices.State != ServicesInitializationState.Initialized)
            {
                Debug.LogError($"[Auth] Unity Services failed to initialize. State: {UnityServices.State}");
                return;
            }

            // 4. Proceed to authentication
            if (!AuthenticationService.Instance.IsSignedIn)
            {
                if (AuthenticationService.Instance.SessionTokenExists)
                {
                    await AuthenticationService.Instance.SignInAnonymouslyAsync();
                }

                await SignInOrLinkPlatformAsync();
            }

            // 5. Load cloud data after sign-in completes
           
        }
        catch (Exception e)
        {
            Debug.LogError($"[Auth] System Error: {e.Message}");
        }
    }

    private async Task SignInOrLinkPlatformAsync()
    {
#if UNITY_EDITOR
        if (!AuthenticationService.Instance.IsSignedIn)
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }
#elif UNITY_ANDROID
        await AuthenticateGooglePlayAsync();
#elif UNITY_IOS
        await AuthenticateAppleGameCenterAsync();
#else
        if (!AuthenticationService.Instance.IsSignedIn)
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }
#endif
    }

#if UNITY_IOS
    private async Task AuthenticateAppleGameCenterAsync()
    {
        try
        {
            var player = await GKLocalPlayer.Authenticate();
            var signatureParams = await GKLocalPlayer.Local.FetchItemsForIdentityVerificationSignature();

            string signature = Convert.ToBase64String(signatureParams.GetSignature());
            string salt = Convert.ToBase64String(signatureParams.GetSalt());
            string publicKeyUrl = signatureParams.PublicKeyUrl.ToString();
            ulong timestamp = signatureParams.Timestamp;
            string teamPlayerId = player.TeamPlayerId;

            // Scenario 1: Already signed in anonymously -> LINK to preserve data
            if (AuthenticationService.Instance.IsSignedIn)
            {
                try
                {
                    await AuthenticationService.Instance.LinkWithAppleGameCenterAsync(
                        signature, teamPlayerId, publicKeyUrl, salt, timestamp);
                    Debug.Log("[Auth] Successfully LINKED anonymous account to Game Center!");
                    return;
                }
                catch (AuthenticationException ex) when (ex.ErrorCode == AuthenticationErrorCodes.AccountAlreadyLinked)
                {
                    Debug.LogWarning("[Auth] Apple account already linked to another user. Switching sessions...");
                    AuthenticationService.Instance.SignOut();
                }
            }

            // Scenario 2: Direct sign in
            await AuthenticationService.Instance.SignInWithAppleGameCenterAsync(
                signature, teamPlayerId, publicKeyUrl, salt, timestamp);
            Debug.Log("[Auth] Game Center Sign-In SUCCESS!");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[Auth] Game Center Error: {ex.Message}");

            if (!AuthenticationService.Instance.IsSignedIn)
            {
                Debug.Log("[Auth] Falling back to Anonymous Sign-In...");
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
            }
        }
    }
#endif

#if UNITY_ANDROID
    private Task AuthenticateGooglePlayAsync()
    {
        var tcs = new TaskCompletionSource<bool>();
        PlayGamesPlatform.Activate();

        PlayGamesPlatform.Instance.Authenticate(async (SignInStatus status) =>
        {
            if (status == SignInStatus.Success)
            {
                PlayGamesPlatform.Instance.RequestServerSideAccess(true, async (string authCode) =>
                {
                    try
                    {
                        if (AuthenticationService.Instance.IsSignedIn)
                        {
                            try
                            {
                                await AuthenticationService.Instance.LinkWithGooglePlayGamesAsync(authCode);
                                Debug.Log("[Auth] Successfully LINKED anonymous account to Google Play!");
                                tcs.SetResult(true);
                                return;
                            }
                            catch (AuthenticationException ex) when (ex.ErrorCode == AuthenticationErrorCodes.AccountAlreadyLinked)
                            {
                                Debug.LogWarning("[Auth] Google account already linked to another user. Switching sessions...");
                                AuthenticationService.Instance.SignOut();
                            }
                        }

                        await AuthenticationService.Instance.SignInWithGooglePlayGamesAsync(authCode);
                        Debug.Log("[Auth] Google Play Sign-In SUCCESS!");
                        tcs.SetResult(true);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"[Auth] Google UGS Error: {ex.Message}");
                        await FallbackToAnonymousAsync();
                        tcs.SetResult(false);
                    }
                });
            }
            else
            {
                Debug.LogWarning("[Auth] Google Play Games local sign-in failed/cancelled.");
                await FallbackToAnonymousAsync();
                tcs.SetResult(false);
            }
        });

        return tcs.Task;
    }

    private async Task FallbackToAnonymousAsync()
    {
        if (!AuthenticationService.Instance.IsSignedIn)
        {
            Debug.Log("[Auth] Falling back to Anonymous Sign-In...");
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }
    }
#endif
}