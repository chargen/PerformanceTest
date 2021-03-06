﻿using Microsoft.Azure.KeyVault;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace AzurePerformanceTest
{
    public class SecretStorage
    {
        KeyVaultClient keyVaultClient;
        string keyVaultUrl;
        Dictionary<string, string> cache = new Dictionary<string, string>();

        public SecretStorage(string AADAppId, string cerificateThumbprint, string keyVaultUrl)
        {
            this.keyVaultUrl = keyVaultUrl;

            var certificate = FindCertificateByThumbprint(cerificateThumbprint);
            var assertionCert = new ClientAssertionCertificate(AADAppId, certificate);

            keyVaultClient = new KeyVaultClient(new KeyVaultClient.AuthenticationCallback(
                   (authority, resource, scope) => GetAccessToken(authority, resource, scope, assertionCert)));
        }

        public async Task<string> GetSecret(string secretName)
        {
            if (cache.ContainsKey(secretName))
                return cache[secretName];

            var secret = await keyVaultClient.GetSecretAsync(keyVaultUrl, secretName);
            var secretValue = secret.Value;
            cache.Add(secretName, secretValue);
            return secretValue;
        }

        static X509Certificate2 FindCertificateByThumbprint(string thumbprint)
        {
            X509Store store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
            try
            {
                store.Open(OpenFlags.ReadOnly);
                X509Certificate2Collection col = store.Certificates.Find(X509FindType.FindByThumbprint, thumbprint, false); // Don't validate certs, since ours is self-signed.
                if (col == null || col.Count == 0)
                    return null;
                return col[0];
            }
            finally
            {
                store.Close();
            }
        }

        static async Task<string> GetAccessToken(string authority, string resource, string scope, ClientAssertionCertificate assertionCert)
        {
            var context = new AuthenticationContext(authority, TokenCache.DefaultShared);
            var result = await context.AcquireTokenAsync(resource, assertionCert).ConfigureAwait(false);

            return result.AccessToken;
        }
    }
}
