// WebAuthn/Passkey support functions

window.PasskeySupport = {
    // Check if passkeys are supported
    isSupported: function() {
        return !!(window.PublicKeyCredential &&
                  typeof window.PublicKeyCredential.isUserVerifyingPlatformAuthenticatorAvailable === 'function');
    },

    // Create a new passkey
    createPasskey: async function(username, challenge, domain = 'localhost') {
        try {
            // Convert base64 challenge to Uint8Array
            const challengeArray = Uint8Array.from(atob(challenge), c => c.charCodeAt(0));

            // Generate a random user ID if needed
            const userId = new TextEncoder().encode(username);

            const publicKey = {
                challenge: challengeArray,
                rp: {
                    name: 'Auto',
                    id: domain
                },
                user: {
                    id: userId,
                    name: username,
                    displayName: username
                },
                pubKeyCredParams: [
                    { type: "public-key", alg: -7 },  // ES256
                    { type: "public-key", alg: -257 } // RS256
                ],
                authenticatorSelection: {
                    authenticatorAttachment: "platform",
                    requireResidentKey: false,
                    userVerification: "preferred"
                },
                timeout: 60000,
                attestation: "none"
            };

            const credential = await navigator.credentials.create({ publicKey });

            // Convert to base64 for easy transport
            return {
                credentialId: btoa(String.fromCharCode(...new Uint8Array(credential.rawId))),
                publicKey: btoa(String.fromCharCode(...new Uint8Array(credential.response.publicKey || []))),
                attestationObject: btoa(String.fromCharCode(...new Uint8Array(credential.response.attestationObject))),
                clientDataJSON: btoa(String.fromCharCode(...new Uint8Array(credential.response.clientDataJSON))),
                userHandle: btoa(String.fromCharCode(...userId))
            };
        } catch (error) {
            console.error('Error creating passkey:', error);
            return null;
        }
    },

    // Authenticate with a passkey
    getPasskey: async function(challenge, credentialIds = [], domain = 'localhost') {
        try {
            // Convert base64 challenge to Uint8Array
            const challengeArray = Uint8Array.from(atob(challenge), c => c.charCodeAt(0));

            const publicKey = {
                challenge: challengeArray,
                rpId: domain,
                timeout: 60000,
                userVerification: "preferred"
            };

            // If we have specific credential IDs, add them
            if (credentialIds && credentialIds.length > 0) {
                publicKey.allowCredentials = credentialIds.map(id => ({
                    id: Uint8Array.from(atob(id), c => c.charCodeAt(0)),
                    type: 'public-key',
                    transports: ['internal', 'usb', 'ble', 'nfc']
                }));
            }

            const credential = await navigator.credentials.get({ publicKey });

            // Convert to base64 for easy transport
            return {
                credentialId: btoa(String.fromCharCode(...new Uint8Array(credential.rawId))),
                authenticatorData: btoa(String.fromCharCode(...new Uint8Array(credential.response.authenticatorData))),
                clientDataJSON: btoa(String.fromCharCode(...new Uint8Array(credential.response.clientDataJSON))),
                signature: btoa(String.fromCharCode(...new Uint8Array(credential.response.signature))),
                userHandle: credential.response.userHandle ?
                    btoa(String.fromCharCode(...new Uint8Array(credential.response.userHandle))) : null
            };
        } catch (error) {
            console.error('Error getting passkey:', error);
            return null;
        }
    }
};