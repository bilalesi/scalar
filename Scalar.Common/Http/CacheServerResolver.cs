using Scalar.Common.Git;
using Scalar.Common.Tracing;
using System;
using System.Linq;

namespace Scalar.Common.Http
{
    public class CacheServerResolver
    {
        private ITracer tracer;
        private Enlistment enlistment;

        public CacheServerResolver(
            ITracer tracer,
            Enlistment enlistment)
        {
            this.tracer = tracer;
            this.enlistment = enlistment;
        }

        public static CacheServerInfo GetCacheServerFromConfig(Enlistment enlistment)
        {
            string url = GetUrlFromConfig(enlistment);
            return new CacheServerInfo(
                url,
                url == enlistment.RepoUrl ? CacheServerInfo.ReservedNames.None : null);
        }

        public static string GetUrlFromConfig(Enlistment enlistment)
        {
            GitProcess git = enlistment.CreateGitProcess();

            // TODO 1057500: Remove support for encoded-repo-url cache config setting
            return
                GetValueFromConfig(git, ScalarConstants.GitConfig.CacheServer, localOnly: true)
                ?? enlistment.RepoUrl;
        }

        public bool TryResolveUrlFromRemote(
            string cacheServerName,
            ServerScalarConfig serverScalarConfig,
            out CacheServerInfo cacheServer,
            out string error)
        {
            if (string.IsNullOrWhiteSpace(cacheServerName))
            {
                throw new InvalidOperationException("An empty name is not supported");
            }

            cacheServer = null;
            error = null;

            if (cacheServerName.Equals(CacheServerInfo.ReservedNames.Default, StringComparison.OrdinalIgnoreCase))
            {
                cacheServer =
                    serverScalarConfig.CacheServers.FirstOrDefault(cache => cache.GlobalDefault)
                    ?? this.CreateNone();
            }
            else
            {
                cacheServer = serverScalarConfig.CacheServers.FirstOrDefault(cache =>
                    cache.Name.Equals(cacheServerName, StringComparison.OrdinalIgnoreCase));

                if (cacheServer == null)
                {
                    error = "No cache server found with name " + cacheServerName;
                    return false;
                }
            }

            return true;
        }

        public CacheServerInfo ResolveNameFromRemote(
            string cacheServerUrl,
            ServerScalarConfig serverScalarConfig)
        {
            if (string.IsNullOrWhiteSpace(cacheServerUrl))
            {
                throw new InvalidOperationException("An empty url is not supported");
            }

            if (this.InputMatchesEnlistmentUrl(cacheServerUrl))
            {
                return this.CreateNone();
            }

            return
                serverScalarConfig.CacheServers.FirstOrDefault(cache => cache.Url.Equals(cacheServerUrl, StringComparison.OrdinalIgnoreCase))
                ?? new CacheServerInfo(cacheServerUrl, CacheServerInfo.ReservedNames.UserDefined);
        }

        public CacheServerInfo ParseUrlOrFriendlyName(string userInput)
        {
            if (userInput == null)
            {
                return new CacheServerInfo(null, CacheServerInfo.ReservedNames.Default);
            }

            if (string.IsNullOrWhiteSpace(userInput))
            {
                throw new InvalidOperationException("A missing input (null) is fine, but an empty input (empty string) is not supported");
            }

            if (this.InputMatchesEnlistmentUrl(userInput) ||
                userInput.Equals(CacheServerInfo.ReservedNames.None, StringComparison.OrdinalIgnoreCase))
            {
                return this.CreateNone();
            }

            Uri uri;
            if (Uri.TryCreate(userInput, UriKind.Absolute, out uri))
            {
                return new CacheServerInfo(userInput, CacheServerInfo.ReservedNames.UserDefined);
            }
            else
            {
                return new CacheServerInfo(null, userInput);
            }
        }

        public bool TrySaveUrlToLocalConfig(CacheServerInfo cache, out string error)
        {
            GitProcess git = this.enlistment.CreateGitProcess();
            GitProcess.Result result = git.SetInLocalConfig(ScalarConstants.GitConfig.CacheServer, cache.Url, replaceAll: true);

            error = result.Errors;
            return result.ExitCodeIsSuccess;
        }

        private static string GetValueFromConfig(GitProcess git, string configName, bool localOnly)
        {
            GitProcess.ConfigResult result =
                localOnly
                ? git.GetFromLocalConfig(configName)
                : git.GetFromConfig(configName);

            if (!result.TryParseAsString(out string value, out string error))
            {
                throw new InvalidRepoException(error);
            }

            return value;
        }

        private CacheServerInfo CreateNone()
        {
            return new CacheServerInfo(this.enlistment.RepoUrl, CacheServerInfo.ReservedNames.None);
        }

        private bool InputMatchesEnlistmentUrl(string userInput)
        {
            return this.enlistment.RepoUrl.TrimEnd('/').Equals(userInput.TrimEnd('/'), StringComparison.OrdinalIgnoreCase);
        }
    }
}
