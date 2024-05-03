using ShiftTemplates.Builder;

new UpdateTemplateVersions().Update();

new PackAndInstallTemplate().PackAndInstall();

new CreateProject().Create(
    includeSampleApp: true,
    identityType: "Internal",
    addFunctions: true,
    addTest: true
);