Imports Autofac
Imports AutoStep.Execution
Imports AutoStep.Extensions
Imports AutoStep.Projects
Imports Microsoft.Extensions.Configuration

Public Class TestExtension
    Implements IExtensionEntryPoint

    Public Sub AttachToProject(projectConfig As IConfiguration, project As Project) Implements IExtensionEntryPoint.AttachToProject

    End Sub

    Public Sub ExtendExecution(projectConfig As IConfiguration, testRun As TestRun) Implements IExtensionEntryPoint.ExtendExecution

    End Sub

    Public Sub Dispose() Implements IDisposable.Dispose

    End Sub
End Class
