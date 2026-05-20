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
    new MultiConfigRunner().Run(launchProcesses: !args.Contains("--skip-launch"));
}
