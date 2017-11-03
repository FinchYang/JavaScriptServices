﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.NodeServices.Npm;
using Microsoft.AspNetCore.NodeServices.Util;
using Microsoft.AspNetCore.SpaServices.Prerendering;
using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.SpaServices.AngularCli
{
    /// <summary>
    /// Provides an implementation of <see cref="ISpaPrerendererBuilder"/> that can build
    /// an Angular application by invoking the Angular CLI.
    /// </summary>
    public class AngularCliBuilder : ISpaPrerendererBuilder
    {
        private const int TimeoutMilliseconds = 50 * 1000;
        private readonly string _npmScriptName;

        /// <summary>
        /// Constructs an instance of <see cref="AngularCliBuilder"/>.
        /// </summary>
        /// <param name="npmScript">The name of the script in your package.json file that builds the server-side bundle for your Angular application.</param>
        public AngularCliBuilder(string npmScript)
        {
            if (string.IsNullOrEmpty(npmScript))
            {
                throw new ArgumentException("Cannot be null or empty.", nameof(npmScript));
            }

            _npmScriptName = npmScript;
        }

        /// <inheritdoc />
        public Task Build(ISpaBuilder spaBuilder)
        {
            var sourcePath = spaBuilder.Options.SourcePath;
            if (string.IsNullOrEmpty(sourcePath))
            {
                throw new InvalidOperationException($"To use {nameof(AngularCliBuilder)}, you must supply a non-empty value for the {nameof(SpaOptions.SourcePath)} property of {nameof(SpaOptions)} when calling {nameof(SpaApplicationBuilderExtensions.UseSpa)}.");
            }

            var logger = AngularCliMiddleware.GetOrCreateLogger(spaBuilder.ApplicationBuilder);
            var npmScriptRunner = new NpmScriptRunner(
                sourcePath,
                _npmScriptName,
                "--watch");
            npmScriptRunner.AttachToLogger(logger);

            using (var stdErrReader = new EventedStreamStringReader(npmScriptRunner.StdErr))
            {
                try
                {
                    return npmScriptRunner.StdOut.WaitForMatch(
                        new Regex("chunk"),
                        TimeoutMilliseconds);
                }
                catch (EndOfStreamException ex)
                {
                    throw new InvalidOperationException(
                        $"The NPM script '{_npmScriptName}' exited without indicating success. " +
                        $"Error output was: {stdErrReader.ReadAsString()}", ex);
                }
            }
        }
    }
}
