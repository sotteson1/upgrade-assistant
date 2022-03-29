﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.Windows
{
    [ApplicableComponents(ProjectComponents.WinUI)]
    public class WinUIPropertiesUpdater : IUpdater<IProject>
    {
        public const string RuleID = "UA302";

        public string Id => typeof(WinUIPropertiesUpdater).FullName;

        public string Title => "WinUI Project Properties Updater";

        public string Description => "Update project properties to use WinUI and Windows App SDK.";

        public BuildBreakRisk Risk => BuildBreakRisk.Medium;

        private readonly ILogger<WinUIPropertiesUpdater> _logger;

        private readonly WinUIOptionsProjectFilePropertyUpdates? _projectFilePropertyUpdates;

        public WinUIPropertiesUpdater(ILogger<WinUIPropertiesUpdater> logger, IOptions<WinUIOptions> options)
        {
            this._logger = logger;
            this._projectFilePropertyUpdates = options?.Value.ProjectFilePropertyUpdates;
        }

        public async Task<IUpdaterResult> ApplyAsync(IUpgradeContext context, ImmutableArray<IProject> inputs, CancellationToken token)
        {
            if (this._projectFilePropertyUpdates == null)
            {
                return new WindowsDesktopUpdaterResult(
                    "UA302",
                    RuleName: Id,
                    FullDescription: Title,
                    false,
                    "",
                    new List<string>());
            }

            foreach (var project in inputs)
            {
                var projectFile = project.GetFile();
                if (_projectFilePropertyUpdates.Set != null)
                {
                    foreach (var propToSet in _projectFilePropertyUpdates.Set)
                    {
                        projectFile.SetPropertyValue(propToSet.Key, propToSet.Value);
                    }
                }

                if (_projectFilePropertyUpdates.Remove != null)
                {
                    foreach (var propToRemove in _projectFilePropertyUpdates.Remove)
                    {
                        projectFile.RemoveProperty(propToRemove);
                    }
                }

                await projectFile.SaveAsync(token).ConfigureAwait(false);
            }

            return new WindowsDesktopUpdaterResult(
                "UA302",
                RuleName: Id,
                FullDescription: Title,
                true,
                "",
                new List<string>());
        }

        public async Task<IUpdaterResult> IsApplicableAsync(IUpgradeContext context, ImmutableArray<IProject> inputs, CancellationToken token)
        {
            if (this._projectFilePropertyUpdates == null)
            {
                return new WindowsDesktopUpdaterResult(
                    RuleID,
                    RuleName: Id,
                    FullDescription: Title,
                    false,
                    "",
                    new List<string>());
            }

            return new WindowsDesktopUpdaterResult(
                RuleID,
                RuleName: Id,
                FullDescription: Title,
                true,
                "",
                new List<string>());
        }
    }
}
