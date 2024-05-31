using ShiftTemplates.Builder;

if (args.Contains("--update-template-versions"))
{
    new UpdateTemplateVersions().Update();
}

if (!args.Contains("--skip-template-install"))
{
    new PackAndInstallTemplate().PackAndInstall();
}

if (!args.Contains("--skip-project"))
{
    new CreateProject().Create(
        includeSampleApp: false,
        identityType: "Internal",
        addFunctions: true,
        addTest: true,
        launch: false
    );
}

if (!args.Contains("--skip-project"))
{
    new CreateShiftEntity().Create();
}