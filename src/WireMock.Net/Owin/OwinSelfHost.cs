﻿#if !NETSTANDARD
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using WireMock.Validation;
using Owin;
using Microsoft.Owin.Hosting;
using WireMock.Http;

namespace WireMock.Owin
{
    internal class OwinSelfHost : IOwinSelfHost
    {
        private readonly WireMockMiddlewareOptions _options;
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();

        public OwinSelfHost([NotNull] WireMockMiddlewareOptions options, [NotNull] params string[] uriPrefixes)
        {
            Check.NotNull(options, nameof(options));
            Check.NotEmpty(uriPrefixes, nameof(uriPrefixes));

            foreach (string uriPrefix in uriPrefixes)
            {
                Urls.Add(uriPrefix);

                int port;
                string host;
                PortUtil.TryExtractProtocolAndPort(uriPrefix, out host, out port);
                Ports.Add(port);
            }

            _options = options;
        }

        public bool IsStarted { get; private set; }

        public List<string> Urls { get; } = new List<string>();

        public List<int> Ports { get; } = new List<int>();

        [PublicAPI]
        public Task StartAsync()
        {
            return Task.Run(() =>
            {
                StartServers();
            }, _cts.Token);
        }

        [PublicAPI]
        public Task StopAsync()
        {
            _cts.Cancel();

            return Task.FromResult(true);
        }

        private void StartServers()
        {
            System.Console.WriteLine("WireMock.Net server using .net 4.5.x or .net 4.6.x");

            Action<IAppBuilder> startup = app =>
            {
                app.Use<WireMockMiddleware>(_options);
            };

            var servers = new List<IDisposable>();
            foreach (var url in Urls)
            {
                servers.Add(WebApp.Start(url, startup));
            }

            IsStarted = true;

            while (!_cts.IsCancellationRequested)
            {
                Thread.Sleep(30000);
            }

            IsStarted = false;

            foreach (var server in servers)
            {
                server.Dispose();
            }
        }
    }
}
#endif