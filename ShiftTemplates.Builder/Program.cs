﻿using ShiftTemplates.Builder;

new UpdateTemplateVersions().Update();

if (!args.Contains("--skip-template-install"))
{
    new PackAndInstallTemplate().PackAndInstall();
}

if (!args.Contains("--skip-project"))
{
    new CreateProject().Create(
        includeSampleApp: true,
        identityType: "Internal",
        addFunctions: true,
        addTest: true
    );
}